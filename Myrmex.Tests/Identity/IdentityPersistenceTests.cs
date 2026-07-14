using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Myrmex.Identity.Persistence;

namespace Myrmex.Tests.Identity;

public sealed class IdentityPersistenceTests
{
    [Fact]
    public void UserKey_UsesStableGuidIdentity()
    {
        using IdentityDbContext dbContext = CreateDbContext();

        IEntityType user = dbContext.Model.FindEntityType(typeof(AppUser))!;
        IKey key = user.FindPrimaryKey()!;

        IProperty id = Assert.Single(key.Properties);
        Assert.Equal(nameof(AppUser.Id), id.Name);
        Assert.Equal(typeof(Guid), id.ClrType);
    }

    [Theory]
    [InlineData(nameof(AppUser.NormalizedEmail))]
    [InlineData(nameof(AppUser.NormalizedUserName))]
    public void NormalizedIdentity_HasUniqueIndex(string propertyName)
    {
        using IdentityDbContext dbContext = CreateDbContext();

        IEntityType user = dbContext.Model.FindEntityType(typeof(AppUser))!;

        IIndex index = Assert.Single(
            user.GetIndexes(),
            candidate => candidate.Properties.Count == 1 &&
                candidate.Properties[0].Name == propertyName);
        Assert.True(index.IsUnique);
    }

    [Fact]
    public void IdentityModel_UsesOnlyIdentitySchema()
    {
        using IdentityDbContext dbContext = CreateDbContext();

        Type[] identityEntityTypes =
        [
            typeof(AppUser),
            typeof(AppRole),
            typeof(IdentityUserClaim<Guid>),
            typeof(IdentityUserLogin<Guid>),
            typeof(IdentityUserToken<Guid>),
            typeof(IdentityUserRole<Guid>),
            typeof(IdentityRoleClaim<Guid>),
            typeof(DataProtectionKey)
        ];

        foreach (Type identityEntityType in identityEntityTypes)
        {
            IEntityType entityType = dbContext.Model.FindEntityType(identityEntityType)!;
            Assert.Equal("identity", entityType.GetSchema());
        }
    }

    [Fact]
    public void DataProtectionKey_IsMappedToIdentityOwnedTable()
    {
        using IdentityDbContext dbContext = CreateDbContext();

        IEntityType key = dbContext.Model.FindEntityType(typeof(DataProtectionKey))!;

        Assert.Equal("DataProtectionKeys", key.GetTableName());
        Assert.Equal("identity", key.GetSchema());
        Assert.False(key.FindProperty(nameof(DataProtectionKey.Xml))!.IsNullable);
    }

    private static IdentityDbContext CreateDbContext()
    {
        DbContextOptions<IdentityDbContext> options =
            new DbContextOptionsBuilder<IdentityDbContext>()
                .UseSqlServer(
                    "Server=localhost;Database=MyrmexIdentityModelTests;" +
                    "Trusted_Connection=True;TrustServerCertificate=True")
                .Options;

        return new IdentityDbContext(options);
    }
}
