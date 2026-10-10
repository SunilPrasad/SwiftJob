using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SwiftJob.Domain.Ai;

namespace SwiftJob.Infrastructure.Persistence.Configurations;

internal sealed class AiSettingsConfiguration : IEntityTypeConfiguration<AiSettings>
{
    public void Configure(EntityTypeBuilder<AiSettings> builder)
    {
        builder.Property(s => s.Provider).HasConversion<string>().HasMaxLength(32);
        builder.Property(s => s.Model).HasMaxLength(200);
        builder.Property(s => s.ProtectedApiKey).HasMaxLength(2000);
        builder.Property(s => s.Endpoint).HasMaxLength(500);
        builder.Ignore(s => s.IsConfigured);
    }
}
