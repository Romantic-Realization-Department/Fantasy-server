using Fantasy.Server.Domain.GameData.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fantasy.Server.Domain.GameData.Entity.Config;

public class StageDataConfig : IEntityTypeConfiguration<StageData>
{
    public void Configure(EntityTypeBuilder<StageData> builder)
    {
        builder.ToTable("stage_data", "game_data");

        builder.HasKey(s => s.Stage);

        builder.Property(s => s.Stage).ValueGeneratedNever();
        builder.Property(s => s.MonsterHp).IsRequired();
        builder.Property(s => s.MonsterAtk).IsRequired();
        builder.Property(s => s.XpPerSecond).IsRequired();
        builder.Property(s => s.GoldPerSecond).IsRequired();
        builder.Property(s => s.IsBossStage).IsRequired().HasDefaultValue(false);
    }
}
