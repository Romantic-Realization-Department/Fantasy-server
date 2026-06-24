using Fantasy.Server.Domain.Dungeon.Entity;
using AccountEntity = Fantasy.Server.Domain.Account.Entity.Account;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fantasy.Server.Domain.Dungeon.Entity.Config;

public class AccountDungeonTicketConfig : IEntityTypeConfiguration<AccountDungeonTicket>
{
    public void Configure(EntityTypeBuilder<AccountDungeonTicket> builder)
    {
        builder.ToTable("account_dungeon_ticket", "dungeon");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .ValueGeneratedOnAdd();

        builder.Property(t => t.AccountId)
            .IsRequired();

        builder.Property(t => t.TicketCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(t => t.LastDailyGrantDate)
            .IsRequired();

        builder.Property(t => t.DailyAdRewardClaimedDate)
            .IsRequired(false);

        builder.HasIndex(t => t.AccountId)
            .IsUnique();

        builder.HasOne<AccountEntity>()
            .WithMany()
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
    }
}
