using Fantasy.Server.Domain.GameData.Entity;
using Fantasy.Server.Domain.Tutorial.Entity;
using Fantasy.Server.Domain.Tutorial.Repository;
using Fantasy.Server.Global.Infrastructure;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Fantasy.Test.Tutorial.Repository;

public class PlayerTutorialRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _dbContext;
    private readonly PlayerTutorialRepository _sut;

    public PlayerTutorialRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new TestAppDbContext(options);
        _dbContext.Database.EnsureCreated();
        _sut = new PlayerTutorialRepository(_dbContext);
    }

    [Fact]
    public async Task SaveAsync_저장한_레코드를_FindAllByPlayerIdAsync로_조회할_수_있다()
    {
        await _sut.SaveAsync(PlayerTutorial.Create(1L, "tutorial_first_game_start"));

        List<PlayerTutorial> saved = await _sut.FindAllByPlayerIdAsync(1L);

        saved.Should().HaveCount(1);
        saved[0].TutorialId.Should().Be("tutorial_first_game_start");
    }

    [Fact]
    public async Task FindByPlayerIdAndTutorialIdAsync_존재하지_않으면_null을_반환한다()
    {
        var result = await _sut.FindByPlayerIdAndTutorialIdAsync(1L, "tutorial_first_dungeon");

        result.Should().BeNull();
    }

    [Fact]
    public async Task FindByPlayerIdAndTutorialIdAsync_존재하면_반환한다()
    {
        await _sut.SaveAsync(PlayerTutorial.Create(1L, "tutorial_first_dungeon"));

        var result = await _sut.FindByPlayerIdAndTutorialIdAsync(1L, "tutorial_first_dungeon");

        result.Should().NotBeNull();
        result!.PlayerId.Should().Be(1L);
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

            modelBuilder.Entity<PlayerTutorial>(entity =>
            {
                entity.ToTable("player_tutorial");
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Id).ValueGeneratedOnAdd();
                entity.Property(t => t.PlayerId).IsRequired();
                entity.Property(t => t.TutorialId).IsRequired();
                entity.Property(t => t.CompletedAt).IsRequired();
                entity.HasIndex(t => new { t.PlayerId, t.TutorialId }).IsUnique();
            });
        }
    }
}
