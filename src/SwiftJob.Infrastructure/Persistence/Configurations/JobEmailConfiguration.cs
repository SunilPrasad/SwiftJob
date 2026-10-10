using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SwiftJob.Domain.Emails;
using SwiftJob.Domain.Jobs;

namespace SwiftJob.Infrastructure.Persistence.Configurations;

internal sealed class JobEmailConfiguration : IEntityTypeConfiguration<JobEmail>
{
    public void Configure(EntityTypeBuilder<JobEmail> builder)
    {
        builder.Property(e => e.GmailMessageId).HasMaxLength(64).IsRequired();
        builder.Property(e => e.GmailThreadId).HasMaxLength(64);
        builder.Property(e => e.Subject).HasMaxLength(1000);
        builder.Property(e => e.Sender).HasMaxLength(500);
        builder.Property(e => e.ProcessingError).HasMaxLength(4000);

        // Idempotent sync: a Gmail message can only be stored once.
        builder.HasIndex(e => e.GmailMessageId).IsUnique();
        builder.HasIndex(e => e.SyncStatus);
        builder.HasIndex(e => e.ReceivedAt);

        builder.HasMany(e => e.JobLinks)
            .WithOne(l => l.JobEmail)
            .HasForeignKey(l => l.JobEmailId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class JobEmailLinkConfiguration : IEntityTypeConfiguration<JobEmailLink>
{
    public void Configure(EntityTypeBuilder<JobEmailLink> builder)
    {
        builder.ToTable("JobEmailLinks");
        builder.HasKey(l => new { l.JobId, l.JobEmailId });
        builder.HasIndex(l => l.JobEmailId);
    }
}
