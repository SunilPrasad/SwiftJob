using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SwiftJob.Application.Ai;
using SwiftJob.Application.Emails;
using SwiftJob.Application.Gmail;
using SwiftJob.Application.Jobs;
using SwiftJob.Application.Sync;

namespace SwiftJob.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IJobService, JobService>();
        services.AddScoped<IJobEmailService, JobEmailService>();
        services.AddScoped<IGmailConnectionService, GmailConnectionService>();
        services.AddScoped<AiSettingsService>();
        services.AddScoped<IAiSettingsService>(sp => sp.GetRequiredService<AiSettingsService>());
        services.AddScoped<IAiConfigurationSource>(sp => sp.GetRequiredService<AiSettingsService>());
        services.AddScoped<JobImporter>();
        services.AddScoped<IJobSyncService, JobSyncService>();
        return services;
    }
}
