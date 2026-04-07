using Fantasy.Server.Domain.Player.Entity;
using Fantasy.Server.Domain.Player.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fantasy.Server.Domain.Player.Entity.Config;

public class PlayerConfig : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.ToTable("player", "player");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedOnAdd();

        builder.Property(p => p.AccountId)
            .IsRequired();

        builder.Property(p => p.JobType)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(p => p.Level)
            .IsRequired()
            .HasDefaultValue(1L);

        builder.Property(p => p.Exp)
            .IsRequired()
            .HasDefaultValue(0L);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .IsRequired();

        builder.HasIndex(p => new { p.AccountId, p.JobType })
            .IsUnique();
    }
}