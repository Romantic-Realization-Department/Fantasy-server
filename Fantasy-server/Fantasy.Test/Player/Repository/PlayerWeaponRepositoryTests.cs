using Fantasy.Server.Domain.Player.Dto.Request;
using Fantasy.Server.Domain.Player.Entity;
using Fantasy.Server.Domain.Player.Repository;
using Fantasy.Server.Global.Infrastructure;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Fantasy.Test.Player.Repository;

public class PlayerWeaponRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _dbContext;
    private readonly PlayerWeaponRepository _sut;

    public PlayerWeaponRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new TestAppDbContext(options);
        _dbContext.Database.EnsureCreated();
        _sut = new PlayerWeaponRepository(_dbContext);
    }

    [Fact]
    public async Task UpsertRangeAsync_중복_무기_ID가_있으면_마지막_값으로_저장한다()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await _dbContext.PlayerWeapons.AddAsync(PlayerWeapon.Create(1L, 1, 2L, 1L, 0L), cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        List<WeaponChangeItem> items =
        [
            new(1, 3L, 2L, 0L),
            new(1, 7L, 4L, 1L),
            new(2, 1L, 0L, 0L)
        ];

        await _sut.UpsertRangeAsync(1L, items);

        List<PlayerWeapon> saved = await _dbContext.PlayerWeapons
            .OrderBy(weapon => weapon.WeaponId)
            .ToListAsync(cancellationToken);

        saved.Should().HaveCount(2);
        saved[0].WeaponId.Should().Be(1);
        saved[0].Count.Should().Be(7L);
        saved[0].EnhancementLevel.Should().Be(4L);
        saved[0].AwakeningCount.Should().Be(1L);
        saved[1].WeaponId.Should().Be(2);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    private sealed class TestAppDbContext : AppDbContext
    {
        public TestAppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PlayerWeapon>(entity =>
            {
                entity.ToTable("player_weapons");
                entity.HasKey(weapon => weapon.Id);
                entity.Property(weapon => weapon.Id).ValueGeneratedOnAdd();
                entity.Property(weapon => weapon.PlayerId).IsRequired();
                entity.Property(weapon => weapon.WeaponId).IsRequired();
                entity.Property(weapon => weapon.Count).IsRequired();
                entity.Property(weapon => weapon.EnhancementLevel).IsRequired();
                entity.Property(weapon => weapon.AwakeningCount).IsRequired();
                entity.HasIndex(weapon => new { weapon.PlayerId, weapon.WeaponId }).IsUnique();
            });
        }
    }
}
