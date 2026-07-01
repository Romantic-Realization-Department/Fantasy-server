using Fantasy.Server.Domain.Tutorial.Repository;
using Fantasy.Server.Domain.Tutorial.Repository.Interface;
using Fantasy.Server.Domain.Tutorial.Service;
using Fantasy.Server.Domain.Tutorial.Service.Interface;

namespace Fantasy.Server.Domain.Tutorial.Config;

public static class TutorialServiceConfig
{
    public static IServiceCollection AddTutorialServices(this IServiceCollection services)
    {
        services.AddScoped<IPlayerTutorialRepository, PlayerTutorialRepository>();
        services.AddScoped<ICompleteTutorialService, CompleteTutorialService>();
        services.AddScoped<IGetCompletedTutorialsService, GetCompletedTutorialsService>();

        return services;
    }
}
