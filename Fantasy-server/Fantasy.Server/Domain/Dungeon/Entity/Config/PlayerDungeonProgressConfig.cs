using Fantasy.Server.Domain.Dungeon.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlayerEntity = Fantasy.Server.Domain.Player.Entity.Player;

namespace Fantasy.Server.Domain.Dungeon.Entity.Config;

public class PlayerDungeonProgressConfig : IEntityTypeConfiguration<PlayerDungeonProgress>
{
    public void Configure(EntityTypeBuilder<PlayerDungeonProgress> builder)
    {
        builder.ToTable("player_dungeon_progress", "dungeon");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedOnAdd();

        builder.Property(p => p.PlayerId)
            .IsRequired();

        builder.Property(p => p.DungeonType)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(p => p.HighestClearedStage)
            .IsRequired()
            .HasDefaultValue(1L);

        builder.Property(p => p.HighScore)
            .IsRequired()
            .HasDefaultValue(0L);

        builder.Property(p => p.LastClearedAt)
            .IsRequired(false);

        builder.HasIndex(p => new { p.PlayerId, p.DungeonType })
            .IsUnique();

        builder.HasOne<PlayerEntity>()
            .WithMany()
            .HasForeignKey(p => p.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
    }
}
