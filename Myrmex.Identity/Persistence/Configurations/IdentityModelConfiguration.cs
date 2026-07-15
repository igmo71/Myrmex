using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Myrmex.Identity.Persistence.Configurations;

internal static class IdentityModelConfiguration
{
    private const string Schema = "identity";

    public static void ConfigureIdentityModel(this ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<AppUser>(builder =>
        {
            builder.ToTable("Users", Schema);

            builder.Property(user => user.Email)
                .HasMaxLength(256)
                .IsRequired();
            builder.Property(user => user.NormalizedEmail)
                .HasMaxLength(256)
                .IsRequired();
            builder.Property(user => user.UserName)
                .HasMaxLength(256)
                .IsRequired();
            builder.Property(user => user.NormalizedUserName)
                .HasMaxLength(256)
                .IsRequired();
            builder.Property(user => user.DisplayName)
                .HasMaxLength(AppUser.MaxDisplayNameLength)
                .IsRequired(false);

            builder.HasIndex(user => user.NormalizedEmail)
                .IsUnique()
                .HasDatabaseName("EmailIndex");
            builder.HasIndex(user => user.NormalizedUserName)
                .IsUnique()
                .HasDatabaseName("UserNameIndex");
        });

        modelBuilder.Entity<AppRole>(builder =>
        {
            builder.ToTable("Roles", Schema);
            builder.Property(role => role.Name)
                .HasMaxLength(256)
                .IsRequired();
            builder.Property(role => role.NormalizedName)
                .HasMaxLength(256)
                .IsRequired();
        });

        modelBuilder.Entity<IdentityUserClaim<Guid>>()
            .ToTable("UserClaims", Schema);
        modelBuilder.Entity<IdentityUserLogin<Guid>>()
            .ToTable("UserLogins", Schema);
        modelBuilder.Entity<IdentityUserToken<Guid>>()
            .ToTable("UserTokens", Schema);
        modelBuilder.Entity<IdentityRoleClaim<Guid>>()
            .ToTable("RoleClaims", Schema);

        modelBuilder.Entity<IdentityUserRole<Guid>>(builder =>
        {
            builder.ToTable("UserRoles", Schema);
            builder.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(userRole => userRole.UserId)
                .IsRequired();
            builder.HasOne<AppRole>()
                .WithMany()
                .HasForeignKey(userRole => userRole.RoleId)
                .IsRequired();
        });

        modelBuilder.Entity<DataProtectionKey>(builder =>
        {
            builder.ToTable("DataProtectionKeys", Schema);
            builder.Property(key => key.Xml)
                .IsRequired();
        });
    }
}
