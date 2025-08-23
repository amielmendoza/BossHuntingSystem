using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BossHuntingSystem.Server.Migrations
{
    /// <inheritdoc />
    public partial class FixSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Bosses",
                keyColumn: "Id",
                keyValue: 1,
                column: "LastKilledAt",
                value: new DateTime(2025, 8, 23, 10, 0, 0, 0, DateTimeKind.Utc));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Bosses",
                keyColumn: "Id",
                keyValue: 1,
                column: "LastKilledAt",
                value: new DateTime(2025, 8, 23, 9, 1, 32, 12, DateTimeKind.Utc).AddTicks(9348));
        }
    }
}
