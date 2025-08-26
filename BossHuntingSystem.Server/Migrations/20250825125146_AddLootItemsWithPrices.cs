using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BossHuntingSystem.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddLootItemsWithPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LootItemsJson",
                table: "BossDefeats",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LootItemsJson",
                table: "BossDefeats");
        }
    }
}
