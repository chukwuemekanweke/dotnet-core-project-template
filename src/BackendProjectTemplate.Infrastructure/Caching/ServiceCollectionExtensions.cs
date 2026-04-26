using BackendProjectTemplate.Domain.Common.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace BackendProjectTemplate.Infrastructure.Caching;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRedisCaching(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IJsonCache, DistributedJsonCache>();

        var redisConnectionString = configuration.GetConnectionString("Redis");
        var lazyMultiplexer = new Lazy<IConnectionMultiplexer>(() =>
            ConnectionMultiplexer.Connect(redisConnectionString!));

        services.AddSingleton<IConnectionMultiplexer>(_ => lazyMultiplexer.Value);
        services.AddStackExchangeRedisCache(options =>
        {
            options.ConnectionMultiplexerFactory = () => Task.FromResult(lazyMultiplexer.Value);
        });

        return services;
    }
}
