using Fantasy.Auth.Domain.Account.Config;
using Fantasy.Auth.Domain.Auth.Config;
using Fantasy.Auth.Global.Security.Config;
using Fantasy.Common.Global.Config;
using Fantasy.Common.Global.Exception;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddRedis(builder.Configuration, "auth:");
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddRateLimit();

builder.Services.AddAccountServices();
builder.Services.AddAuthServices();
builder.Services.AddSecurityServices();

var app = builder.Build();

app.UseExceptionHandler();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
