namespace Fantasy.Server.Domain.Player.Entity;

public class PlayerResource
{
    public long Id { get; private set; }
    public long PlayerId { get; private set; }
    public long Gold { get; private set; }
    public long EnhancementScroll { get; private set; }
    public long Mithril { get; private set; }
    public long Sp { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static PlayerResource Create(long playerId) => new()
    {
        PlayerId = playerId,
        Gold = 0,
        EnhancementScroll = 0,
        Mithril = 0,
        Sp = 0,
        UpdatedAt = DateTime.UtcNow
    };

    public void UpdateGold(long gold)
    {
        Gold = gold;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateChangeData(long? enhancementScroll, long? mithril, long? sp)
    {
        if (enhancementScroll.HasValue) EnhancementScroll = enhancementScroll.Value;
        if (mithril.HasValue) Mithril = mithril.Value;
        if (sp.HasValue) Sp = sp.Value;
        UpdatedAt = DateTime.UtcNow;
    }
}