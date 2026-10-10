using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SwiftJob.Domain.Jobs;

namespace SwiftJob.Infrastructure.Persistence.Configurations;

internal sealed class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.Property(j => j.Title).HasMaxLength(500).IsRequired();
        builder.Property(j => j.Company).HasMaxLength(300);
        builder.Property(j => j.SalaryText).HasMaxLength(300);
        builder.Property(j => j.SalaryCurrency).HasMaxLength(3).IsFixedLength();
        builder.Property(j => j.Location).HasMaxLength(300);
        builder.Property(j => j.ApplicationUrl).HasMaxLength(2048);
        builder.Property(j => j.CanonicalUrl).HasMaxLength(2048);
        builder.Property(j => j.Source).HasMaxLength(100).IsRequired();
        builder.Property(j => j.SourceJobId).HasMaxLength(200);

        // Dashboard default view: status filter + newest first.
        builder.HasIndex(j => new { j.Status, j.ReceivedAt });
        builder.HasIndex(j => j.ReceivedAt);
        builder.HasIndex(j => j.Source);

        // De-duplication keys. Partial unique indexes: the filter syntax is valid on SQLite and PostgreSQL.
        builder.HasIndex(j => new { j.Source, j.SourceJobId })
            .IsUnique()
            .HasFilter("\"SourceJobId\" IS NOT NULL");
        builder.HasIndex(j => j.CanonicalUrl)
            .IsUnique()
            .HasFilter("\"CanonicalUrl\" IS NOT NULL");

        builder.HasMany(j => j.EmailLinks)
            .WithOne(l => l.Job)
            .HasForeignKey(l => l.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        // Application history is an audit record: deleting a job with history is refused.
        builder.HasMany(j => j.ApplicationSessions)
            .WithOne(s => s.Job)
            .HasForeignKey(s => s.JobId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
