using Fantasy.Server.Domain.Account.Entity;
using Fantasy.Server.Domain.Player.Entity;
using Microsoft.EntityFrameworkCore;

namespace Fantasy.Server.Global.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<PlayerResource> PlayerResources => Set<PlayerResource>();
    public DbSet<PlayerStage> PlayerStages => Set<PlayerStage>();
    public DbSet<PlayerSession> PlayerSessions => Set<PlayerSession>();
    public DbSet<PlayerWeapon> PlayerWeapons => Set<PlayerWeapon>();
    public DbSet<PlayerSkill> PlayerSkills => Set<PlayerSkill>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
