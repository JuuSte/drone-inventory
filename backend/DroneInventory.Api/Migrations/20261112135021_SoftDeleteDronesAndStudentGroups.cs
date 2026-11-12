using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DroneInventory.Api.Migrations
{
    /// <inheritdoc />
    public partial class SoftDeleteDronesAndStudentGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StudentGroups_Name",
                table: "StudentGroups");

            migrationBuilder.DropIndex(
                name: "IX_Drones_Name",
                table: "Drones");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "StudentGroups",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Drones",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_StudentGroups_Name",
                table: "StudentGroups",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Drones_Name",
                table: "Drones",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StudentGroups_Name",
                table: "StudentGroups");

            migrationBuilder.DropIndex(
                name: "IX_Drones_Name",
                table: "Drones");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "StudentGroups");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Drones");

            migrationBuilder.CreateIndex(
                name: "IX_StudentGroups_Name",
                table: "StudentGroups",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Drones_Name",
                table: "Drones",
                column: "Name",
                unique: true);
        }
    }
}
