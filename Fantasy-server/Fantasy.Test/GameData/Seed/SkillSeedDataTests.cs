using Fantasy.Server.Domain.GameData.Seed;
using FluentAssertions;
using Xunit;

namespace Fantasy.Test.GameData.Seed;

public class SkillSeedDataTests
{
    private static readonly IReadOnlyList<GameDataSeeder.SkillSeed> Skills = GameDataSeeder.LoadSkillSeeds();

    [Fact]
    public void SkillId는_유일하다()
    {
        var ids = Skills.Select(s => s.SkillId);

        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void 선행_스킬은_존재하는_SkillId를_참조한다()
    {
        var ids = Skills.Select(s => s.SkillId).ToHashSet();

        var danglingPrereqs = Skills
            .Where(s => s.PrereqSkillId.HasValue && !ids.Contains(s.PrereqSkillId.Value))
            .Select(s => s.SkillId);

        danglingPrereqs.Should().BeEmpty();
    }

    [Fact]
    public void 선행_스킬과_본_스킬은_동일_직업이다()
    {
        var byId = Skills.ToDictionary(s => s.SkillId);

        var crossJob = Skills
            .Where(s => s.PrereqSkillId.HasValue && byId.TryGetValue(s.PrereqSkillId.Value, out var prereq) && prereq.JobType != s.JobType)
            .Select(s => s.SkillId);

        crossJob.Should().BeEmpty();
    }

    [Fact]
    public void 스킬은_자기_자신을_선행으로_참조하지_않는다()
    {
        var selfReferencing = Skills
            .Where(s => s.PrereqSkillId.HasValue && s.PrereqSkillId.Value == s.SkillId)
            .Select(s => s.SkillId);

        selfReferencing.Should().BeEmpty();
    }

    [Fact]
    public void 선행_스킬_체인에_순환이_없다()
    {
        var byId = Skills.ToDictionary(s => s.SkillId);

        foreach (var skill in Skills)
        {
            var visited = new HashSet<int> { skill.SkillId };
            var currentPrereq = skill.PrereqSkillId;

            while (currentPrereq.HasValue)
            {
                visited.Add(currentPrereq.Value).Should().BeTrue(
                    "스킬 {0}의 선행 체인에 순환이 존재합니다.", skill.SkillId);

                currentPrereq = byId.TryGetValue(currentPrereq.Value, out var prereq)
                    ? prereq.PrereqSkillId
                    : null;
            }
        }
    }
}

public class GameDataSeedParseTests
{
    [Fact]
    public void LoadAllSeeds_ShouldParseSuccessfully()
    {
        var act = () => GameDataSeeder.LoadAllSeeds();

        var seeds = act.Should().NotThrow().Subject;

        seeds.JobBaseStats.Should().NotBeEmpty();
        seeds.Levels.Should().NotBeEmpty();
        seeds.Stages.Should().NotBeEmpty();
        seeds.Skills.Should().NotBeEmpty();
        seeds.Weapons.Should().NotBeEmpty();
    }
}
