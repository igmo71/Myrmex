using Myrmex.Core.Domain;
using Myrmex.Core.Domain.Validation;

namespace Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;

internal sealed class UnitOfMeasure : AggregateRoot
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
            errors.Add(new(
                "UnitOfMeasure.CodeRequired",
                "UoM code is required.",
                "code"));
        }
        else if (normalizedCode.Length > MaxCodeLength)
        {
            errors.Add(new(
                "UnitOfMeasure.CodeTooLong",
                $"UoM code must not exceed {MaxCodeLength} characters.",
                "code"));
        }

        string normalizedName = DomainText.NormalizeRequiredText(name);

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            errors.Add(new(
                "UnitOfMeasure.NameRequired",
                "UoM name is required.",
                "name"));
        }
        else if (normalizedName.Length > MaxNameLength)
        {
            errors.Add(new(
                "UnitOfMeasure.NameTooLong",
                $"UoM name must not exceed {MaxNameLength} characters.",
                "name"));
        }

        string? normalizedSymbol = DomainText.NormalizeOptionalText(symbol);

        if (normalizedSymbol is not null &&
            normalizedSymbol.Length > MaxSymbolLength)
        {
            errors.Add(new(
                "UnitOfMeasure.SymbolTooLong",
                $"UoM symbol must not exceed {MaxSymbolLength} characters.",
                "symbol"));
        }

        return DomainValidationResult.From(errors);
    }
}
