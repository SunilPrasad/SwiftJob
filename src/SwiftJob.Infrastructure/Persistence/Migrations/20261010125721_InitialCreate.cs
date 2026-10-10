using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftJob.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CandidateProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FirstName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 320, nullable: true),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    AddressLine1 = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    AddressLine2 = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Region = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    PostalCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Country = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    LinkedInUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PortfolioUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ProfessionalSummary = table.Column<string>(type: "TEXT", nullable: true),
                    TechnicalSkills = table.Column<string>(type: "TEXT", nullable: false),
                    WorkAuthorisation = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RequiresVisaSponsorship = table.Column<bool>(type: "INTEGER", nullable: true),
                    NoticePeriod = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    PreferredSalaryMin = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    PreferredSalaryMax = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    PreferredSalaryCurrency = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 3, nullable: true),
                    PreferredLocations = table.Column<string>(type: "TEXT", nullable: false),
                    PreferredWorkModes = table.Column<string>(type: "TEXT", nullable: false),
                    CvFileName = table.Column<string>(type: "TEXT", maxLength: 260, nullable: true),
                    CvStoragePath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    CvContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CvUploadedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobEmails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GmailMessageId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    GmailThreadId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Sender = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BodyText = table.Column<string>(type: "TEXT", nullable: true),
                    BodyHtml = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStatus = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ProcessingError = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    SyncedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobEmails", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Jobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Company = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    SalaryText = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    SalaryMin = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    SalaryMax = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    SalaryCurrency = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 3, nullable: true),
                    Location = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    WorkMode = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Requirements = table.Column<string>(type: "TEXT", nullable: true),
                    EnrichedDescription = table.Column<string>(type: "TEXT", nullable: true),
                    EnrichedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ApplicationUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    CanonicalUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    Source = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SourceJobId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    StatusChangedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Educations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CandidateProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    Institution = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Qualification = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    FieldOfStudy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Grade = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Educations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Educations_CandidateProfiles_CandidateProfileId",
                        column: x => x.CandidateProfileId,
                        principalTable: "CandidateProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SavedAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CandidateProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    Question = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Answer = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedAnswers_CandidateProfiles_CandidateProfileId",
                        column: x => x.CandidateProfileId,
                        principalTable: "CandidateProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkExperiences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CandidateProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    JobTitle = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Employer = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Location = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Summary = table.Column<string>(type: "TEXT", nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkExperiences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkExperiences_CandidateProfiles_CandidateProfileId",
                        column: x => x.CandidateProfileId,
                        principalTable: "CandidateProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    JobId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Controller = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ApplicationUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    AgentProvider = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    AgentModel = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CurrentStep = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    LastError = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    FinalOutcome = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationSessions_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JobEmailLinks",
                columns: table => new
                {
                    JobId = table.Column<int>(type: "INTEGER", nullable: false),
                    JobEmailId = table.Column<int>(type: "INTEGER", nullable: false),
                    LinkedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobEmailLinks", x => new { x.JobId, x.JobEmailId });
                    table.ForeignKey(
                        name: "FK_JobEmailLinks_JobEmails_JobEmailId",
                        column: x => x.JobEmailId,
                        principalTable: "JobEmails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobEmailLinks_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationSessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ActionType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Actor = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Outcome = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationActivities_ApplicationSessions_ApplicationSessionId",
                        column: x => x.ApplicationSessionId,
                        principalTable: "ApplicationSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationSessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Question = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Answer = table.Column<string>(type: "TEXT", nullable: true),
                    Source = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ConfirmedByUser = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationAnswers_ApplicationSessions_ApplicationSessionId",
                        column: x => x.ApplicationSessionId,
                        principalTable: "ApplicationSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationActivities_ApplicationSessionId_Timestamp",
                table: "ApplicationActivities",
                columns: new[] { "ApplicationSessionId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationAnswers_ApplicationSessionId",
                table: "ApplicationAnswers",
                column: "ApplicationSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationSessions_JobId_CreatedAt",
                table: "ApplicationSessions",
                columns: new[] { "JobId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationSessions_Status",
                table: "ApplicationSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Educations_CandidateProfileId_SortOrder",
                table: "Educations",
                columns: new[] { "CandidateProfileId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_JobEmailLinks_JobEmailId",
                table: "JobEmailLinks",
                column: "JobEmailId");

            migrationBuilder.CreateIndex(
                name: "IX_JobEmails_GmailMessageId",
                table: "JobEmails",
                column: "GmailMessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobEmails_ReceivedAt",
                table: "JobEmails",
                column: "ReceivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_JobEmails_SyncStatus",
                table: "JobEmails",
                column: "SyncStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_CanonicalUrl",
                table: "Jobs",
                column: "CanonicalUrl",
                unique: true,
                filter: "\"CanonicalUrl\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_ReceivedAt",
                table: "Jobs",
                column: "ReceivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_Source",
                table: "Jobs",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_Source_SourceJobId",
                table: "Jobs",
                columns: new[] { "Source", "SourceJobId" },
                unique: true,
                filter: "\"SourceJobId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_Status_ReceivedAt",
                table: "Jobs",
                columns: new[] { "Status", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SavedAnswers_CandidateProfileId_Category",
                table: "SavedAnswers",
                columns: new[] { "CandidateProfileId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkExperiences_CandidateProfileId_SortOrder",
                table: "WorkExperiences",
                columns: new[] { "CandidateProfileId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationActivities");

            migrationBuilder.DropTable(
                name: "ApplicationAnswers");

            migrationBuilder.DropTable(
                name: "Educations");

            migrationBuilder.DropTable(
                name: "JobEmailLinks");

            migrationBuilder.DropTable(
                name: "SavedAnswers");

            migrationBuilder.DropTable(
                name: "WorkExperiences");

            migrationBuilder.DropTable(
                name: "ApplicationSessions");

            migrationBuilder.DropTable(
                name: "JobEmails");

            migrationBuilder.DropTable(
                name: "CandidateProfiles");

            migrationBuilder.DropTable(
                name: "Jobs");
        }
    }
}
