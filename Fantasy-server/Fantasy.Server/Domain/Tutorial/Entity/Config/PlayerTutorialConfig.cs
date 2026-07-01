using Fantasy.Server.Domain.Tutorial.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlayerEntity = Fantasy.Server.Domain.Player.Entity.Player;

namespace Fantasy.Server.Domain.Tutorial.Entity.Config;

public class PlayerTutorialConfig : IEntityTypeConfiguration<PlayerTutorial>
{
    public void Configure(EntityTypeBuilder<PlayerTutorial> builder)
    {
        builder.ToTable("player_tutorial", "tutorial");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .ValueGeneratedOnAdd();

        builder.Property(t => t.PlayerId)
            .IsRequired();

        builder.Property(t => t.TutorialId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.CompletedAt)
            .IsRequired();

        builder.HasIndex(t => new { t.PlayerId, t.TutorialId })
            .IsUnique();

        builder.HasOne<PlayerEntity>()
            .WithMany()
            .HasForeignKey(t => t.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
