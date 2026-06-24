using Fantasy.Server.Domain.Account.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fantasy.Server.Domain.Account.Entity.Config;

public class AccountConfig : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("account", "account");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .ValueGeneratedOnAdd();

        builder.Property(a => a.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(a => a.Email)
            .IsUnique();

        builder.Property(a => a.Password)
            .IsRequired();

        builder.Property(a => a.Role)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(a => a.IsNewAccount)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(a => a.CreatedAt)
            .IsRequired()
            .ValueGeneratedOnAdd();

        builder.Property(a => a.UpdatedAt)
            .IsRequired();
    }
}
