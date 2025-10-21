using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BossHuntingSystem.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenancySupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Members_Name",
                table: "Members");

            migrationBuilder.DeleteData(
                table: "Bosses",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClientId",
                table: "Members",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ClientId",
                table: "Bosses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ClientId",
                table: "BossDefeats",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LicenseKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LicenseExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    GracePeriodDays = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: true),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AdditionalInfo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLoginDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // DATA MIGRATION: Create default client for existing data
            migrationBuilder.Sql(@"
                -- Create default Legacy client for existing records
                INSERT INTO Clients (Name, LicenseKey, LicenseExpirationDate, IsActive, CreatedDate, ContactEmail, GracePeriodDays, CompanyName, Notes)
                VALUES (
                    'Legacy Client',
                    'LEGACY-' + REPLACE(CAST(NEWID() AS NVARCHAR(36)), '-', ''),
                    DATEADD(year, 10, GETUTCDATE()),
                    1,
                    GETUTCDATE(),
                    'admin@legacy.local',
                    7,
                    'Migrated from single-tenant system',
                    'This client was automatically created during multi-tenant migration. All existing data has been assigned to this client.'
                );

                DECLARE @LegacyClientId INT = SCOPE_IDENTITY();

                -- Assign all existing Members to Legacy client
                UPDATE Members SET ClientId = @LegacyClientId WHERE ClientId = 0;

                -- Assign all existing Bosses to Legacy client
                UPDATE Bosses SET ClientId = @LegacyClientId WHERE ClientId = 0;

                -- Assign all existing BossDefeats to Legacy client
                UPDATE BossDefeats SET ClientId = @LegacyClientId WHERE ClientId = 0;

                -- Create default SuperAdmin user for Legacy client (password: Admin@123 - CHANGE IMMEDIATELY!)
                INSERT INTO Users (Username, PasswordHash, Email, Role, ClientId, IsActive, CreatedDate)
                VALUES (
                    'admin',
                    '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYIeWIgNZkq',
                    'admin@legacy.local',
                    'Admin',
                    @LegacyClientId,
                    1,
                    GETUTCDATE()
                );

                -- Create SuperAdmin user (not tied to any client, password: SuperAdmin@123 - CHANGE IMMEDIATELY!)
                INSERT INTO Clients (Name, LicenseKey, LicenseExpirationDate, IsActive, CreatedDate, ContactEmail, GracePeriodDays, CompanyName)
                VALUES (
                    'System Administration',
                    'SYSTEM-ADMIN-' + REPLACE(CAST(NEWID() AS NVARCHAR(36)), '-', ''),
                    DATEADD(year, 100, GETUTCDATE()),
                    1,
                    GETUTCDATE(),
                    'superadmin@system.local',
                    0,
                    'Internal System Client'
                );

                DECLARE @SystemClientId INT = SCOPE_IDENTITY();

                INSERT INTO Users (Username, PasswordHash, Email, Role, ClientId, IsActive, CreatedDate)
                VALUES (
                    'superadmin',
                    '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYIeWIgNZkq',
                    'superadmin@system.local',
                    'SuperAdmin',
                    @SystemClientId,
                    1,
                    GETUTCDATE()
                );
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Members_ClientId_Name",
                table: "Members",
                columns: new[] { "ClientId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bosses_ClientId_Name",
                table: "Bosses",
                columns: new[] { "ClientId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_BossDefeats_ClientId_DefeatedAtUtc",
                table: "BossDefeats",
                columns: new[] { "ClientId", "DefeatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ClientId_EntityType_EntityId",
                table: "AuditLogs",
                columns: new[] { "ClientId", "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ClientId_Timestamp",
                table: "AuditLogs",
                columns: new[] { "ClientId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_Clients_LicenseKey",
                table: "Clients",
                column: "LicenseKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ClientId_Username",
                table: "Users",
                columns: new[] { "ClientId", "Username" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BossDefeats_Clients_ClientId",
                table: "BossDefeats",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Bosses_Clients_ClientId",
                table: "Bosses",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Members_Clients_ClientId",
                table: "Members",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BossDefeats_Clients_ClientId",
                table: "BossDefeats");

            migrationBuilder.DropForeignKey(
                name: "FK_Bosses_Clients_ClientId",
                table: "Bosses");

            migrationBuilder.DropForeignKey(
                name: "FK_Members_Clients_ClientId",
                table: "Members");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Clients");

            migrationBuilder.DropIndex(
                name: "IX_Members_ClientId_Name",
                table: "Members");

            migrationBuilder.DropIndex(
                name: "IX_Bosses_ClientId_Name",
                table: "Bosses");

            migrationBuilder.DropIndex(
                name: "IX_BossDefeats_ClientId_DefeatedAtUtc",
                table: "BossDefeats");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "Bosses");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "BossDefeats");

            migrationBuilder.InsertData(
                table: "Bosses",
                columns: new[] { "Id", "LastKilledAt", "Name", "Owner", "RespawnHours" },
                values: new object[] { 1, new DateTime(2025, 8, 23, 10, 0, 0, 0, DateTimeKind.Utc), "Gadwa", null, 1 });

            migrationBuilder.CreateIndex(
                name: "IX_Members_Name",
                table: "Members",
                column: "Name",
                unique: true);
        }
    }
}
