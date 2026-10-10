using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SwiftJob.Application.Emails;
using SwiftJob.Application.Gmail;
using SwiftJob.Application.Jobs;

namespace SwiftJob.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IJobService, JobService>();
        services.AddScoped<IJobEmailService, JobEmailService>();
        services.AddScoped<IGmailConnectionService, GmailConnectionService>();
        return services;
    }
}
