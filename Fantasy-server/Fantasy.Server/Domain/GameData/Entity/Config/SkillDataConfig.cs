using Fantasy.Server.Domain.GameData.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fantasy.Server.Domain.GameData.Entity.Config;

public class SkillDataConfig : IEntityTypeConfiguration<SkillData>
{
    public void Configure(EntityTypeBuilder<SkillData> builder)
    {
        builder.ToTable("skill_data", "game_data");

        builder.HasKey(s => s.SkillId);

        builder.Property(s => s.SkillId).ValueGeneratedNever();
        builder.Property(s => s.JobType).IsRequired().HasConversion<string>();
        builder.Property(s => s.IsActive).IsRequired();
        builder.Property(s => s.SpCost).IsRequired();
        builder.Property(s => s.PrereqSkillId);
        builder.Property(s => s.EffectType).IsRequired().HasConversion<string>();
        builder.Property(s => s.EffectValue).IsRequired();
    }
}
