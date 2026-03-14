using Fantasy.Common.Global.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fantasy.Common.Global.Config;

public static class DatabaseConfig
{
    public static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("Database")));

        return services;
    }
}