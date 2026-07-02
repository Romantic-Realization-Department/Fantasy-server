using Fantasy.Server.Domain.GameData.Entity;
using Fantasy.Server.Domain.Player.Constant;
using Fantasy.Server.Domain.Player.Entity;
using Fantasy.Server.Domain.Player.Repository;
using Fantasy.Server.Global.Infrastructure;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Fantasy.Test.Player.Repository;

public class RewardTransactionRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _dbContext;
    private readonly RewardTransactionRepository _sut;

    public RewardTransactionRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new TestAppDbContext(options);
        _dbContext.Database.EnsureCreated();
        _sut = new RewardTransactionRepository(_dbContext);
    }

    [Fact]
    public async Task SaveRangeAsync_여러_건을_한_번에_저장한다()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await _sut.SaveRangeAsync([
            RewardTransaction.Create(1L, RewardSourceTypes.DungeonGold, "run-1", RewardTypes.Gold, null, 500L),
            RewardTransaction.Create(1L, RewardSourceTypes.WeaponUpgrade, null, RewardTypes.Gold, null, -100L)
        ]);

        var saved = await _dbContext.RewardTransactions.ToListAsync(cancellationToken);

        saved.Should().HaveCount(2);
        saved.Should().ContainSingle(t => t.Amount == -100L && t.SourceType == "weapon_upgrade");
        saved.Should().ContainSingle(t => t.SourceRefId == "run-1" && t.Amount == 500L);
    }

    [Fact]
    public async Task SaveRangeAsync_빈_리스트면_저장하지_않는다()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await _sut.SaveRangeAsync([]);

        var saved = await _dbContext.RewardTransactions.ToListAsync(cancellationToken);
        saved.Should().BeEmpty();
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

            modelBuilder.Entity<RewardTransaction>(entity =>
            {
                entity.ToTable("reward_transaction");
                entity.HasKey(t => t.Id);
                entity.Property(t => t.PlayerId).IsRequired();
                entity.Property(t => t.SourceType).IsRequired();
                entity.Property(t => t.RewardType).IsRequired();
                entity.Property(t => t.Amount).IsRequired();
            });
        }
    }
}
