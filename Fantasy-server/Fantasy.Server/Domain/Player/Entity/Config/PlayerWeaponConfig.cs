using Fantasy.Server.Domain.Player.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fantasy.Server.Domain.Player.Entity.Config;

public class PlayerWeaponConfig : IEntityTypeConfiguration<PlayerWeapon>
{
    public void Configure(EntityTypeBuilder<PlayerWeapon> builder)
    {
        builder.ToTable("player_weapon", "player");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Id)
            .ValueGeneratedOnAdd();

        builder.Property(w => w.PlayerId)
            .IsRequired();

        builder.Property(w => w.WeaponId)
            .IsRequired();

        builder.Property(w => w.Count)
            .IsRequired()
            .HasDefaultValue(0L);

        builder.Property(w => w.EnhancementLevel)
            .IsRequired()
            .HasDefaultValue(0L);

        builder.Property(w => w.AwakeningCount)
            .IsRequired()
            .HasDefaultValue(0L);

        builder.HasIndex(w => new { w.PlayerId, w.WeaponId })
            .IsUnique();

        builder.HasOne<Player>()
            .WithMany()
            .HasForeignKey(w => w.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
