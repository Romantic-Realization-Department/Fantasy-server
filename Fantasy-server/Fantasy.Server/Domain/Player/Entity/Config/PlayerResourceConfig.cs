using Fantasy.Server.Domain.Player.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fantasy.Server.Domain.Player.Entity.Config;

public class PlayerResourceConfig : IEntityTypeConfiguration<PlayerResource>
{
    public void Configure(EntityTypeBuilder<PlayerResource> builder)
    {
        builder.ToTable("player_resource", "player");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedOnAdd();

        builder.Property(r => r.PlayerId)
            .IsRequired();

        builder.Property(r => r.Gold)
            .IsRequired()
            .HasDefaultValue(0L);

        builder.Property(r => r.EnhancementScroll)
            .IsRequired()
            .HasDefaultValue(0L);

        builder.Property(r => r.Mithril)
            .IsRequired()
            .HasDefaultValue(0L);

        builder.Property(r => r.Sp)
            .IsRequired()
            .HasDefaultValue(0L);

        builder.Property(r => r.UpdatedAt)
            .IsRequired();

        builder.HasIndex(r => r.PlayerId)
            .IsUnique();

        builder.HasOne<Player>()
            .WithOne()
            .HasForeignKey<PlayerResource>(r => r.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}