using BackendProjectTemplate.Domain.Analytics;
using Microsoft.Extensions.DependencyInjection;

namespace BackendProjectTemplate.Infrastructure.Analytics;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUserActivityAnalytics(this IServiceCollection services)
    {
        services.AddSingleton<IUserActivityThrottle, RedisUserActivityThrottle>();
        services.AddScoped<IUserDailyActivityPersistence, UserDailyActivityPersistence>();
        return services;
    }
}
