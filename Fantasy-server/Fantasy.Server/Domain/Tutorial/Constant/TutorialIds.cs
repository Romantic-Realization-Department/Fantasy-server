namespace Fantasy.Server.Domain.Tutorial.Constant;

public static class TutorialIds
{
    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        "tutorial_first_game_start",
        "tutorial_first_dungeon",
        "tutorial_first_upgrade"
    };
}
