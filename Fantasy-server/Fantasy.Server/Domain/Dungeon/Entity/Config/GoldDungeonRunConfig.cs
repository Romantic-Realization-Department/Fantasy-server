using Fantasy.Server.Domain.Dungeon.Entity;
using AccountEntity = Fantasy.Server.Domain.Account.Entity.Account;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fantasy.Server.Domain.Dungeon.Entity.Config;

public class GoldDungeonRunConfig : IEntityTypeConfiguration<GoldDungeonRun>
{
    public void Configure(EntityTypeBuilder<GoldDungeonRun> builder)
    {
        builder.ToTable("gold_dungeon_run", "dungeon");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedNever();

        builder.Property(r => r.AccountId)
            .IsRequired();

        builder.Property(r => r.StartedAt)
            .IsRequired();

        builder.Property(r => r.DurationSeconds)
            .IsRequired();

        builder.Property(r => r.ExpiresAt)
            .IsRequired();

        builder.Property(r => r.MaxClicks)
            .IsRequired();

        builder.Property(r => r.ClaimedAt)
            .IsRequired(false);

        builder.Property(r => r.ClaimedClicks)
            .IsRequired(false);

        builder.Property(r => r.EarnedGold)
            .IsRequired(false);

        builder.Property(r => r.EarnedMithril)
            .IsRequired(false);

        builder.HasIndex(r => r.AccountId);

        builder.HasOne<AccountEntity>()
            .WithMany()
            .HasForeignKey(r => r.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
    }
}
