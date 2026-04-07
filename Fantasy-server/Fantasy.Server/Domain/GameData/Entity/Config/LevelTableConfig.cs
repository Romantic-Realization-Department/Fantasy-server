using Fantasy.Server.Domain.GameData.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fantasy.Server.Domain.GameData.Entity.Config;

public class LevelTableConfig : IEntityTypeConfiguration<LevelTable>
{
    public void Configure(EntityTypeBuilder<LevelTable> builder)
    {
        builder.ToTable("level_table", "game_data");

        builder.HasKey(l => l.Level);

        builder.Property(l => l.Level).IsRequired().ValueGeneratedNever();
        builder.Property(l => l.RequiredExp).IsRequired();
        builder.Property(l => l.RewardSp).IsRequired();
    }
}
