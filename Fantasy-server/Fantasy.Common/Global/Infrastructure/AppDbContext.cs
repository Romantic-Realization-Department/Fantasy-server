using Fantasy.Common.Domain.Account.Entity;
using Microsoft.EntityFrameworkCore;

namespace Fantasy.Common.Global.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        :base(options)
    {
    }
    
    public DbSet<Account> Accounts => Set<Account>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}