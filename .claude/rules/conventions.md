---
description: Naming, DTO, and EF Core entity configuration conventions. Always applied.
alwaysApply: true
---

## Naming Conventions

- Database table names: `snake_case`, schema-qualified — e.g., `"account"."account"`
- Enum values stored as strings in DB (use `.HasConversion<string>()`)

## DTOs

- Use `record` types with positional parameters and `DataAnnotations`.
- Requests → `Dto/Request/`, Responses → `Dto/Response/`.

## Entity Configuration (EF Core)

- Use `IEntityTypeConfiguration<T>` Fluent API — never data annotations on entities.
- Place in `Domain/{Name}/Entity/Config/`.
- `AppDbContext` applies all configs automatically via `ApplyConfigurationsFromAssembly`.

```csharp
public class AccountConfig : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("account", "account");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Role).HasConversion<string>();
    }
}
```

## Domain DI Registration

Each domain exposes a single static extension method in `Config/`:

```csharp
public static class AccountServiceConfig
{
    public static IServiceCollection AddAccountServices(this IServiceCollection services)
    {
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ICreateAccountService, CreateAccountService>();
        return services;
    }
}
```

Call all domain configs from `Program.cs`.

## Password Hashing

Always use BCrypt — never store plain text passwords.

```csharp
// Hash
var hashed = BCrypt.Net.BCrypt.HashPassword(request.Password);

// Verify
bool valid = BCrypt.Net.BCrypt.Verify(request.Password, account.Password);
```
