using Myrmex.Core.Domain;
using Myrmex.Core.Domain.Validation;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Modules.Wms.Domain;

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

    public decimal? WeightKilograms { get; private set; }

    public decimal? LengthMetres { get; private set; }

    public decimal? AreaSquareMetres { get; private set; }

    public decimal? VolumeCubicMetres { get; private set; }

    internal ExternalImportState? ImportState { get; set; }

    public Guid? ExternalRefKey => ImportState?.RefKey;

    public byte[]? ExternalDataVersion => ImportState?.DataVersion;

    public DateTimeOffset? LastImportedAtUtc => ImportState?.ImportedAtUtc;

    public bool HasExternalDataVersion(ReadOnlySpan<byte> dataVersion) =>
        ImportState?.HasDataVersion(dataVersion) == true;

    public DomainValidationResult ApplyImport(
        Guid externalRefKey,
        byte[]? externalDataVersion,
        string? code,
        string? name,
        Guid? baseUnitOfMeasureId,
        decimal? weightKilograms,
        decimal? lengthMetres,
        decimal? areaSquareMetres,
        decimal? volumeCubicMetres,
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

        if (externalDataVersion is null || externalDataVersion.Length == 0)
        {
            errors.Add(DomainValidationFailure.Required<StockKeepingUnit>(nameof(ExternalDataVersion)));
        }
        else if (externalDataVersion.Length > ExternalImportState.MaxDataVersionLength)
        {
            errors.Add(DomainValidationFailure.TooLong<StockKeepingUnit>(
                nameof(ExternalDataVersion),
                ExternalImportState.MaxDataVersionLength));
        }

        DomainValidationResult validationResult = DomainValidationResult.From(errors);
        if (!validationResult.IsValid)
        {
            return validationResult;
        }

        bool physicalCharacteristicsChanged = WeightKilograms != weightKilograms ||
            LengthMetres != lengthMetres ||
            AreaSquareMetres != areaSquareMetres ||
            VolumeCubicMetres != volumeCubicMetres;

        if (isDeletionMarked)
        {
            WeightKilograms = weightKilograms;
            LengthMetres = lengthMetres;
            AreaSquareMetres = areaSquareMetres;
            VolumeCubicMetres = volumeCubicMetres;
            RecordImport(externalRefKey, externalDataVersion!, importedAtUtc);
            if (IsActive)
            {
                Deactivate();
            }
            else
            {
                Touch();
            }
            if (physicalCharacteristicsChanged)
            {
                AddDomainEvent(new StockKeepingUnitDetailsUpdatedDomainEvent(Id));
            }
            return DomainValidationResult.Valid;
        }

        validationResult = ValidateCreate(code, name, Description, baseUnitOfMeasureId);
        if (!validationResult.IsValid)
        {
            return validationResult;
        }

        string normalizedCode = DomainText.NormalizeCode(code);
        string normalizedName = DomainText.NormalizeRequiredText(name);
        Guid normalizedBaseUnitOfMeasureId = baseUnitOfMeasureId!.Value;
        bool detailsChanged = !string.Equals(Code, normalizedCode, StringComparison.Ordinal) ||
            !string.Equals(Name, normalizedName, StringComparison.Ordinal) ||
            BaseUnitOfMeasureId != normalizedBaseUnitOfMeasureId ||
            physicalCharacteristicsChanged;
        bool wasInactive = !IsActive;

        RecordImport(externalRefKey, externalDataVersion!, importedAtUtc);
        Code = normalizedCode;
        Name = normalizedName;
        BaseUnitOfMeasureId = normalizedBaseUnitOfMeasureId;
        WeightKilograms = weightKilograms;
        LengthMetres = lengthMetres;
        AreaSquareMetres = areaSquareMetres;
        VolumeCubicMetres = volumeCubicMetres;
        Reactivate();

        if (detailsChanged)
        {
            Touch();
            AddDomainEvent(new StockKeepingUnitDetailsUpdatedDomainEvent(Id));
        }
        else if (!wasInactive)
        {
            Touch();
        }

        return DomainValidationResult.Valid;
    }

    private void RecordImport(
        Guid externalRefKey,
        byte[] externalDataVersion,
        DateTimeOffset importedAtUtc)
    {
        if (ImportState is null)
        {
            ImportState = ExternalImportState.Create(
                externalRefKey,
                externalDataVersion,
                importedAtUtc);
            return;
        }

        ImportState.RecordImport(externalDataVersion, importedAtUtc);
    }

    public DomainValidationResult UpdateDetails(
        string? name,
        string? description,
        Guid? baseUnitOfMeasureId)
    {
        DomainValidationResult ownershipValidationResult = ValidateLocalDetailsOwnership(
            name,
            baseUnitOfMeasureId);
        if (!ownershipValidationResult.IsValid)
        {
            return ownershipValidationResult;
        }

        DomainValidationResult validationResult = ValidateDetails(
            name,
            description,
            baseUnitOfMeasureId);

        if (!validationResult.IsValid)
        {
            return validationResult;
        }

        string normalizedName = DomainText.NormalizeRequiredText(name);
        string? normalizedDescription = DomainText.NormalizeOptionalText(description);
        if (ExternalRefKey.HasValue &&
            string.Equals(Description, normalizedDescription, StringComparison.Ordinal))
        {
            return DomainValidationResult.Valid;
        }

        Name = normalizedName;
        Description = normalizedDescription;
        BaseUnitOfMeasureId = baseUnitOfMeasureId!.Value;

        Touch();
        AddDomainEvent(new StockKeepingUnitDetailsUpdatedDomainEvent(Id));

        return DomainValidationResult.Valid;
    }

    public DomainValidationResult ValidateLocalDetailsOwnership(
        string? name,
        Guid? baseUnitOfMeasureId)
    {
        if (!ExternalRefKey.HasValue)
        {
            return DomainValidationResult.Valid;
        }

        List<DomainValidationFailure> errors = [];
        if (!string.Equals(
                Name,
                DomainText.NormalizeRequiredText(name),
                StringComparison.Ordinal))
        {
            errors.Add(DomainValidationFailure.IncorrectState<StockKeepingUnit>(nameof(Name)));
        }
        if (baseUnitOfMeasureId != BaseUnitOfMeasureId)
        {
            errors.Add(DomainValidationFailure.IncorrectState<StockKeepingUnit>(
                nameof(BaseUnitOfMeasureId)));
        }
        return DomainValidationResult.From(errors);
    }

    public DomainValidationResult DeactivateLocally()
    {
        if (!IsActive)
        {
            return DomainValidationResult.Valid;
        }

        if (ExternalRefKey.HasValue)
        {
            return DomainValidationResult.From([
                DomainValidationFailure.IncorrectState<StockKeepingUnit>(nameof(IsActive))]);
        }

        Deactivate();
        return DomainValidationResult.Valid;
    }

    public DomainValidationResult ReactivateLocally()
    {
        if (IsActive)
        {
            return DomainValidationResult.Valid;
        }

        if (ExternalRefKey.HasValue)
        {
            return DomainValidationResult.From([
                DomainValidationFailure.IncorrectState<StockKeepingUnit>(nameof(IsActive))]);
        }

        Reactivate();
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
