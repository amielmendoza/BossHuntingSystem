using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BossHuntingSystem.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddKillerFieldToBoss : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Killer",
                table: "Bosses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Bosses",
                keyColumn: "Id",
                keyValue: 1,
                column: "Killer",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Killer",
                table: "Bosses");
        }
    }
}
