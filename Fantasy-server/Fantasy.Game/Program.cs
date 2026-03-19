using Gamism.SDK.Extensions.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddGamismSdk(options =>
{
    options.Swagger.Title = "Fantasy Game API";
    options.Logging.NotLoggingUrls = ["/swagger/**", "/health"];
    options.Response.NotWrappingUrls = ["/swagger/**", "/health"];
});

var app = builder.Build();

app.UseGamismSdk();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
