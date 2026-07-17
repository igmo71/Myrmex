using Myrmex.Core.Domain;
using Myrmex.Core.Domain.Validation;
using Myrmex.Modules.Wms.Domain;

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
        string? symbol,
        bool isDeletionMarked,
        DateTimeOffset importedAtUtc)
    {
        List<DomainValidationFailure> errors = [];

        if (externalRefKey == Guid.Empty)
        {
            errors.Add(DomainValidationFailure.Required<UnitOfMeasure>(nameof(ExternalRefKey)));
        }
        else if (ExternalRefKey.HasValue && ExternalRefKey.Value != externalRefKey)
        {
            errors.Add(DomainValidationFailure.IncorrectState<UnitOfMeasure>(nameof(ExternalRefKey)));
        }

        if (externalDataVersion is null || externalDataVersion.Length == 0)
        {
            errors.Add(DomainValidationFailure.Required<UnitOfMeasure>(nameof(ExternalDataVersion)));
        }
        else if (externalDataVersion.Length > ExternalImportState.MaxDataVersionLength)
        {
            errors.Add(DomainValidationFailure.TooLong<UnitOfMeasure>(
                nameof(ExternalDataVersion),
                ExternalImportState.MaxDataVersionLength));
        }

        DomainValidationResult validationResult = DomainValidationResult.From(errors);
        if (!validationResult.IsValid)
        {
            return validationResult;
        }

        if (isDeletionMarked)
        {
            RecordImport(externalRefKey, externalDataVersion!, importedAtUtc);
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

        validationResult = ValidateCreate(code, name, symbol);
        if (!validationResult.IsValid)
        {
            return validationResult;
        }

        string normalizedCode = DomainText.NormalizeCode(code);
        string normalizedName = DomainText.NormalizeRequiredText(name);
        string? normalizedSymbol = DomainText.NormalizeOptionalText(symbol);
        bool detailsChanged = !string.Equals(Code, normalizedCode, StringComparison.Ordinal) ||
            !string.Equals(Name, normalizedName, StringComparison.Ordinal) ||
            !string.Equals(Symbol, normalizedSymbol, StringComparison.Ordinal);
        bool wasInactive = !IsActive;

        RecordImport(externalRefKey, externalDataVersion!, importedAtUtc);
        Code = normalizedCode;
        Name = normalizedName;
        Symbol = normalizedSymbol;
        Reactivate();

        if (detailsChanged)
        {
            Touch();
            AddDomainEvent(new UnitOfMeasureDetailsUpdatedDomainEvent(Id));
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
        string? symbol)
    {
        string normalizedName = DomainText.NormalizeRequiredText(name);
        string? normalizedSymbol = DomainText.NormalizeOptionalText(symbol);
        if (ExternalRefKey.HasValue &&
            (!string.Equals(Name, normalizedName, StringComparison.Ordinal) ||
             !string.Equals(Symbol, normalizedSymbol, StringComparison.Ordinal)))
        {
            List<DomainValidationFailure> ownershipErrors = [];
            if (!string.Equals(Name, normalizedName, StringComparison.Ordinal))
            {
                ownershipErrors.Add(
                    DomainValidationFailure.IncorrectState<UnitOfMeasure>(nameof(Name)));
            }
            if (!string.Equals(Symbol, normalizedSymbol, StringComparison.Ordinal))
            {
                ownershipErrors.Add(
                    DomainValidationFailure.IncorrectState<UnitOfMeasure>(nameof(Symbol)));
            }
            return DomainValidationResult.From(ownershipErrors);
        }

        DomainValidationResult validationResult = ValidateDetails(
            name,
            symbol);

        if (!validationResult.IsValid)
        {
            return validationResult;
        }

        if (ExternalRefKey.HasValue)
        {
            return DomainValidationResult.Valid;
        }

        Name = normalizedName;
        Symbol = normalizedSymbol;

        Touch();
        AddDomainEvent(new UnitOfMeasureDetailsUpdatedDomainEvent(Id));

        return DomainValidationResult.Valid;
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
                DomainValidationFailure.IncorrectState<UnitOfMeasure>(nameof(IsActive))]);
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
                DomainValidationFailure.IncorrectState<UnitOfMeasure>(nameof(IsActive))]);
        }

        Reactivate();
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
            errors.Add(DomainValidationFailure.Required<UnitOfMeasure>(nameof(Code)));
        }
        else if (normalizedCode.Length > MaxCodeLength)
        {
            errors.Add(DomainValidationFailure.TooLong<UnitOfMeasure>(nameof(Code), MaxCodeLength));
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
            errors.Add(DomainValidationFailure.Required<UnitOfMeasure>(nameof(Name)));
        }
        else if (normalizedName.Length > MaxNameLength)
        {
            errors.Add(DomainValidationFailure.TooLong<UnitOfMeasure>(nameof(Name), MaxNameLength));
        }

        string? normalizedSymbol = DomainText.NormalizeOptionalText(symbol);

        if (normalizedSymbol is not null &&
            normalizedSymbol.Length > MaxSymbolLength)
        {
            errors.Add(DomainValidationFailure.TooLong<UnitOfMeasure>(nameof(Symbol), MaxSymbolLength));
        }

        return DomainValidationResult.From(errors);
    }
}
