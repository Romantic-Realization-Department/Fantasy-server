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
            ?? throw new InvalidOperationException("Database connection string is missing.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }
}
