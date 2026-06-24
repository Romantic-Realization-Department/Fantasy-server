using Fantasy.Server.Domain.GameData.Entity;
using Fantasy.Server.Domain.GameData.Service.Interface;
using Fantasy.Server.Domain.LevelUp.Service;
using Fantasy.Server.Domain.Player.Entity;
using Fantasy.Server.Domain.Player.Enum;
using FluentAssertions;
using NSubstitute;
using Xunit;
using PlayerEntity = Fantasy.Server.Domain.Player.Entity.Player;

namespace Fantasy.Test.LevelUp.Service;

public class LevelUpServiceTests
{
    private static readonly Dictionary<long, LevelTable> SampleLevelTable = new()
    {
        [1] = LevelTable.Create(1, requiredExp: 100, rewardSp: 3),
        [2] = LevelTable.Create(2, requiredExp: 200, rewardSp: 5),
        [3] = LevelTable.Create(3, requiredExp: 400, rewardSp: 7),
    };

    private static PlayerEntity MakePlayer(long level = 1, long exp = 0)
    {
        var p = PlayerEntity.Create(1L, JobType.Warrior);
        p.UpdateLevel(level);
        p.UpdateExp(exp);
        return p;
    }

    private static PlayerResource MakeResource() => PlayerResource.Create(1L);

    public class XP가_부족할_때
    {
        private readonly IGameDataCacheService _cache = Substitute.For<IGameDataCacheService>();
        private readonly LevelUpService _sut;

        public XP가_부족할_때()
        {
            _cache.GetLevelTableAsync().Returns(SampleLevelTable);
            _sut = new LevelUpService(_cache);
        }

        [Fact]
        public async Task 레벨업이_발생하지_않는다()
        {
            var player = MakePlayer(level: 1, exp: 0);
            var resource = MakeResource();

            var result = await _sut.ExecuteAsync(player, resource, earnedExp: 50);

            result.Should().BeEmpty();
            player.Level.Should().Be(1);
        }

        [Fact]
        public async Task XP가_누적된다()
        {
            var player = MakePlayer(level: 1, exp: 0);
            var resource = MakeResource();

            await _sut.ExecuteAsync(player, resource, earnedExp: 50);

            player.Exp.Should().Be(50);
        }
    }

    public class 정확한_XP로_레벨업할_때
    {
        private readonly IGameDataCacheService _cache = Substitute.For<IGameDataCacheService>();
        private readonly LevelUpService _sut;

        public 정확한_XP로_레벨업할_때()
        {
            _cache.GetLevelTableAsync().Returns(SampleLevelTable);
            _sut = new LevelUpService(_cache);
        }

        [Fact]
        public async Task 레벨이_1_증가한다()
        {
            var player = MakePlayer(level: 1, exp: 0);
            var resource = MakeResource();

            await _sut.ExecuteAsync(player, resource, earnedExp: 100);

            player.Level.Should().Be(2);
        }

        [Fact]
        public async Task 레벨업_결과가_반환된다()
        {
            var player = MakePlayer(level: 1, exp: 0);
            var resource = MakeResource();

            var result = await _sut.ExecuteAsync(player, resource, earnedExp: 100);

            result.Should().HaveCount(1);
            result[0].NewLevel.Should().Be(2);
            result[0].EarnedSp.Should().Be(3);
        }

        [Fact]
        public async Task SP가_지급된다()
        {
            var player = MakePlayer(level: 1, exp: 0);
            var resource = MakeResource();

            await _sut.ExecuteAsync(player, resource, earnedExp: 100);

            resource.Sp.Should().Be(3);
        }

        [Fact]
        public async Task 레벨업_후_남은_XP가_0이다()
        {
            var player = MakePlayer(level: 1, exp: 0);
            var resource = MakeResource();

            await _sut.ExecuteAsync(player, resource, earnedExp: 100);

            player.Exp.Should().Be(0);
        }
    }

    public class 초과_XP로_연속_레벨업할_때
    {
        private readonly IGameDataCacheService _cache = Substitute.For<IGameDataCacheService>();
        private readonly LevelUpService _sut;

        public 초과_XP로_연속_레벨업할_때()
        {
            _cache.GetLevelTableAsync().Returns(SampleLevelTable);
            _sut = new LevelUpService(_cache);
        }

        [Fact]
        public async Task 레벨이_2_증가한다()
        {
            var player = MakePlayer(level: 1, exp: 0);
            var resource = MakeResource();

            // 레벨1 → 2 (100 XP), 레벨2 → 3 (200 XP) = 300 XP
            await _sut.ExecuteAsync(player, resource, earnedExp: 300);

            player.Level.Should().Be(3);
        }

        [Fact]
        public async Task 레벨업_결과_목록이_순서대로_반환된다()
        {
            var player = MakePlayer(level: 1, exp: 0);
            var resource = MakeResource();

            var result = await _sut.ExecuteAsync(player, resource, earnedExp: 300);

            result.Should().HaveCount(2);
            result[0].NewLevel.Should().Be(2);
            result[1].NewLevel.Should().Be(3);
        }

        [Fact]
        public async Task 초과된_XP가_다음_레벨에_누적된다()
        {
            var player = MakePlayer(level: 1, exp: 0);
            var resource = MakeResource();

            // 100 (lv1→2) + 200 (lv2→3) + 50 남음
            await _sut.ExecuteAsync(player, resource, earnedExp: 350);

            player.Exp.Should().Be(50);
        }

        [Fact]
        public async Task SP가_레벨업마다_지급된다()
        {
            var player = MakePlayer(level: 1, exp: 0);
            var resource = MakeResource();

            await _sut.ExecuteAsync(player, resource, earnedExp: 300);

            resource.Sp.Should().Be(3 + 5); // lv1 reward + lv2 reward
        }
    }

    public class 기존_XP가_있을_때
    {
        private readonly IGameDataCacheService _cache = Substitute.For<IGameDataCacheService>();
        private readonly LevelUpService _sut;

        public 기존_XP가_있을_때()
        {
            _cache.GetLevelTableAsync().Returns(SampleLevelTable);
            _sut = new LevelUpService(_cache);
        }

        [Fact]
        public async Task 기존_XP와_합산하여_레벨업을_판정한다()
        {
            var player = MakePlayer(level: 1, exp: 80);
            var resource = MakeResource();

            // 80 + 30 = 110 >= 100 → 레벨업
            var result = await _sut.ExecuteAsync(player, resource, earnedExp: 30);

            result.Should().HaveCount(1);
            player.Level.Should().Be(2);
        }
    }
}
