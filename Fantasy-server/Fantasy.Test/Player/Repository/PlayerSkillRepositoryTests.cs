using Fantasy.Server.Domain.Player.Dto.Request;
using Fantasy.Server.Domain.Player.Entity;
using Fantasy.Server.Domain.Player.Repository;
using Fantasy.Server.Global.Infrastructure;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Fantasy.Test.Player.Repository;

public class PlayerSkillRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _dbContext;
    private readonly PlayerSkillRepository _sut;

    public PlayerSkillRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new TestAppDbContext(options);
        _dbContext.Database.EnsureCreated();
        _sut = new PlayerSkillRepository(_dbContext);
    }

    [Fact]
    public async Task UpsertRangeAsync_중복_스킬_ID가_있으면_마지막_값으로_저장한다()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await _dbContext.PlayerSkills.AddAsync(PlayerSkill.Create(1L, 1, false), cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        List<SkillChangeItem> items =
        [
            new(1, false),
            new(1, true),
            new(2, true)
        ];

        await _sut.UpsertRangeAsync(1L, items);

        List<PlayerSkill> saved = await _dbContext.PlayerSkills
            .OrderBy(skill => skill.SkillId)
            .ToListAsync(cancellationToken);

        saved.Should().HaveCount(2);
        saved[0].SkillId.Should().Be(1);
        saved[0].IsUnlocked.Should().BeTrue();
        saved[1].SkillId.Should().Be(2);
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
            modelBuilder.Entity<PlayerSkill>(entity =>
            {
                entity.ToTable("player_skills");
                entity.HasKey(skill => skill.Id);
                entity.Property(skill => skill.Id).ValueGeneratedOnAdd();
                entity.Property(skill => skill.PlayerId).IsRequired();
                entity.Property(skill => skill.SkillId).IsRequired();
                entity.Property(skill => skill.IsUnlocked).IsRequired();
                entity.HasIndex(skill => new { skill.PlayerId, skill.SkillId }).IsUnique();
            });
        }
    }
}
