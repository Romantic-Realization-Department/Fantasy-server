using Fantasy.Server.Domain.Account.Config;
using Fantasy.Server.Domain.Auth.Config;
using Fantasy.Server.Domain.Dungeon.Config;
using Fantasy.Server.Domain.GameData.Config;
using Fantasy.Server.Domain.GameData.Seed;
using Fantasy.Server.Domain.LevelUp.Config;
using Fantasy.Server.Domain.Player.Config;
using Fantasy.Server.Global.Config;
using Fantasy.Server.Global.Infrastructure;
using Fantasy.Server.Global.Security.Config;
using Gamism.SDK.Extensions.AspNetCore;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddGamismSdk(options =>
{
    options.Swagger.Title = "Fantasy API";
    options.Logging.NotLoggingUrls = ["/swagger/**", "/health"];
    options.Response.NotWrappingUrls = ["/swagger/**", "/health"];
});

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddRedis(builder.Configuration, "fantasy:");
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddRateLimit();

builder.Services.AddAccountServices();
builder.Services.AddAuthServices();
builder.Services.AddPlayerServices();
builder.Services.AddSecurityServices();
builder.Services.AddGameDataServices();
builder.Services.AddLevelUpServices();
builder.Services.AddDungeonServices();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(GameDataSeeder));
    await db.Database.MigrateAsync();
    await GameDataSeeder.SeedAsync(db, logger);
}

app.UseGamismSdk();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
