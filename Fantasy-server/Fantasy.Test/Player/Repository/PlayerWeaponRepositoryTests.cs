using Fantasy.Server.Domain.Player.Dto.Request;
using Fantasy.Server.Domain.GameData.Entity;
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

    [Fact]
    public async Task FindByPlayerIdAndWeaponIdAsync_없으면_null을_반환한다()
    {
        var result = await _sut.FindByPlayerIdAndWeaponIdAsync(1L, 999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveAsync_저장한_무기를_단건_조회할_수_있다()
    {
        await _sut.SaveAsync(PlayerWeapon.Create(1L, 1001, 3L, 0L, 0L));

        var result = await _sut.FindByPlayerIdAndWeaponIdAsync(1L, 1001);

        result.Should().NotBeNull();
        result!.Count.Should().Be(3L);
    }

    [Fact]
    public async Task UpdateAsync_엔티티_메서드로_변경한_값이_반영된다()
    {
        var weapon = PlayerWeapon.Create(1L, 1001, 5L, 0L, 0L);
        await _sut.SaveAsync(weapon);

        weapon.ConsumeCount(3L);
        weapon.Enhance();
        weapon.Awaken();
        weapon.AddCount(1L);
        await _sut.UpdateAsync(weapon);

        var result = await _sut.FindByPlayerIdAndWeaponIdAsync(1L, 1001);
        result!.Count.Should().Be(3L);
        result.EnhancementLevel.Should().Be(1L);
        result.AwakeningCount.Should().Be(1L);
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
            modelBuilder.Ignore<JobBaseStat>();
            modelBuilder.Ignore<LevelTable>();
            modelBuilder.Ignore<StageData>();
            modelBuilder.Ignore<WeaponData>();
            modelBuilder.Ignore<SkillData>();
            modelBuilder.Ignore<WeaponEnhancementCost>();
            modelBuilder.Ignore<WeaponAwakenCost>();

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
