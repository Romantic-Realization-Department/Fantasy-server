using Fantasy.Server.Domain.Player.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fantasy.Server.Domain.Player.Entity.Config;

public class PlayerSessionConfig : IEntityTypeConfiguration<PlayerSession>
{
    public void Configure(EntityTypeBuilder<PlayerSession> builder)
    {
        builder.ToTable("player_session", "player");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedOnAdd();

        builder.Property(s => s.PlayerId)
            .IsRequired();

        builder.Property(s => s.LastWeaponId);

        builder.Property(s => s.ActiveSkills)
            .IsRequired()
            .HasDefaultValueSql("ARRAY[]::integer[]");

        builder.Property(s => s.UpdatedAt)
            .IsRequired();

        builder.HasIndex(s => s.PlayerId)
            .IsUnique();

        builder.HasOne<Player>()
            .WithOne()
            .HasForeignKey<PlayerSession>(s => s.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
