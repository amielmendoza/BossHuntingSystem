using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BossHuntingSystem.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddKillerFieldToBossDefeat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Killer",
                table: "BossDefeats",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Killer",
                table: "BossDefeats");
        }
    }
}
