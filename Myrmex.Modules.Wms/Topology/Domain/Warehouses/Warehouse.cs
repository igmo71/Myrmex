using Myrmex.Core.Domain;
using Myrmex.Core.Domain.Validation;
using Myrmex.Modules.Wms.Domain;

namespace Myrmex.Modules.Wms.Topology.Domain.Warehouses;

internal sealed class Warehouse : AggregateRoot, IActivatable
{
    public const int MaxCodeLength = DomainTextLengths.Code;
    public const int MaxNameLength = DomainTextLengths.Name;
    public const int MaxDescriptionLength = DomainTextLengths.Description;

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

    public bool IsActive { get; private set; } = true;

    internal ExternalImportState? ImportState { get; set; }

    public Guid? ExternalRefKey => ImportState?.RefKey;

    public byte[]? ExternalDataVersion => ImportState?.DataVersion;

    public DateTimeOffset? LastImportedAtUtc => ImportState?.ImportedAtUtc;

    public bool HasExternalDataVersion(ReadOnlySpan<byte> dataVersion) =>
        ImportState?.HasDataVersion(dataVersion) == true;

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
            DomainText.NormalizeCode(code),
            DomainText.NormalizeRequiredText(name),
            DomainText.NormalizeOptionalText(description));

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

        Name = DomainText.NormalizeRequiredText(name);
        Description = DomainText.NormalizeOptionalText(description);

        Touch();
        AddDomainEvent(new WarehouseDetailsUpdatedDomainEvent(Id));

        return DomainValidationResult.Valid;
    }

    public DomainValidationResult ApplyImport(
        Guid externalRefKey,
        byte[]? externalDataVersion,
        string? code,
        string? name,
        bool isDeletionMarked,
        DateTimeOffset importedAtUtc)
    {
        List<DomainValidationFailure> errors = [];

        if (externalRefKey == Guid.Empty)
        {
            errors.Add(DomainValidationFailure.Required<Warehouse>(nameof(ExternalRefKey)));
        }
        else if (ExternalRefKey.HasValue && ExternalRefKey.Value != externalRefKey)
        {
            errors.Add(DomainValidationFailure.IncorrectState<Warehouse>(nameof(ExternalRefKey)));
        }

        if (externalDataVersion is null || externalDataVersion.Length == 0)
        {
            errors.Add(DomainValidationFailure.Required<Warehouse>(nameof(ExternalDataVersion)));
        }
        else if (externalDataVersion.Length > ExternalImportState.MaxDataVersionLength)
        {
            errors.Add(DomainValidationFailure.TooLong<Warehouse>(
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

        validationResult = ValidateCreate(code, name, Description);
        if (!validationResult.IsValid)
        {
            return validationResult;
        }

        string normalizedCode = DomainText.NormalizeCode(code);
        string normalizedName = DomainText.NormalizeRequiredText(name);
        bool detailsChanged = !string.Equals(Code, normalizedCode, StringComparison.Ordinal) ||
            !string.Equals(Name, normalizedName, StringComparison.Ordinal);
        bool wasInactive = !IsActive;

        RecordImport(externalRefKey, externalDataVersion!, importedAtUtc);
        Code = normalizedCode;
        Name = normalizedName;
        Reactivate();

        if (detailsChanged)
        {
            Touch();
            AddDomainEvent(new WarehouseDetailsUpdatedDomainEvent(Id));
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

    public void Deactivate()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        Touch();
        AddDomainEvent(new WarehouseDeactivatedDomainEvent(Id));
    }

    public void Reactivate()
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        Touch();
        AddDomainEvent(new WarehouseReactivatedDomainEvent(Id));
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
            errors.Add(DomainValidationFailure.Required<Warehouse>(nameof(Code)));
        }
        else if (normalizedCode.Length > MaxCodeLength)
        {
            errors.Add(DomainValidationFailure.TooLong<Warehouse>(nameof(Code), MaxCodeLength));
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
            errors.Add(DomainValidationFailure.Required<Warehouse>(nameof(Name)));
        }
        else if (normalizedName.Length > MaxNameLength)
        {
            errors.Add(DomainValidationFailure.TooLong<Warehouse>(nameof(Name), MaxNameLength));
        }

        if (normalizedDescription is not null &&
            normalizedDescription.Length > MaxDescriptionLength)
        {
            errors.Add(DomainValidationFailure.TooLong<Warehouse>(nameof(Description), MaxDescriptionLength));
        }

        return DomainValidationResult.From(errors);
    }
}
