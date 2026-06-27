using Myrmex.Core.Domain;
using Myrmex.Core.Domain.Validation;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;

namespace Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;

internal sealed class StockKeepingUnit : AggregateRoot, IActivatable
{
    public const int MaxCodeLength = DomainTextLengths.Code;
    public const int MaxNameLength = DomainTextLengths.Name;
    public const int MaxDescriptionLength = DomainTextLengths.Description;

    private StockKeepingUnit(
        string code,
        string name,
        string? description,
        Guid baseUnitOfMeasureId)
    {
        Code = code;
        Name = name;
        Description = description;
        BaseUnitOfMeasureId = baseUnitOfMeasureId;
    }

    private StockKeepingUnit()
    {
    }

    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    public Guid BaseUnitOfMeasureId { get; private set; }
    public UnitOfMeasure BaseUnitOfMeasure { get; private set; } = null!;

    public bool IsActive { get; private set; } = true;

    public Guid? ExternalRefKey { get; private set; }

    public DateTimeOffset? LastImportedAtUtc { get; private set; }

    public DomainValidationResult ApplyImport(
        Guid externalRefKey,
        string? code,
        string? name,
        Guid? baseUnitOfMeasureId,
        bool isDeletionMarked,
        DateTimeOffset importedAtUtc)
    {
        List<DomainValidationFailure> errors = [];

        if (externalRefKey == Guid.Empty)
        {
            errors.Add(DomainValidationFailure.Required<StockKeepingUnit>(nameof(ExternalRefKey)));
        }
        else if (ExternalRefKey.HasValue && ExternalRefKey.Value != externalRefKey)
        {
            errors.Add(DomainValidationFailure.IncorrectState<StockKeepingUnit>(nameof(ExternalRefKey)));
        }

        DomainValidationResult validationResult = DomainValidationResult.From(errors);
        if (!validationResult.IsValid)
        {
            return validationResult;
        }

        if (isDeletionMarked)
        {
            ExternalRefKey ??= externalRefKey;
            LastImportedAtUtc = importedAtUtc;
            if (IsActive)
            {
                Deactivate();
            }
            else
            {
                Touch();
            }
            return DomainValidationResult.Valid;
        }

        validationResult = ValidateCreate(code, name, Description, baseUnitOfMeasureId);
        if (!validationResult.IsValid)
        {
            return validationResult;
        }

        ExternalRefKey ??= externalRefKey;
        Code = DomainText.NormalizeCode(code);
        Name = DomainText.NormalizeRequiredText(name);
        BaseUnitOfMeasureId = baseUnitOfMeasureId!.Value;
        LastImportedAtUtc = importedAtUtc;
        Reactivate();

        Touch();
        AddDomainEvent(new StockKeepingUnitDetailsUpdatedDomainEvent(Id));
        return DomainValidationResult.Valid;
    }

    public DomainValidationResult UpdateDetails(
        string? name,
        string? description,
        Guid? baseUnitOfMeasureId)
    {
        DomainValidationResult validationResult = ValidateDetails(
            name,
            description,
            baseUnitOfMeasureId);

        if (!validationResult.IsValid)
        {
            return validationResult;
        }

        Name = DomainText.NormalizeRequiredText(name);
        Description = DomainText.NormalizeOptionalText(description);
        BaseUnitOfMeasureId = baseUnitOfMeasureId!.Value;

        Touch();
        AddDomainEvent(new StockKeepingUnitDetailsUpdatedDomainEvent(Id));

        return DomainValidationResult.Valid;
    }

    public DomainValidationResult UpdateDetails(
        string? name,
        string? description)
    {
        return UpdateDetails(
            name,
            description,
            BaseUnitOfMeasureId);
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        Touch();
        AddDomainEvent(new StockKeepingUnitDeactivatedDomainEvent(Id));
    }

    public void Reactivate()
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        Touch();
        AddDomainEvent(new StockKeepingUnitReactivatedDomainEvent(Id));
    }

    public static DomainValidationResult Create(
        string? code,
        string? name,
        string? description,
        Guid? baseUnitOfMeasureId,
        out StockKeepingUnit? stockKeepingUnit)
    {
        DomainValidationResult validationResult = ValidateCreate(
            code,
            name,
            description,
            baseUnitOfMeasureId);

        if (!validationResult.IsValid)
        {
            stockKeepingUnit = null;
            return validationResult;
        }

        stockKeepingUnit = new StockKeepingUnit(
            DomainText.NormalizeCode(code),
            DomainText.NormalizeRequiredText(name),
            DomainText.NormalizeOptionalText(description),
            baseUnitOfMeasureId!.Value);

        stockKeepingUnit.AddDomainEvent(new StockKeepingUnitCreatedDomainEvent(stockKeepingUnit.Id));

        return DomainValidationResult.Valid;
    }

    public static DomainValidationResult ValidateCreate(
        string? code,
        string? name,
        string? description,
        Guid? baseUnitOfMeasureId)
    {
        List<DomainValidationFailure> errors = [];

        string normalizedCode = DomainText.NormalizeCode(code);

        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            errors.Add(DomainValidationFailure.Required<StockKeepingUnit>(nameof(Code)));
        }
        else if (normalizedCode.Length > MaxCodeLength)
        {
            errors.Add(DomainValidationFailure.TooLong<StockKeepingUnit>(nameof(Code), MaxCodeLength));
        }

        DomainValidationResult detailsValidationResult = ValidateDetails(
            name,
            description,
            baseUnitOfMeasureId);

        errors.AddRange(detailsValidationResult.Errors);

        return DomainValidationResult.From(errors);
    }

    public static DomainValidationResult ValidateDetails(
        string? name,
        string? description,
        Guid? baseUnitOfMeasureId)
    {
        List<DomainValidationFailure> errors = [];

        string normalizedName = DomainText.NormalizeRequiredText(name);
        string? normalizedDescription = DomainText.NormalizeOptionalText(description);

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            errors.Add(DomainValidationFailure.Required<StockKeepingUnit>(nameof(Name)));
        }
        else if (normalizedName.Length > MaxNameLength)
        {
            errors.Add(DomainValidationFailure.TooLong<StockKeepingUnit>(nameof(Name), MaxNameLength));
        }

        if (normalizedDescription is not null &&
            normalizedDescription.Length > MaxDescriptionLength)
        {
            errors.Add(DomainValidationFailure.TooLong<StockKeepingUnit>(nameof(Description), MaxDescriptionLength));
        }

        if (!baseUnitOfMeasureId.HasValue ||
            baseUnitOfMeasureId.Value == Guid.Empty)
        {
            errors.Add(DomainValidationFailure.Required<StockKeepingUnit>(nameof(BaseUnitOfMeasureId)));
        }

        return DomainValidationResult.From(errors);
    }

    public static DomainValidationResult ValidateDetails(
        string? name,
        string? description)
    {
        return ValidateDetails(
            name,
            description,
            baseUnitOfMeasureId: null);
    }
}
