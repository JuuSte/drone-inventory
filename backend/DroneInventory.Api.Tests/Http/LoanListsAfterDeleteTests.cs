using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DroneInventory.Api.Data;
using DroneInventory.Api.Dtos;
using DroneInventory.Api.Models;
using DroneInventory.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DroneInventory.Api.Tests.Http;

/// <summary>
/// The Loan lists keep the Names of deleted Drones and StudentGroups, end to end through the API
/// (REQ-011, REQ-012, REQ-010; soft-delete design D2, D8).
/// </summary>
public sealed class LoanListsAfterDeleteTests : IDisposable
{
    private readonly DroneInventoryApiFactory _factory = new();
    private readonly HttpClient _client;

    public LoanListsAfterDeleteTests() => _client = _factory.CreateClient();

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task<T> PostCreatedAsync<T>(string url, object body)
    {
        var response = await _client.PostAsJsonAsync(url, body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private async Task<(DroneDto Drone, StudentGroupDto Group, LoanDto Loan)> LoanTello07ToGroup3Async()
    {
        var drone = await PostCreatedAsync<DroneDto>("/api/drones", new CreateDroneRequest("TELLO-07", "DJI Tello"));
        var group = await PostCreatedAsync<StudentGroupDto>("/api/student-groups", new CreateStudentGroupRequest("Group 3"));
        var loan = await PostCreatedAsync<LoanDto>("/api/loans", new CreateLoanRequest(drone.Id, group.Id, 4, 8));
        return (drone, group, loan);
    }

    private async Task<JsonElement> GetJsonAsync(string url)
    {
        var response = await _client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    // "Drone with loan history" (REQ-011) and "StudentGroup with loan history" (REQ-012),
    // the "loan history still shows ..." part:
    // GIVEN "TELLO-07" and "Group 3" have only a returned Loan
    // WHEN "TELLO-07" and "Group 3" are deleted
    // THEN they are no longer listed
    // AND GET /api/loans/history still shows the Loan with "TELLO-07" and "Group 3"
    [Fact]
    public async Task Loan_history_still_shows_the_Names_of_a_deleted_Drone_and_StudentGroup()
    {
        var (drone, group, loan) = await LoanTello07ToGroup3Async();
        var returned = await _client.PostAsync($"/api/loans/{loan.Id}/return", content: null);
        Assert.Equal(HttpStatusCode.OK, returned.StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/drones/{drone.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/student-groups/{group.Id}")).StatusCode);

        Assert.Equal(0, (await GetJsonAsync("/api/drones")).GetArrayLength());
        Assert.Equal(0, (await GetJsonAsync("/api/student-groups")).GetArrayLength());

        var history = await GetJsonAsync("/api/loans/history");
        var entry = Assert.Single(history.EnumerateArray());
        Assert.Equal(loan.Id, entry.GetProperty("id").GetInt32());
        Assert.Equal(drone.Id, entry.GetProperty("droneId").GetInt32());
        Assert.Equal("TELLO-07", entry.GetProperty("droneName").GetString());
        Assert.Equal(group.Id, entry.GetProperty("studentGroupId").GetInt32());
        Assert.Equal("Group 3", entry.GetProperty("studentGroupName").GetString());
        Assert.Equal(4, entry.GetProperty("batteries").GetInt32());
        Assert.Equal(8, entry.GetProperty("propellers").GetInt32());
        Assert.Equal(nameof(LoanStatus.Returned), entry.GetProperty("status").GetString());
    }

    // Guard for design D2 (no global query filter): the active Loans list must not drop a Loan whose
    // Drone or StudentGroup is marked as deleted. Through the API this state cannot occur, because the
    // delete of a Drone or StudentGroup with an active Loan is rejected, so the flags are set directly.
    [Fact]
    public async Task Active_Loans_list_keeps_a_Loan_whose_Drone_and_StudentGroup_are_marked_deleted()
    {
        var (drone, group, loan) = await LoanTello07ToGroup3Async();
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.Drones.Where(d => d.Id == drone.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(d => d.IsDeleted, true));
            await context.StudentGroups.Where(g => g.Id == group.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(g => g.IsDeleted, true));
        }

        var active = await GetJsonAsync("/api/loans/active");

        var entry = Assert.Single(active.EnumerateArray());
        Assert.Equal(loan.Id, entry.GetProperty("id").GetInt32());
        Assert.Equal("TELLO-07", entry.GetProperty("droneName").GetString());
        Assert.Equal("Group 3", entry.GetProperty("studentGroupName").GetString());
        Assert.Equal(nameof(LoanStatus.Active), entry.GetProperty("status").GetString());
    }
}
