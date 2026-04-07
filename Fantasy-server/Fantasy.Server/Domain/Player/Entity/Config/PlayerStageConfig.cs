using Fantasy.Server.Domain.Player.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fantasy.Server.Domain.Player.Entity.Config;

public class PlayerStageConfig : IEntityTypeConfiguration<PlayerStage>
{
    public void Configure(EntityTypeBuilder<PlayerStage> builder)
    {
        builder.ToTable("player_stage", "player");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedOnAdd();

        builder.Property(s => s.PlayerId)
            .IsRequired();

        builder.Property(s => s.MaxStage)
            .IsRequired()
            .HasDefaultValue(1L);

        builder.HasIndex(s => s.PlayerId)
            .IsUnique();

        builder.HasOne<Player>()
            .WithOne()
            .HasForeignKey<PlayerStage>(s => s.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
