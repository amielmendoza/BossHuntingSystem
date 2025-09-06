using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BossHuntingSystem.Server.Migrations
{
    /// <inheritdoc />
    public partial class RenameKillerToOwner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Killer",
                table: "Bosses",
                newName: "Owner");

            migrationBuilder.RenameColumn(
                name: "Killer",
                table: "BossDefeats",
                newName: "Owner");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Owner",
                table: "Bosses",
                newName: "Killer");

            migrationBuilder.RenameColumn(
                name: "Owner",
                table: "BossDefeats",
                newName: "Killer");
        }
    }
}
