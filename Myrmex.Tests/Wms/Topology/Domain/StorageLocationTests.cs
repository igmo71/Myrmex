using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;

namespace Myrmex.Tests.Wms.Topology.Domain;

public sealed class StorageLocationTests
{
    [Fact]
    public void Create_WhenRequiredIdsAreMissing_ReturnsValidationErrors()
    {
        // Act
        var result = StorageLocation.Create(
            warehouseId: Guid.Empty,
            zoneId: Guid.Empty,
            storageLocationTypeId: Guid.Empty,
            storageLocationStatusId: Guid.Empty,
            code: "A-01-01",
            name: "A-01-01",
            description: null,
            isPickable: true,
            out StorageLocation? storageLocation);

        // Assert
        Assert.False(result.IsValid);
        Assert.Null(storageLocation);

        Assert.Contains(result.Errors, error =>
            error.Code == "StorageLocation.WarehouseIdRequired" &&
            error.Property == "warehouseId");

        Assert.Contains(result.Errors, error =>
            error.Code == "StorageLocation.ZoneIdRequired" &&
            error.Property == "zoneId");

        Assert.Contains(result.Errors, error =>
            error.Code == "StorageLocation.TypeIdRequired" &&
            error.Property == "storageLocationTypeId");

        Assert.Contains(result.Errors, error =>
            error.Code == "StorageLocation.StatusIdRequired" &&
            error.Property == "storageLocationStatusId");
    }

    [Fact]
    public void Create_WhenCodeIsMissing_ReturnsValidationError()
    {
        // Act
        var result = StorageLocation.Create(
            warehouseId: Guid.NewGuid(),
            zoneId: Guid.NewGuid(),
            storageLocationTypeId: Guid.NewGuid(),
            storageLocationStatusId: Guid.NewGuid(),
            code: "",
            name: "A-01-01",
            description: null,
            isPickable: true,
            out StorageLocation? storageLocation);

        // Assert
        Assert.False(result.IsValid);
        Assert.Null(storageLocation);

        var error = Assert.Single(result.Errors);

        Assert.Equal("StorageLocation.CodeRequired", error.Code);
        Assert.Equal("Storage location code is required.", error.Message);
        Assert.Equal("code", error.Property);
    }

    [Fact]
    public void Create_WhenNameIsMissing_ReturnsValidationError()
    {
        // Act
        var result = StorageLocation.Create(
            warehouseId: Guid.NewGuid(),
            zoneId: Guid.NewGuid(),
            storageLocationTypeId: Guid.NewGuid(),
            storageLocationStatusId: Guid.NewGuid(),
            code: "A-01-01",
            name: "",
            description: null,
            isPickable: true,
            out StorageLocation? storageLocation);

        // Assert
        Assert.False(result.IsValid);
        Assert.Null(storageLocation);

        var error = Assert.Single(result.Errors);

        Assert.Equal("StorageLocation.NameRequired", error.Code);
        Assert.Equal("Storage location name is required.", error.Message);
        Assert.Equal("name", error.Property);
    }

    [Fact]
    public void Create_WhenValuesAreValid_NormalizesValuesAndCreatesStorageLocation()
    {
        // Arrange
        Guid warehouseId = Guid.NewGuid();
        Guid zoneId = Guid.NewGuid();
        Guid typeId = Guid.NewGuid();
        Guid statusId = Guid.NewGuid();

        // Act
        var result = StorageLocation.Create(
            warehouseId,
            zoneId,
            typeId,
            statusId,
            code: " a-01-01 ",
            name: " A-01-01 ",
            description: " Pick face ",
            isPickable: true,
            out StorageLocation? storageLocation);

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(storageLocation);

        Assert.Equal(warehouseId, storageLocation.WarehouseId);
        Assert.Equal(zoneId, storageLocation.ZoneId);
        Assert.Equal(typeId, storageLocation.StorageLocationTypeId);
        Assert.Equal(statusId, storageLocation.StorageLocationStatusId);
        Assert.Equal("A-01-01", storageLocation.Code);
        Assert.Equal("A-01-01", storageLocation.Name);
        Assert.Equal("Pick face", storageLocation.Description);
        Assert.True(storageLocation.IsPickable);
        Assert.True(storageLocation.IsActive);
    }

    [Fact]
    public void UpdateDetails_WhenNameIsMissing_ReturnsValidationError()
    {
        // Arrange
        StorageLocation storageLocation = CreateValidStorageLocation();

        // Act
        var result = storageLocation.UpdateDetails(
            name: "",
            description: null,
            isPickable: false);

        // Assert
        Assert.False(result.IsValid);

        var error = Assert.Single(result.Errors);

        Assert.Equal("StorageLocation.NameRequired", error.Code);
        Assert.Equal("Storage location name is required.", error.Message);
        Assert.Equal("name", error.Property);
    }

    [Fact]
    public void UpdateDetails_WhenValuesAreValid_UpdatesDetails()
    {
        // Arrange
        StorageLocation storageLocation = CreateValidStorageLocation();

        // Act
        var result = storageLocation.UpdateDetails(
            name: " Updated Location ",
            description: " Updated description ",
            isPickable: false);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("Updated Location", storageLocation.Name);
        Assert.Equal("Updated description", storageLocation.Description);
        Assert.False(storageLocation.IsPickable);
    }

    [Fact]
    public void Deactivate_WhenStorageLocationIsActive_MarksStorageLocationInactive()
    {
        // Arrange
        StorageLocation storageLocation = CreateValidStorageLocation();

        // Act
        storageLocation.Deactivate();

        // Assert
        Assert.False(storageLocation.IsActive);
    }

    [Fact]
    public void Reactivate_WhenStorageLocationIsInactive_MarksStorageLocationActive()
    {
        // Arrange
        StorageLocation storageLocation = CreateValidStorageLocation();

        storageLocation.Deactivate();
        Assert.False(storageLocation.IsActive);

        // Act
        storageLocation.Reactivate();

        // Assert
        Assert.True(storageLocation.IsActive);
    }

    private static StorageLocation CreateValidStorageLocation()
    {
        var result = StorageLocation.Create(
            warehouseId: Guid.NewGuid(),
            zoneId: Guid.NewGuid(),
            storageLocationTypeId: Guid.NewGuid(),
            storageLocationStatusId: Guid.NewGuid(),
            code: "A-01-01",
            name: "A-01-01",
            description: null,
            isPickable: true,
            out StorageLocation? storageLocation);

        Assert.True(result.IsValid);
        Assert.NotNull(storageLocation);

        return storageLocation;
    }
}