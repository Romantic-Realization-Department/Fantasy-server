using Fantasy.Server.Domain.Player.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlayerEntity = Fantasy.Server.Domain.Player.Entity.Player;

namespace Fantasy.Server.Domain.Player.Entity.Config;

public class RewardTransactionConfig : IEntityTypeConfiguration<RewardTransaction>
{
    public void Configure(EntityTypeBuilder<RewardTransaction> builder)
    {
        builder.ToTable("reward_transaction", "player");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.PlayerId).IsRequired();
        builder.Property(t => t.SourceType).IsRequired().HasMaxLength(30);
        builder.Property(t => t.SourceRefId).HasMaxLength(50);
        builder.Property(t => t.RewardType).IsRequired().HasMaxLength(30);
        builder.Property(t => t.RewardRefId).HasMaxLength(50);
        builder.Property(t => t.Amount).IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();

        builder.HasIndex(t => new { t.PlayerId, t.CreatedAt });

        builder.HasOne<PlayerEntity>()
            .WithMany()
            .HasForeignKey(t => t.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
