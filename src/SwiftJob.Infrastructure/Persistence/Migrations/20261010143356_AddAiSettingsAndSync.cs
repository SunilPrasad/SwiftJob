using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftJob.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiSettingsAndSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProcessingAttempts",
                table: "JobEmails",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "AutoSyncEnabled",
                table: "GmailConnections",
                type: "INTEGER",
                nullable: false,
                defaultValue: true); // existing connections: keep automatic sync on

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSuccessfulSyncStartedAt",
                table: "GmailConnections",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncCompletedAt",
                table: "GmailConnections",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastSyncError",
                table: "GmailConnections",
                type: "TEXT",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastSyncSummary",
                table: "GmailConnections",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SyncIntervalMinutes",
                table: "GmailConnections",
                type: "INTEGER",
                nullable: false,
                defaultValue: 30); // GmailConnection.DefaultSyncIntervalMinutes

            migrationBuilder.CreateTable(
                name: "AiSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Model = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ProtectedApiKey = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Endpoint = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiSettings");

            migrationBuilder.DropColumn(
                name: "ProcessingAttempts",
                table: "JobEmails");

            migrationBuilder.DropColumn(
                name: "AutoSyncEnabled",
                table: "GmailConnections");

            migrationBuilder.DropColumn(
                name: "LastSuccessfulSyncStartedAt",
                table: "GmailConnections");

            migrationBuilder.DropColumn(
                name: "LastSyncCompletedAt",
                table: "GmailConnections");

            migrationBuilder.DropColumn(
                name: "LastSyncError",
                table: "GmailConnections");

            migrationBuilder.DropColumn(
                name: "LastSyncSummary",
                table: "GmailConnections");

            migrationBuilder.DropColumn(
                name: "SyncIntervalMinutes",
                table: "GmailConnections");
        }
    }
}
