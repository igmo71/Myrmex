using Myrmex.Core.Domain;
using Myrmex.Core.Domain.Validation;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;
using Myrmex.Modules.Wms.Topology.Domain.Zones;

namespace Myrmex.Modules.Wms.Topology.Domain.StorageLocations;

internal sealed class StorageLocation : AggregateRoot, IActivatable
{
    public const int MaxCodeLength = DomainTextLengths.Code;
    public const int MaxNameLength = DomainTextLengths.Name;
    public const int MaxDescriptionLength = DomainTextLengths.Description;

    private StorageLocation(
        Guid warehouseId,
        Guid zoneId,
        Guid storageLocationTypeId,
        Guid storageLocationStatusId,
        string code,
        string name,
        string? description,
        bool isPickable)
    {
        WarehouseId = warehouseId;
        ZoneId = zoneId;
        StorageLocationTypeId = storageLocationTypeId;
        StorageLocationStatusId = storageLocationStatusId;
        Code = code;
        Name = name;
        Description = description;
        IsPickable = isPickable;
    }

    private StorageLocation()
    {
    }

    public Guid WarehouseId { get; private set; }
    public Warehouse Warehouse { get; private set; } = null!;

    public Guid ZoneId { get; private set; }
    public Zone Zone { get; private set; } = null!;


    public Guid StorageLocationTypeId { get; private set; }
    public StorageLocationType StorageLocationType { get; private set; } = null!;


    public Guid StorageLocationStatusId { get; private set; }
    public StorageLocationStatus StorageLocationStatus { get; private set; } = null!;


    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    public bool IsPickable { get; private set; }

    public bool IsActive { get; private set; } = true;

    public static DomainValidationResult Create(
        Guid warehouseId,
        Guid zoneId,
        Guid storageLocationTypeId,
        Guid storageLocationStatusId,
        string? code,
        string? name,
        string? description,
        bool isPickable,
        out StorageLocation? storageLocation)
    {
        DomainValidationResult validationResult = ValidateCreate(
            warehouseId,
            zoneId,
            storageLocationTypeId,
            storageLocationStatusId,
            code,
            name,
            description);

        if (!validationResult.IsValid)
        {
            storageLocation = null;
            return validationResult;
        }

        storageLocation = new StorageLocation(
            warehouseId,
            zoneId,
            storageLocationTypeId,
            storageLocationStatusId,
            DomainText.NormalizeCode(code),
            DomainText.NormalizeRequiredText(name),
            DomainText.NormalizeOptionalText(description),
            isPickable);

        storageLocation.AddDomainEvent(
            new StorageLocationCreatedDomainEvent(
                storageLocation.Id,
                storageLocation.WarehouseId,
                storageLocation.ZoneId));

        return DomainValidationResult.Valid;
    }

    public DomainValidationResult UpdateDetails(
        string? name,
        string? description,
        bool isPickable)
    {
        DomainValidationResult validationResult = ValidateDetails(
            name,
            description);

        if (!validationResult.IsValid)
        {
            return validationResult;
        }

        Name = DomainText.NormalizeRequiredText(name);
        Description = DomainText.NormalizeOptionalText(description);
        IsPickable = isPickable;

        Touch();

        AddDomainEvent(
            new StorageLocationDetailsUpdatedDomainEvent(
                Id,
                WarehouseId,
                ZoneId));

        return DomainValidationResult.Valid;
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        Touch();

        AddDomainEvent(
            new StorageLocationDeactivatedDomainEvent(
                Id,
                WarehouseId,
                ZoneId));
    }

    public void Reactivate()
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        Touch();

        AddDomainEvent(
            new StorageLocationReactivatedDomainEvent(
                Id,
                WarehouseId,
                ZoneId));
    }

    public static DomainValidationResult ValidateCreate(
        Guid warehouseId,
        Guid zoneId,
        Guid storageLocationTypeId,
        Guid storageLocationStatusId,
        string? code,
        string? name,
        string? description)
    {
        List<DomainValidationFailure> errors = [];

        if (warehouseId == Guid.Empty)
        {
            errors.Add(StorageLocationValidationErrors.WarehouseIdRequired);
        }

        if (zoneId == Guid.Empty)
        {
            errors.Add(StorageLocationValidationErrors.ZoneIdRequired);
        }

        if (storageLocationTypeId == Guid.Empty)
        {
            errors.Add(StorageLocationValidationErrors.TypeIdRequired);
        }

        if (storageLocationStatusId == Guid.Empty)
        {
            errors.Add(StorageLocationValidationErrors.StatusIdRequired);
        }

        string normalizedCode = DomainText.NormalizeCode(code);

        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            errors.Add(StorageLocationValidationErrors.CodeRequired);
        }
        else if (normalizedCode.Length > MaxCodeLength)
        {
            errors.Add(StorageLocationValidationErrors.CodeTooLong(MaxCodeLength));
        }

        DomainValidationResult detailsValidationResult = ValidateDetails(
            name,
            description);

        errors.AddRange(detailsValidationResult.Errors);

        return DomainValidationResult.From(errors);
    }

    public static DomainValidationResult ValidateDetails(
        string? name,
        string? description)
    {
        List<DomainValidationFailure> errors = [];

        string normalizedName = DomainText.NormalizeRequiredText(name);
        string? normalizedDescription = DomainText.NormalizeOptionalText(description);

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            errors.Add(StorageLocationValidationErrors.NameRequired);
        }
        else if (normalizedName.Length > MaxNameLength)
        {
            errors.Add(StorageLocationValidationErrors.NameTooLong(MaxNameLength));
        }

        if (normalizedDescription is not null &&
            normalizedDescription.Length > MaxDescriptionLength)
        {
            errors.Add(StorageLocationValidationErrors.DescriptionTooLong(MaxDescriptionLength));
        }

        return DomainValidationResult.From(errors);
    }
}
