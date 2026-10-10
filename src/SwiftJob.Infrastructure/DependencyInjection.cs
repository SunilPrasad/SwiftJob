using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SwiftJob.Application.Abstractions;
using SwiftJob.Application.Ai;
using SwiftJob.Application.Browser;
using SwiftJob.Application.Extraction;
using SwiftJob.Application.Gmail;
using SwiftJob.Application.Sync;
using SwiftJob.Infrastructure.Ai;
using SwiftJob.Infrastructure.Browser;
using SwiftJob.Infrastructure.Gmail;
using SwiftJob.Infrastructure.Persistence;
using SwiftJob.Infrastructure.Persistence.Seeding;
using SwiftJob.Infrastructure.Security;

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

        AddSecretProtection(services, configuration, contentRootPath);
        AddGmail(services, configuration);
        AddAi(services);
        AddBrowser(services, configuration, contentRootPath);

        return services;
    }

    private static void AddSecretProtection(IServiceCollection services, IConfiguration configuration, string contentRootPath)
    {
        var keysPath = Path.GetFullPath(Path.Combine(
            contentRootPath, configuration["DataProtection:KeysPath"] ?? Path.Combine("App_Data", "keys")));
        Directory.CreateDirectory(keysPath);

        var dataProtection = services.AddDataProtection()
            .SetApplicationName("SwiftJob")
            .PersistKeysToFileSystem(new DirectoryInfo(keysPath));
        if (OperatingSystem.IsWindows())
        {
            dataProtection.ProtectKeysWithDpapi();
        }

        services.AddSingleton<ISecretProtector, DataProtectionSecretProtector>();
    }

    private static void AddGmail(IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.Configure<GmailOptions>(configuration.GetSection(GmailOptions.SectionName));
        services.Configure<SyncOptions>(configuration.GetSection(SyncOptions.SectionName));
        services.AddSingleton<IOAuthStateStore, MemoryOAuthStateStore>();
        services.AddSingleton<GoogleGmailClient>();
        services.AddSingleton<IGmailOAuthClient>(sp => sp.GetRequiredService<GoogleGmailClient>());
        services.AddSingleton<IGmailMailbox>(sp => sp.GetRequiredService<GoogleGmailClient>());
    }

    private static void AddBrowser(IServiceCollection services, IConfiguration configuration, string contentRootPath)
    {
        var section = configuration.GetSection(BrowserOptions.SectionName);
        services.Configure<BrowserOptions>(section);
        var options = section.Get<BrowserOptions>() ?? new BrowserOptions();
        services.AddSingleton(new BrowserPaths(
            Path.GetFullPath(Path.Combine(contentRootPath, options.ProfilePath)),
            Path.GetFullPath(Path.Combine(contentRootPath, options.WorkspacePath))));
        services.AddSingleton<PlaywrightMcpBrowser>();
        services.AddSingleton<IBrowserSessionService>(sp => sp.GetRequiredService<PlaywrightMcpBrowser>());
        services.AddSingleton<IFormFillAgent, FormFillAgent>();
    }

    private static void AddAi(IServiceCollection services)
    {
        services.AddSingleton<IAiChatClientFactory, AiChatClientFactory>();
        services.AddScoped<IAiConnectionProbe, AgentConnectionProbe>();
        services.AddScoped<IJobExtractionService, AgentJobExtractionService>();
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
