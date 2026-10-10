using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftJob.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGmailConnection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GmailConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClientId = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    ProtectedClientSecret = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ProtectedRefreshToken = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    ConnectedEmail = table.Column<string>(type: "TEXT", maxLength: 320, nullable: true),
                    GrantedScopes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ConnectedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SearchQuery = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GmailConnections", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GmailConnections");
        }
    }
}
