using Fantasy.Server.Domain.GameData.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fantasy.Server.Domain.GameData.Entity.Config;

public class JobBaseStatConfig : IEntityTypeConfiguration<JobBaseStat>
{
    public void Configure(EntityTypeBuilder<JobBaseStat> builder)
    {
        builder.ToTable("job_base_stat", "game_data");

        builder.HasKey(j => j.JobType);

        builder.Property(j => j.JobType).IsRequired().HasConversion<string>();
        builder.Property(j => j.BaseHp).IsRequired();
        builder.Property(j => j.BaseAtk).IsRequired();
        builder.Property(j => j.CritRate).IsRequired();
        builder.Property(j => j.CritDmgMultiplier).IsRequired();
        builder.Property(j => j.HpPerLevel).IsRequired();
        builder.Property(j => j.AtkPerLevel).IsRequired();
    }
}
