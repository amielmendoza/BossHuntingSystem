using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BossHuntingSystem.Server.Migrations
{
    /// <inheritdoc />
    public partial class FixEmptyJsonStrings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fix empty JSON strings by converting them to proper JSON arrays
            migrationBuilder.Sql("UPDATE BossDefeats SET LootsJson = '[]' WHERE LootsJson = '' OR LootsJson IS NULL");
            migrationBuilder.Sql("UPDATE BossDefeats SET AttendeesJson = '[]' WHERE AttendeesJson = '' OR AttendeesJson IS NULL");
            migrationBuilder.Sql("UPDATE BossDefeats SET LootItemsJson = '[]' WHERE LootItemsJson = '' OR LootItemsJson IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
