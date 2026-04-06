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
            ?? throw new InvalidOperationException("Redis 연결 문자열이 설정되지 않았습니다.");

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
