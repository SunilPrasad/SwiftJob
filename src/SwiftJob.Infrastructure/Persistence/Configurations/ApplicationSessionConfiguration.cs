using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SwiftJob.Domain.Applications;

namespace SwiftJob.Infrastructure.Persistence.Configurations;

internal sealed class ApplicationSessionConfiguration : IEntityTypeConfiguration<ApplicationSession>
{
    public void Configure(EntityTypeBuilder<ApplicationSession> builder)
    {
        builder.Property(s => s.ApplicationUrl).HasMaxLength(2048);
        builder.Property(s => s.AgentProvider).HasMaxLength(100);
        builder.Property(s => s.AgentModel).HasMaxLength(200);
        builder.Property(s => s.CurrentStep).HasMaxLength(500);
        builder.Property(s => s.LastError).HasMaxLength(4000);
        builder.Property(s => s.FinalOutcome).HasMaxLength(1000);

        builder.HasIndex(s => new { s.JobId, s.CreatedAt });
        builder.HasIndex(s => s.Status);

        builder.HasMany(s => s.Activities).WithOne(a => a.ApplicationSession)
            .HasForeignKey(a => a.ApplicationSessionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(s => s.Answers).WithOne(a => a.ApplicationSession)
            .HasForeignKey(a => a.ApplicationSessionId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ApplicationActivityConfiguration : IEntityTypeConfiguration<ApplicationActivity>
{
    public void Configure(EntityTypeBuilder<ApplicationActivity> builder)
    {
        builder.Property(a => a.Description).HasMaxLength(4000).IsRequired();
        builder.HasIndex(a => new { a.ApplicationSessionId, a.Timestamp });
    }
}

internal sealed class ApplicationAnswerConfiguration : IEntityTypeConfiguration<ApplicationAnswer>
{
    public void Configure(EntityTypeBuilder<ApplicationAnswer> builder)
    {
        builder.Property(a => a.Question).HasMaxLength(2000).IsRequired();
        builder.HasIndex(a => a.ApplicationSessionId);
    }
}
