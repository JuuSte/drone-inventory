using System.Net;
using System.Net.Http.Json;
using DroneInventory.Api.Dtos;
using DroneInventory.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DroneInventory.Api.Tests.Http;

/// <summary>
/// API error mapping (design D3, D4): checks through real HTTP requests that the services'
/// results and domain exceptions are translated to 201, 204, 400, 404 and 409 with ProblemDetails.
/// One endpoint per status code, plus DELETE /api/drones/{id} with 204 (no Loans or only returned Loans),
/// 404 (unknown or already deleted) and 409 (active Loan) for the soft delete (REQ-011).
/// </summary>
public sealed class HttpStatusMappingTests : IDisposable
{
    private const string ProblemJson = "application/problem+json";

    private readonly DroneInventoryApiFactory _factory = new();
    private readonly HttpClient _client;

    public HttpStatusMappingTests() => _client = _factory.CreateClient();

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private Task<HttpResponseMessage> PostDroneAsync(string? name) =>
        _client.PostAsJsonAsync("/api/drones", new CreateDroneRequest(name, "Tello"));

    private static async Task<ProblemDetails> ReadProblemAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal((int)expected, problem.Status);
        Assert.False(string.IsNullOrWhiteSpace(problem.Detail));
        return problem;
    }

    // POST /api/drones with a valid body -> 201 Created with Location header and the Drone.
    [Fact]
    public async Task Created_drone_returns_201()
    {
        var response = await PostDroneAsync("TELLO-07");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var drone = await response.Content.ReadFromJsonAsync<DroneDto>();
        Assert.NotNull(drone);
        Assert.Equal("TELLO-07", drone.Name);
        Assert.Equal(DroneStatus.Available, drone.Status);
    }

    // POST /api/drones with an empty name -> ValidationException -> 400 ProblemDetails.
    [Fact]
    public async Task Empty_drone_name_returns_400()
    {
        var response = await PostDroneAsync("   ");

        await ReadProblemAsync(response, HttpStatusCode.BadRequest);
    }

    // POST /api/loans/{id}/return for an unknown Loan -> NotFoundException -> 404 ProblemDetails.
    [Fact]
    public async Task Return_of_unknown_loan_returns_404()
    {
        var response = await _client.PostAsync("/api/loans/999/return", content: null);

        await ReadProblemAsync(response, HttpStatusCode.NotFound);
    }

    // POST /api/drones with an existing name -> ConflictException -> 409 ProblemDetails.
    [Fact]
    public async Task Duplicate_drone_name_returns_409()
    {
        Assert.Equal(HttpStatusCode.Created, (await PostDroneAsync("TELLO-07")).StatusCode);

        var response = await PostDroneAsync("TELLO-07");

        await ReadProblemAsync(response, HttpStatusCode.Conflict);
    }

    private async Task<DroneDto> CreateDroneAsync(string name)
    {
        var response = await PostDroneAsync(name);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<DroneDto>())!;
    }

    private async Task<IReadOnlyList<DroneDto>> GetDronesAsync() =>
        (await _client.GetFromJsonAsync<IReadOnlyList<DroneDto>>("/api/drones"))!;

    // DELETE /api/drones/{id} for a Drone without Loans -> 204 No Content, the Drone is no longer listed (REQ-011).
    [Fact]
    public async Task Delete_of_drone_without_loans_returns_204()
    {
        var drone = await CreateDroneAsync("TELLO-08");

        var response = await _client.DeleteAsync($"/api/drones/{drone.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());
        Assert.Empty(await GetDronesAsync());
    }

    // DELETE /api/drones/{id} for an unknown Id -> NotFoundException -> 404 ProblemDetails.
    [Fact]
    public async Task Delete_of_unknown_drone_returns_404()
    {
        var response = await _client.DeleteAsync("/api/drones/999");

        var problem = await ReadProblemAsync(response, HttpStatusCode.NotFound);
        Assert.Equal("Drone with Id 999 was not found.", problem.Detail);
    }

    /// <summary>Creates the StudentGroup "Group 3" and an active Loan of the given Drone to it through the API.</summary>
    private async Task<LoanDto> LoanToGroup3Async(int droneId)
    {
        var groupResponse = await _client.PostAsJsonAsync("/api/student-groups", new CreateStudentGroupRequest("Group 3"));
        Assert.Equal(HttpStatusCode.Created, groupResponse.StatusCode);
        var group = (await groupResponse.Content.ReadFromJsonAsync<StudentGroupDto>())!;
        var loanResponse = await _client.PostAsJsonAsync("/api/loans", new CreateLoanRequest(droneId, group.Id, 4, 8));
        Assert.Equal(HttpStatusCode.Created, loanResponse.StatusCode);
        return (await loanResponse.Content.ReadFromJsonAsync<LoanDto>())!;
    }

    // DELETE /api/drones/{id} for a Drone with only a returned Loan -> 204 No Content (soft delete),
    // the Drone is no longer listed (REQ-011, design D7).
    [Fact]
    public async Task Delete_of_drone_with_a_returned_loan_returns_204()
    {
        var drone = await CreateDroneAsync("TELLO-07");
        var loan = await LoanToGroup3Async(drone.Id);
        var returnResponse = await _client.PostAsync($"/api/loans/{loan.Id}/return", content: null);
        Assert.Equal(HttpStatusCode.OK, returnResponse.StatusCode);

        var response = await _client.DeleteAsync($"/api/drones/{drone.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());
        Assert.Empty(await GetDronesAsync());
    }

    // DELETE /api/drones/{id} for a Drone with an active Loan -> ConflictException -> 409 ProblemDetails,
    // the Drone stays listed as "Loaned" (REQ-011, design D7).
    [Fact]
    public async Task Delete_of_drone_with_an_active_loan_returns_409()
    {
        var drone = await CreateDroneAsync("TELLO-07");
        await LoanToGroup3Async(drone.Id);

        var response = await _client.DeleteAsync($"/api/drones/{drone.Id}");

        var problem = await ReadProblemAsync(response, HttpStatusCode.Conflict);
        Assert.Equal("Drone \"TELLO-07\" cannot be deleted because it has an active Loan.", problem.Detail);
        var stored = Assert.Single(await GetDronesAsync());
        Assert.Equal(DroneStatus.Loaned, stored.Status);
    }

    // DELETE /api/drones/{id} for a Drone that is already deleted -> NotFoundException -> 404 ProblemDetails
    // (REQ-011, design D7).
    [Fact]
    public async Task Delete_of_already_deleted_drone_returns_404()
    {
        var drone = await CreateDroneAsync("TELLO-08");
        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/drones/{drone.Id}")).StatusCode);

        var response = await _client.DeleteAsync($"/api/drones/{drone.Id}");

        var problem = await ReadProblemAsync(response, HttpStatusCode.NotFound);
        Assert.Equal($"Drone with Id {drone.Id} was not found.", problem.Detail);
    }
}
