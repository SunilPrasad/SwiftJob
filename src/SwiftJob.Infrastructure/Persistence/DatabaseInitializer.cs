using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SwiftJob.Infrastructure.Persistence.Seeding;

namespace SwiftJob.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    /// <summary>Applies pending migrations and, when requested, seeds sample data into an empty database.</summary>
    public static async Task InitializeAsync(
        IServiceProvider services, bool applyMigrations, bool seedSampleData, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseInitializer));

        if (applyMigrations)
        {
            logger.LogInformation("Applying database migrations");
            await db.Database.MigrateAsync(cancellationToken);
        }

        if (seedSampleData)
        {
            await scope.ServiceProvider.GetRequiredService<SampleDataSeeder>().SeedAsync(cancellationToken);
        }
    }
}
