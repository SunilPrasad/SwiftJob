using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SwiftJob.Domain.Candidates;

namespace SwiftJob.Infrastructure.Persistence.Configurations;

internal sealed class CandidateProfileConfiguration : IEntityTypeConfiguration<CandidateProfile>
{
    public void Configure(EntityTypeBuilder<CandidateProfile> builder)
    {
        builder.Property(p => p.FirstName).HasMaxLength(100);
        builder.Property(p => p.LastName).HasMaxLength(100);
        builder.Property(p => p.Email).HasMaxLength(320);
        builder.Property(p => p.Phone).HasMaxLength(50);
        builder.Property(p => p.AddressLine1).HasMaxLength(200);
        builder.Property(p => p.AddressLine2).HasMaxLength(200);
        builder.Property(p => p.City).HasMaxLength(100);
        builder.Property(p => p.Region).HasMaxLength(100);
        builder.Property(p => p.PostalCode).HasMaxLength(20);
        builder.Property(p => p.Country).HasMaxLength(100);
        builder.Property(p => p.LinkedInUrl).HasMaxLength(500);
        builder.Property(p => p.PortfolioUrl).HasMaxLength(500);
        builder.Property(p => p.WorkAuthorisation).HasMaxLength(500);
        builder.Property(p => p.NoticePeriod).HasMaxLength(100);
        builder.Property(p => p.PreferredSalaryCurrency).HasMaxLength(3).IsFixedLength();
        builder.Property(p => p.CvFileName).HasMaxLength(260);
        builder.Property(p => p.CvStoragePath).HasMaxLength(1024);
        builder.Property(p => p.CvContentType).HasMaxLength(100);

        // Primitive collections are stored as JSON on SQLite (and map to arrays/jsonb on PostgreSQL).
        builder.PrimitiveCollection(p => p.TechnicalSkills);
        builder.PrimitiveCollection(p => p.PreferredLocations);
        builder.PrimitiveCollection(p => p.PreferredWorkModes);

        builder.HasMany(p => p.WorkExperiences).WithOne(w => w.CandidateProfile)
            .HasForeignKey(w => w.CandidateProfileId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(p => p.Educations).WithOne(e => e.CandidateProfile)
            .HasForeignKey(e => e.CandidateProfileId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(p => p.SavedAnswers).WithOne(a => a.CandidateProfile)
            .HasForeignKey(a => a.CandidateProfileId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class WorkExperienceConfiguration : IEntityTypeConfiguration<WorkExperience>
{
    public void Configure(EntityTypeBuilder<WorkExperience> builder)
    {
        builder.Property(w => w.JobTitle).HasMaxLength(200).IsRequired();
        builder.Property(w => w.Employer).HasMaxLength(200).IsRequired();
        builder.Property(w => w.Location).HasMaxLength(200);
        builder.HasIndex(w => new { w.CandidateProfileId, w.SortOrder });
    }
}

internal sealed class EducationConfiguration : IEntityTypeConfiguration<Education>
{
    public void Configure(EntityTypeBuilder<Education> builder)
    {
        builder.Property(e => e.Institution).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Qualification).HasMaxLength(200);
        builder.Property(e => e.FieldOfStudy).HasMaxLength(200);
        builder.Property(e => e.Grade).HasMaxLength(100);
        builder.HasIndex(e => new { e.CandidateProfileId, e.SortOrder });
    }
}

internal sealed class SavedAnswerConfiguration : IEntityTypeConfiguration<SavedAnswer>
{
    public void Configure(EntityTypeBuilder<SavedAnswer> builder)
    {
        builder.Property(a => a.Question).HasMaxLength(1000).IsRequired();
        builder.Property(a => a.Answer).IsRequired();
        builder.Property(a => a.Category).HasMaxLength(100);
        builder.HasIndex(a => new { a.CandidateProfileId, a.Category });
    }
}
