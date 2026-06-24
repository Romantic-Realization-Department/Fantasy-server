using Fantasy.Server.Domain.Account.Repository;
using Fantasy.Server.Domain.Account.Repository.Interface;
using Fantasy.Server.Domain.Account.Service;
using Fantasy.Server.Domain.Account.Service.Interface;

namespace Fantasy.Server.Domain.Account.Config;

public static class AccountServiceConfig
{
    public static IServiceCollection AddAccountServices(this IServiceCollection services)
    {
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ICreateAccountService, CreateAccountService>();
        services.AddScoped<IDeleteAccountService, DeleteAccountService>();
        return services;
    }
}
