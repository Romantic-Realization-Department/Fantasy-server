using Fantasy.Server.Domain.Dungeon.Entity;
using Fantasy.Server.Domain.Dungeon.Enum;
using Fantasy.Server.Domain.Dungeon.Repository;
using Fantasy.Server.Domain.GameData.Entity;
using Fantasy.Server.Global.Infrastructure;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Fantasy.Test.Dungeon.Repository;

public class PlayerDungeonProgressRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _dbContext;
    private readonly PlayerDungeonProgressRepository _sut;

    public PlayerDungeonProgressRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new TestAppDbContext(options);
        _dbContext.Database.EnsureCreated();
        _sut = new PlayerDungeonProgressRepository(_dbContext);
    }

    [Fact]
    public async Task FindByPlayerIdAndDungeonTypeAsync_존재하지_않으면_null을_반환한다()
    {
        var result = await _sut.FindByPlayerIdAndDungeonTypeAsync(1L, DungeonType.Weapon);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveAsync_저장한_레코드를_FindByPlayerIdAndDungeonTypeAsync로_조회할_수_있다()
    {
        await _sut.SaveAsync(PlayerDungeonProgress.Create(1L, DungeonType.Weapon));

        var result = await _sut.FindByPlayerIdAndDungeonTypeAsync(1L, DungeonType.Weapon);

        result.Should().NotBeNull();
        result!.HighestClearedStage.Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_변경한_값이_반영된다()
    {
        var progress = PlayerDungeonProgress.Create(1L, DungeonType.Gold);
        await _sut.SaveAsync(progress);

        progress.UpdateHighScore(500L);
        await _sut.UpdateAsync(progress);

        var result = await _sut.FindByPlayerIdAndDungeonTypeAsync(1L, DungeonType.Gold);
        result!.HighScore.Should().Be(500L);
    }

    [Fact]
    public async Task 다른_DungeonType은_독립적으로_저장된다()
    {
        await _sut.SaveAsync(PlayerDungeonProgress.Create(1L, DungeonType.Weapon));
        await _sut.SaveAsync(PlayerDungeonProgress.Create(1L, DungeonType.Boss));

        var weapon = await _sut.FindByPlayerIdAndDungeonTypeAsync(1L, DungeonType.Weapon);
        var boss = await _sut.FindByPlayerIdAndDungeonTypeAsync(1L, DungeonType.Boss);

        weapon.Should().NotBeNull();
        boss.Should().NotBeNull();
        weapon!.DungeonType.Should().Be(DungeonType.Weapon);
        boss!.DungeonType.Should().Be(DungeonType.Boss);
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

            modelBuilder.Entity<PlayerDungeonProgress>(entity =>
            {
                entity.ToTable("player_dungeon_progress");
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Id).ValueGeneratedOnAdd();
                entity.Property(p => p.PlayerId).IsRequired();
                entity.Property(p => p.DungeonType).HasConversion<string>().IsRequired();
                entity.Property(p => p.HighestClearedStage).IsRequired();
                entity.Property(p => p.HighScore).IsRequired();
                entity.HasIndex(p => new { p.PlayerId, p.DungeonType }).IsUnique();
            });
        }
    }
}
