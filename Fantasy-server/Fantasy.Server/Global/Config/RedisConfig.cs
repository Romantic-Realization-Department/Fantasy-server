using StackExchange.Redis;

namespace Fantasy.Server.Global.Config;

public static class RedisConfig
{
    public static IServiceCollection AddRedis(
        this IServiceCollection services,
        IConfiguration configuration,
        string instanceName)
    {
        var connectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Redis connection string is missing.");

        services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(connectionString));

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = connectionString;
            options.InstanceName = instanceName;
        });

        return services;
    }
}
