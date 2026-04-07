using Fantasy.Server.Domain.Player.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fantasy.Server.Domain.Player.Entity.Config;

public class PlayerSkillConfig : IEntityTypeConfiguration<PlayerSkill>
{
    public void Configure(EntityTypeBuilder<PlayerSkill> builder)
    {
        builder.ToTable("player_skill", "player");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedOnAdd();

        builder.Property(s => s.PlayerId)
            .IsRequired();

        builder.Property(s => s.SkillId)
            .IsRequired();

        builder.Property(s => s.IsUnlocked)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(s => new { s.PlayerId, s.SkillId })
            .IsUnique();

        builder.HasOne<Player>()
            .WithMany()
            .HasForeignKey(s => s.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
