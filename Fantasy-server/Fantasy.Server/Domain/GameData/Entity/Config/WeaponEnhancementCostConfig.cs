using Fantasy.Server.Domain.GameData.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fantasy.Server.Domain.GameData.Entity.Config;

public class WeaponEnhancementCostConfig : IEntityTypeConfiguration<WeaponEnhancementCost>
{
    public void Configure(EntityTypeBuilder<WeaponEnhancementCost> builder)
    {
        builder.ToTable("weapon_enhancement_cost", "game_data");
        builder.HasKey(c => new { c.WeaponId, c.EnhancementLevel });
        builder.Property(c => c.WeaponId).ValueGeneratedNever();
        builder.Property(c => c.EnhancementLevel).ValueGeneratedNever();
        builder.Property(c => c.RequiredGold).IsRequired();
        builder.Property(c => c.RequiredScroll).IsRequired();
    }
}
