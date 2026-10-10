using Microsoft.EntityFrameworkCore;
using SwiftJob.Application.Abstractions;
using SwiftJob.Domain.Applications;
using SwiftJob.Domain.Candidates;
using SwiftJob.Domain.Emails;
using SwiftJob.Domain.Jobs;

namespace SwiftJob.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<JobEmail> JobEmails => Set<JobEmail>();
    public DbSet<JobEmailLink> JobEmailLinks => Set<JobEmailLink>();
    public DbSet<CandidateProfile> CandidateProfiles => Set<CandidateProfile>();
    public DbSet<WorkExperience> WorkExperiences => Set<WorkExperience>();
    public DbSet<Education> Educations => Set<Education>();
    public DbSet<SavedAnswer> SavedAnswers => Set<SavedAnswer>();
    public DbSet<ApplicationSession> ApplicationSessions => Set<ApplicationSession>();
    public DbSet<ApplicationActivity> ApplicationActivities => Set<ApplicationActivity>();
    public DbSet<ApplicationAnswer> ApplicationAnswers => Set<ApplicationAnswer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Enums are stored as readable strings: easier to inspect and safe against enum re-ordering.
        configurationBuilder.Properties<JobStatus>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<WorkMode>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<EmailSyncStatus>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<ApplicationSessionStatus>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<BrowserController>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<ActivityActionType>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<ActivityOutcome>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<ActivityActor>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<AnswerSource>().HaveConversion<string>().HaveMaxLength(32);

        // All timestamps are UTC. SQLite has no DateTimeOffset ordering support, and PostgreSQL's
        // timestamptz requires DateTimeKind.Utc, so UTC DateTime is the portable choice.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<NullableUtcDateTimeConverter>();

        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }
}
