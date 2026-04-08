using System.Data;
using Fantasy.Server.Domain.GameData.Entity;
using Fantasy.Server.Domain.Player.Entity;
using Fantasy.Server.Domain.Player.Enum;
using Fantasy.Server.Global.Infrastructure;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using PlayerEntity = Fantasy.Server.Domain.Player.Entity.Player;

namespace Fantasy.Test.Global.Infrastructure;

public class AppDbTransactionRunnerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _dbContext;
    private readonly AppDbTransactionRunner _sut;

    public AppDbTransactionRunnerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new TestAppDbContext(options);
        _dbContext.Database.EnsureCreated();
        _sut = new AppDbTransactionRunner(_dbContext);
    }

    [Fact]
    public async Task ExecuteAsync_예외가_발생하면_롤백한다()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var act = async () => await _sut.ExecuteAsync(async () =>
        {
            await _dbContext.Players.AddAsync(PlayerEntity.Create(1L, JobType.Warrior), cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("rollback");
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("rollback");
        var count = await _dbContext.Players.CountAsync(cancellationToken);
        count.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsyncT_결과를_반환한다()
    {
        var result = await _sut.ExecuteAsync(async () =>
        {
            await Task.CompletedTask;
            return 42;
        });

        result.Should().Be(42);
    }

    [Fact]
    public async Task ExecuteAsync_중첩_호출이면_기존_트랜잭션을_재사용한다()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await _sut.ExecuteAsync(async () =>
        {
            var outerTransaction = _dbContext.Database.CurrentTransaction;

            await _sut.ExecuteAsync(async () =>
            {
                _dbContext.Database.CurrentTransaction.Should().BeSameAs(outerTransaction);
                await _dbContext.Players.AddAsync(PlayerEntity.Create(2L, JobType.Archer), cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }, IsolationLevel.Serializable);
        });

        var count = await _dbContext.Players.CountAsync(cancellationToken);
        count.Should().Be(1);
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

            modelBuilder.Entity<PlayerEntity>(entity =>
            {
                entity.ToTable("players");
                entity.HasKey(player => player.Id);
                entity.Property(player => player.Id).ValueGeneratedOnAdd();
                entity.Property(player => player.AccountId).IsRequired();
                entity.Property(player => player.JobType).HasConversion<string>().IsRequired();
                entity.Property(player => player.Level).IsRequired();
                entity.Property(player => player.Exp).IsRequired();
                entity.Property(player => player.CreatedAt).IsRequired();
                entity.Property(player => player.UpdatedAt).IsRequired();
            });
        }
    }
}
