using Myrmex.Core.Domain;
using Myrmex.Core.Domain.Validation;

namespace Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;

internal sealed class StockKeepingUnit : AggregateRoot
{
    public const int MaxCodeLength = DomainTextLengths.Code;
    public const int MaxNameLength = DomainTextLengths.Name;
    public const int MaxDescriptionLength = DomainTextLengths.Description;

    private StockKeepingUnit(string code, string name, string? description)
    {
        Code = code;
        Name = name;
        Description = description;
    }

    private StockKeepingUnit()
    {
    }

    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

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
        AddDomainEvent(new StockKeepingUnitDetailsUpdatedDomainEvent(Id));

        return DomainValidationResult.Valid;
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            return;
        }

        MarkDeactivated();
        AddDomainEvent(new StockKeepingUnitDeactivatedDomainEvent(Id));
    }

    public void Reactivate()
    {
        if (IsActive)
        {
            return;
        }

        MarkReactivated();
        AddDomainEvent(new StockKeepingUnitReactivatedDomainEvent(Id));
    }

    public static DomainValidationResult Create(
        string? code,
        string? name,
        string? description,
        out StockKeepingUnit? stockKeepingUnit)
    {
        DomainValidationResult validationResult = ValidateCreate(
            code,
            name,
            description);

        if (!validationResult.IsValid)
        {
            stockKeepingUnit = null;
            return validationResult;
        }

        stockKeepingUnit = new StockKeepingUnit(
            DomainText.NormalizeCode(code),
            DomainText.NormalizeRequiredText(name),
            DomainText.NormalizeOptionalText(description));

        stockKeepingUnit.AddDomainEvent(new StockKeepingUnitCreatedDomainEvent(stockKeepingUnit.Id));

        return DomainValidationResult.Valid;
    }

    public static DomainValidationResult ValidateCreate(
        string? code,
        string? name,
        string? description)
    {
        List<DomainValidationFailure> errors = [];

        string normalizedCode = DomainText.NormalizeCode(code);

        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            errors.Add(new(
                "StockKeepingUnit.CodeRequired",
                "SKU code is required.",
                "code"));
        }
        else if (normalizedCode.Length > MaxCodeLength)
        {
            errors.Add(new(
                "StockKeepingUnit.CodeTooLong",
                $"SKU code must not exceed {MaxCodeLength} characters.",
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

        string normalizedName = DomainText.NormalizeRequiredText(name);
        string? normalizedDescription = DomainText.NormalizeOptionalText(description);

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            errors.Add(new(
                "StockKeepingUnit.NameRequired",
                "SKU name is required.",
                "name"));
        }
        else if (normalizedName.Length > MaxNameLength)
        {
            errors.Add(new(
                "StockKeepingUnit.NameTooLong",
                $"SKU name must not exceed {MaxNameLength} characters.",
                "name"));
        }

        if (normalizedDescription is not null &&
            normalizedDescription.Length > MaxDescriptionLength)
        {
            errors.Add(new(
                "StockKeepingUnit.DescriptionTooLong",
                $"SKU description must not exceed {MaxDescriptionLength} characters.",
                "description"));
        }

        return DomainValidationResult.From(errors);
    }
}
