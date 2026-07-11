using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Myrmex.Identity.Persistence.Configurations;

namespace Myrmex.Identity.Persistence;

public sealed class MyrmexIdentityDbContext(
    DbContextOptions<MyrmexIdentityDbContext> options)
    : IdentityDbContext<MyrmexUser, MyrmexRole, Guid>(options),
        IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ConfigureIdentityModel();
    }
}
