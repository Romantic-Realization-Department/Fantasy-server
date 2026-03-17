using Fantasy.Auth.Domain.Account.Repository;
using Fantasy.Auth.Domain.Account.Service;
using Fantasy.Auth.Domain.Account.Service.Interface;
using Fantasy.Auth.Domain.Auth.Repository;
using Fantasy.Auth.Domain.Auth.Service;
using Fantasy.Auth.Domain.Auth.Service.Interface;
using Fantasy.Auth.Global.Security.Jwt;
using Fantasy.Auth.Global.Security.Provider;
using Fantasy.Common.Domain.Account.Repository;
using Fantasy.Common.Domain.Auth.Repository;
using Fantasy.Common.Global.Config;
using Fantasy.Common.Global.Exception;
using Fantasy.Common.Global.Security.Filter;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddRedis(builder.Configuration, "auth:");
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddRateLimit();

builder.Services.AddScoped<IAccountRepository, AccountRepository>();

builder.Services.AddScoped<ICreateAccountService, CreateAccountService>();
builder.Services.AddScoped<IDeleteAccountService, DeleteAccountService>();
builder.Services.AddScoped<ILoginService, LoginService>();
builder.Services.AddScoped<ILogoutService, LogoutService>();
builder.Services.AddScoped<IRefreshTokenRedisRepository, RefreshTokenRedisRepository>();

builder.Services.AddSingleton<IJwtProvider, JwtProvider>();
builder.Services.AddSingleton<JwtAuthenticationFilter>();
builder.Services.AddScoped<ICurrentUserProvider, CurrentUserProvider>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();