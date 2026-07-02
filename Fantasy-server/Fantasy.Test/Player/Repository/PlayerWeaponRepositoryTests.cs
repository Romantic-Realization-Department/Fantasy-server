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
    public async Task GrantWeaponsAsync_기존_무기가_있으면_Count만_증가하고_강화각성은_유지한다()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await _dbContext.PlayerWeapons.AddAsync(PlayerWeapon.Create(1L, 1, 2L, 4L, 1L), cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _sut.GrantWeaponsAsync(1L, [1]);

        PlayerWeapon saved = await _dbContext.PlayerWeapons
            .FirstAsync(weapon => weapon.WeaponId == 1, cancellationToken);

        saved.Count.Should().Be(3L);
        saved.EnhancementLevel.Should().Be(4L);
        saved.AwakeningCount.Should().Be(1L);
    }

    [Fact]
    public async Task GrantWeaponsAsync_보유하지_않은_무기면_기본값으로_생성된다()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await _sut.GrantWeaponsAsync(1L, [5]);

        PlayerWeapon saved = await _dbContext.PlayerWeapons
            .FirstAsync(weapon => weapon.WeaponId == 5, cancellationToken);

        saved.Count.Should().Be(1L);
        saved.EnhancementLevel.Should().Be(0L);
        saved.AwakeningCount.Should().Be(0L);
    }

    [Fact]
    public async Task GrantWeaponsAsync_보유하지_않은_무기가_목록에_중복되면_Count가_2로_생성된다()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await _sut.GrantWeaponsAsync(1L, [7, 7]);

        PlayerWeapon saved = await _dbContext.PlayerWeapons
            .FirstAsync(weapon => weapon.WeaponId == 7, cancellationToken);

        saved.Count.Should().Be(2L);
        saved.EnhancementLevel.Should().Be(0L);
        saved.AwakeningCount.Should().Be(0L);
    }

    [Fact]
    public async Task GrantWeaponsAsync_기존과_신규가_섞여있으면_각각_올바르게_처리된다()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await _dbContext.PlayerWeapons.AddAsync(PlayerWeapon.Create(1L, 1, 2L, 3L, 2L), cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _sut.GrantWeaponsAsync(1L, [1, 9]);

        List<PlayerWeapon> saved = await _dbContext.PlayerWeapons
            .OrderBy(weapon => weapon.WeaponId)
            .ToListAsync(cancellationToken);

        saved.Should().HaveCount(2);
        saved[0].WeaponId.Should().Be(1);
        saved[0].Count.Should().Be(3L);
        saved[0].EnhancementLevel.Should().Be(3L);
        saved[0].AwakeningCount.Should().Be(2L);
        saved[1].WeaponId.Should().Be(9);
        saved[1].Count.Should().Be(1L);
        saved[1].EnhancementLevel.Should().Be(0L);
        saved[1].AwakeningCount.Should().Be(0L);
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
