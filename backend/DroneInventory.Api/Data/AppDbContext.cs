using DroneInventory.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DroneInventory.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    /// <summary>Name of the filtered unique index that allows only one active Loan per Drone (design D2).</summary>
    public const string ActiveLoanPerDroneIndex = "IX_Loans_DroneId_Active";

    public DbSet<Drone> Drones => Set<Drone>();
    public DbSet<StudentGroup> StudentGroups => Set<StudentGroup>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<LoanItem> LoanItems => Set<LoanItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Drone>(drone =>
        {
            drone.Property(d => d.Name).IsRequired().UseCollation("NOCASE");
            drone.Property(d => d.Description).IsRequired();
            drone.HasIndex(d => d.Name).IsUnique();
        });

        modelBuilder.Entity<StudentGroup>(group =>
        {
            group.Property(g => g.Name).IsRequired().UseCollation("NOCASE");
            group.HasIndex(g => g.Name).IsUnique();
        });

        modelBuilder.Entity<Loan>(loan =>
        {
            loan.Property(l => l.Status).HasConversion<string>();

            // Loans are never deleted together with a Drone or StudentGroup, so history can't be lost.
            loan.HasOne(l => l.Drone)
                .WithMany(d => d.Loans)
                .HasForeignKey(l => l.DroneId)
                .OnDelete(DeleteBehavior.Restrict);

            loan.HasOne(l => l.StudentGroup)
                .WithMany(g => g.Loans)
                .HasForeignKey(l => l.StudentGroupId)
                .OnDelete(DeleteBehavior.Restrict);

            // A Drone can have at most one active Loan, enforced by the database (REQ-005).
            loan.HasIndex(l => l.DroneId)
                .IsUnique()
                .HasFilter($"\"Status\" = '{nameof(LoanStatus.Active)}'")
                .HasDatabaseName(ActiveLoanPerDroneIndex);
        });

        modelBuilder.Entity<LoanItem>(item =>
        {
            item.Property(i => i.Type).HasConversion<string>();

            item.HasOne(i => i.Loan)
                .WithMany(l => l.Items)
                .HasForeignKey(i => i.LoanId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
