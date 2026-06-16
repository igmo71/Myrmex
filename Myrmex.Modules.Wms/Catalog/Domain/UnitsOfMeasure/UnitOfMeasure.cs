using Myrmex.Core.Domain;
using Myrmex.Core.Domain.Validation;

namespace Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;

internal sealed class UnitOfMeasure : AggregateRoot, IActivatable
{
    public const int MaxCodeLength = DomainTextLengths.Code;
    public const int MaxNameLength = DomainTextLengths.Name;
    public const int MaxSymbolLength = DomainTextLengths.Code;

    private UnitOfMeasure(string code, string name, string? symbol)
    {
        Code = code;
        Name = name;
        Symbol = symbol;
    }

    private UnitOfMeasure()
    {
    }

    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string? Symbol { get; private set; }

    public bool IsActive { get; private set; } = true;

    public DomainValidationResult UpdateDetails(
        string? name,
        string? symbol)
    {
        DomainValidationResult validationResult = ValidateDetails(
            name,
            symbol);

        if (!validationResult.IsValid)
        {
            return validationResult;
        }

        Name = DomainText.NormalizeRequiredText(name);
        Symbol = DomainText.NormalizeOptionalText(symbol);

        Touch();
        AddDomainEvent(new UnitOfMeasureDetailsUpdatedDomainEvent(Id));

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
        AddDomainEvent(new UnitOfMeasureDeactivatedDomainEvent(Id));
    }

    public void Reactivate()
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        Touch();
        AddDomainEvent(new UnitOfMeasureReactivatedDomainEvent(Id));
    }

    public static DomainValidationResult Create(
        string? code,
        string? name,
        string? symbol,
        out UnitOfMeasure? unitOfMeasure)
    {
        DomainValidationResult validationResult = ValidateCreate(
            code,
            name,
            symbol);

        if (!validationResult.IsValid)
        {
            unitOfMeasure = null;
            return validationResult;
        }

        unitOfMeasure = new UnitOfMeasure(
            DomainText.NormalizeCode(code),
            DomainText.NormalizeRequiredText(name),
            DomainText.NormalizeOptionalText(symbol));

        unitOfMeasure.AddDomainEvent(new UnitOfMeasureCreatedDomainEvent(unitOfMeasure.Id));

        return DomainValidationResult.Valid;
    }

    public static DomainValidationResult ValidateCreate(
        string? code,
        string? name,
        string? symbol)
    {
        List<DomainValidationFailure> errors = [];

        string normalizedCode = DomainText.NormalizeCode(code);

        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            errors.Add(DomainValidationFailure.Required<UnitOfMeasure>("Code"));
        }
        else if (normalizedCode.Length > MaxCodeLength)
        {
            errors.Add(DomainValidationFailure.TooLong<UnitOfMeasure>("Code", MaxCodeLength));
        }

        DomainValidationResult detailsValidationResult = ValidateDetails(
            name,
            symbol);

        errors.AddRange(detailsValidationResult.Errors);

        return DomainValidationResult.From(errors);
    }

    public static DomainValidationResult ValidateDetails(
        string? name,
        string? symbol)
    {
        List<DomainValidationFailure> errors = [];

        string normalizedName = DomainText.NormalizeRequiredText(name);

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            errors.Add(DomainValidationFailure.Required<UnitOfMeasure>("Name"));
        }
        else if (normalizedName.Length > MaxNameLength)
        {
            errors.Add(DomainValidationFailure.TooLong<UnitOfMeasure>("Name", MaxNameLength));
        }

        string? normalizedSymbol = DomainText.NormalizeOptionalText(symbol);

        if (normalizedSymbol is not null &&
            normalizedSymbol.Length > MaxSymbolLength)
        {
            errors.Add(DomainValidationFailure.TooLong<UnitOfMeasure>("Symbol", MaxSymbolLength));
        }

        return DomainValidationResult.From(errors);
    }
}
