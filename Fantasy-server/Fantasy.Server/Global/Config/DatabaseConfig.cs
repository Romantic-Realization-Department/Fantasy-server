using Fantasy.Server.Global.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Fantasy.Server.Global.Config;

public static class DatabaseConfig
{
    public static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("데이터베이스 연결 문자열이 설정되지 않았습니다.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }
}
