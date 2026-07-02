namespace Fantasy.Server.Domain.Tutorial.Entity;

public class PlayerTutorial
{
    public long Id { get; private set; }
    public long PlayerId { get; private set; }
    public string TutorialId { get; private set; } = string.Empty;
    public DateTime CompletedAt { get; private set; }

    public static PlayerTutorial Create(long playerId, string tutorialId) => new()
    {
        PlayerId = playerId,
        TutorialId = tutorialId,
        CompletedAt = DateTime.UtcNow
    };
}
