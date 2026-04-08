using Fantasy.Server.Domain.GameData.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fantasy.Server.Domain.GameData.Entity.Config;

public class WeaponDataConfig : IEntityTypeConfiguration<WeaponData>
{
    public void Configure(EntityTypeBuilder<WeaponData> builder)
    {
        builder.ToTable("weapon_data", "game_data");

        builder.HasKey(w => w.WeaponId);

        builder.Property(w => w.WeaponId).ValueGeneratedNever();
        builder.Property(w => w.Name).IsRequired().HasMaxLength(50);
        builder.Property(w => w.Grade).IsRequired().HasConversion<string>();
        builder.Property(w => w.JobType).IsRequired().HasConversion<string>();
        builder.Property(w => w.BaseAtk).IsRequired();
        builder.Property(w => w.AtkPerEnhancement).IsRequired();
    }
}
