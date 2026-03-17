using Fantasy.Auth.Domain.Account.Repository;
using Fantasy.Auth.Domain.Account.Service;
using Fantasy.Auth.Domain.Account.Service.Interface;
using Fantasy.Common.Domain.Account.Repository;

namespace Fantasy.Auth.Domain.Account.Config;

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
