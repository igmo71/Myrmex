using Myrmex.Core.Domain;
using Myrmex.Core.Domain.Validation;

namespace Myrmex.Modules.Wms.Topology.Domain.Zones;

internal sealed class Zone : AggregateRoot, IActivatable
{
    public const int MaxCodeLength = DomainTextLengths.Code;
    public const int MaxNameLength = DomainTextLengths.Name;
    public const int MaxDescriptionLength = DomainTextLengths.Description;

    private Zone(
        Guid warehouseId,
        string code,
        string name,
        string? description)
    {
        WarehouseId = warehouseId;
        Code = code;
        Name = name;
        Description = description;
    }

    private Zone()
    {
    }

    public Guid WarehouseId { get; private set; }

    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    public bool IsActive { get; private set; } = true;

    public static DomainValidationResult Create(
        Guid warehouseId,
        string? code,
        string? name,
        string? description,
        out Zone? zone)
    {
        DomainValidationResult validationResult = ValidateCreate(
            warehouseId,
            code,
            name,
            description);

        if (!validationResult.IsValid)
        {
            zone = null;
            return validationResult;
        }

        zone = new Zone(
            warehouseId,
            DomainText.NormalizeCode(code),
            DomainText.NormalizeRequiredText(name),
            DomainText.NormalizeOptionalText(description));

        zone.AddDomainEvent(new ZoneCreatedDomainEvent(zone.Id, zone.WarehouseId));

        return DomainValidationResult.Valid;
    }

    public DomainValidationResult UpdateDetails(
        string? name,
        string? description)
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

        Touch();
        AddDomainEvent(new ZoneDetailsUpdatedDomainEvent(Id, WarehouseId));

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
        AddDomainEvent(new ZoneDeactivatedDomainEvent(Id, WarehouseId));
    }

    public void Reactivate()
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        Touch();
        AddDomainEvent(new ZoneReactivatedDomainEvent(Id, WarehouseId));
    }

    public static DomainValidationResult ValidateCreate(
        Guid warehouseId,
        string? code,
        string? name,
        string? description)
    {
        List<DomainValidationFailure> errors = [];

        if (warehouseId == Guid.Empty)
        {
            errors.Add(DomainValidationFailure.Required<Zone>(nameof(WarehouseId)));
        }

        string normalizedCode = DomainText.NormalizeCode(code);

        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            errors.Add(DomainValidationFailure.Required<Zone>(nameof(Code)));
        }
        else if (normalizedCode.Length > MaxCodeLength)
        {
            errors.Add(DomainValidationFailure.TooLong<Zone>(nameof(Code), MaxCodeLength));
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
            errors.Add(DomainValidationFailure.Required<Zone>(nameof(Name)));
        }
        else if (normalizedName.Length > MaxNameLength)
        {
            errors.Add(DomainValidationFailure.TooLong<Zone>(nameof(Name), MaxNameLength));
        }

        if (normalizedDescription is not null &&
            normalizedDescription.Length > MaxDescriptionLength)
        {
            errors.Add(DomainValidationFailure.TooLong<Zone>(nameof(Description), MaxDescriptionLength));
        }

        return DomainValidationResult.From(errors);
    }
}
