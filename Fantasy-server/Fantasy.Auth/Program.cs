using Fantasy.Auth.Domain.Account.Service;
using Fantasy.Auth.Domain.Account.Service.Interface;
using Fantasy.Auth.Global.Security.Provider;
using Fantasy.Common.Global.Config;
using Fantasy.Common.Global.Security.Filter;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddScoped<ICreateAccountService, CreateAccountService>();
builder.Services.AddScoped<IDeleteAccountService, DeleteAccountService>();

builder.Services.AddScoped<JwtProvider>();
builder.Services.AddScoped<JwtAuthenticationFilter>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();