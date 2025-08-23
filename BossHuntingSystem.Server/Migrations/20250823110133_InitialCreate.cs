using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BossHuntingSystem.Server.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bosses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RespawnHours = table.Column<int>(type: "int", nullable: false),
                    LastKilledAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bosses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BossDefeats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BossId = table.Column<int>(type: "int", nullable: false),
                    BossName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DefeatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LootsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AttendeesJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BossDefeats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BossDefeats_Bosses_BossId",
                        column: x => x.BossId,
                        principalTable: "Bosses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Bosses",
                columns: new[] { "Id", "LastKilledAt", "Name", "RespawnHours" },
                values: new object[] { 1, new DateTime(2025, 8, 23, 9, 1, 32, 12, DateTimeKind.Utc).AddTicks(9348), "Gadwa", 1 });

            migrationBuilder.CreateIndex(
                name: "IX_BossDefeats_BossId",
                table: "BossDefeats",
                column: "BossId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BossDefeats");

            migrationBuilder.DropTable(
                name: "Bosses");
        }
    }
}
