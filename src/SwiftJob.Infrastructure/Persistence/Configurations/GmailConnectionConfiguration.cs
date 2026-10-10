using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SwiftJob.Domain.Settings;

namespace SwiftJob.Infrastructure.Persistence.Configurations;

internal sealed class GmailConnectionConfiguration : IEntityTypeConfiguration<GmailConnection>
{
    public void Configure(EntityTypeBuilder<GmailConnection> builder)
    {
        builder.Property(c => c.ClientId).HasMaxLength(300);
        builder.Property(c => c.ProtectedClientSecret).HasMaxLength(2000);
        builder.Property(c => c.ProtectedRefreshToken).HasMaxLength(4000);
        builder.Property(c => c.ConnectedEmail).HasMaxLength(320);
        builder.Property(c => c.GrantedScopes).HasMaxLength(2000);
        builder.Property(c => c.SearchQuery).HasMaxLength(1000).IsRequired();
        builder.Ignore(c => c.HasClientCredentials);
        builder.Ignore(c => c.IsConnected);
    }
}
