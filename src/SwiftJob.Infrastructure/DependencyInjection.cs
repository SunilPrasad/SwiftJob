using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SwiftJob.Application.Abstractions;
using SwiftJob.Infrastructure.Persistence;
using SwiftJob.Infrastructure.Persistence.Seeding;

namespace SwiftJob.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "SwiftJob";

    /// <param name="contentRootPath">Base directory for resolving a relative SQLite file path.</param>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration, string contentRootPath)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<AuditingInterceptor>();
        services.AddDbContext<AppDbContext>((sp, options) => options
            .UseSqlite(ResolveSqlitePath(connectionString, contentRootPath))
            .AddInterceptors(sp.GetRequiredService<AuditingInterceptor>()));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<SampleDataSeeder>();

        return services;
    }

    /// <summary>Makes a relative SQLite file path absolute (relative to the content root) and ensures its folder exists.</summary>
    internal static string ResolveSqlitePath(string connectionString, string contentRootPath)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        if (builder.DataSource is { Length: > 0 } dataSource
            && !dataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase)
            && builder.Mode != SqliteOpenMode.Memory)
        {
            var fullPath = Path.GetFullPath(Path.Combine(contentRootPath, dataSource));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            builder.DataSource = fullPath;
        }

        return builder.ToString();
    }
}
