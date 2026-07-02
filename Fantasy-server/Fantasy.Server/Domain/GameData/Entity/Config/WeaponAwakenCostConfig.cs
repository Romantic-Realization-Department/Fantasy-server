using Fantasy.Server.Domain.GameData.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fantasy.Server.Domain.GameData.Entity.Config;

public class WeaponAwakenCostConfig : IEntityTypeConfiguration<WeaponAwakenCost>
{
    public void Configure(EntityTypeBuilder<WeaponAwakenCost> builder)
    {
        builder.ToTable("weapon_awaken_cost", "game_data");
        builder.HasKey(c => new { c.WeaponId, c.AwakeningLevel });
        builder.Property(c => c.WeaponId).ValueGeneratedNever();
        builder.Property(c => c.AwakeningLevel).ValueGeneratedNever();
        builder.Property(c => c.RequiredCount).IsRequired();
        builder.Property(c => c.RequiredMithril).IsRequired();
    }
}
