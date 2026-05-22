using Myrmex.Core.Domain;
using Myrmex.Core.Domain.Validation;

namespace Myrmex.Modules.Wms.Capabilities.Topology.Domain.Warehouses;

internal sealed class Warehouse : AggregateRoot
{
    public const int MaxCodeLength = 32;
    public const int MaxNameLength = 200;
    public const int MaxDescriptionLength = 1000;

    private Warehouse(string code, string name, string? description)
    {
        Code = code;
        Name = name;
        Description = description;
    }

    private Warehouse()
    {
    }

    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    public static DomainValidationResult Create(
        string? code,
        string? name,
        string? description,
        out Warehouse? warehouse)
    {
        DomainValidationResult validationResult = ValidateCreate(
            code,
            name,
            description);

        if (!validationResult.IsValid)
        {
            warehouse = null;
            return validationResult;
        }

        warehouse = new Warehouse(
            NormalizeCode(code),
            NormalizeRequiredText(name),
            NormalizeOptionalText(description));

        warehouse.AddDomainEvent(new WarehouseCreatedDomainEvent(warehouse.Id));

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

        Name = NormalizeRequiredText(name);
        Description = NormalizeOptionalText(description);

        Touch();
        AddDomainEvent(new WarehouseDetailsUpdatedDomainEvent(Id));

        return DomainValidationResult.Valid;
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            return;
        }

        MarkDeactivated();
        AddDomainEvent(new WarehouseDeactivatedDomainEvent(Id));
    }

    public void Reactivate()
    {
        if (IsActive)
        {
            return;
        }

        MarkReactivated();
        AddDomainEvent(new WarehouseReactivatedDomainEvent(Id));
    }

    public static DomainValidationResult ValidateCreate(
        string? code,
        string? name,
        string? description)
    {
        List<DomainValidationFailure> errors = [];

        string normalizedCode = NormalizeCode(code);

        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            errors.Add(new(
                "Warehouse.CodeRequired",
                "Warehouse code is required.",
                "code"));
        }
        else if (normalizedCode.Length > MaxCodeLength)
        {
            errors.Add(new(
                "Warehouse.CodeTooLong",
                $"Warehouse code must not exceed {MaxCodeLength} characters.",
                "code"));
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

        string normalizedName = NormalizeRequiredText(name);
        string? normalizedDescription = NormalizeOptionalText(description);

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            errors.Add(new(
                "Warehouse.NameRequired",
                "Warehouse name is required.",
                "name"));
        }
        else if (normalizedName.Length > MaxNameLength)
        {
            errors.Add(new(
                "Warehouse.NameTooLong",
                $"Warehouse name must not exceed {MaxNameLength} characters.",
                "name"));
        }

        if (normalizedDescription is not null &&
            normalizedDescription.Length > MaxDescriptionLength)
        {
            errors.Add(new(
                "Warehouse.DescriptionTooLong",
                $"Warehouse description must not exceed {MaxDescriptionLength} characters.",
                "description"));
        }

        return DomainValidationResult.From(errors);
    }

    public static string NormalizeCode(string? code)
    {
        return NormalizeRequiredText(code).ToUpperInvariant();
    }

    private static string NormalizeRequiredText(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        string? normalized = value?.Trim();

        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }
}