using Myrmex.Core.Domain;
using Myrmex.Core.Domain.Validation;

namespace Myrmex.Modules.Wms.Catalog.Domain.SkuBarcodes;

internal sealed class SkuBarcode : AggregateRoot, IActivatable
{
    public const int MaxValueLength = 200;

    private SkuBarcode(
        Guid stockKeepingUnitId,
        string value,
        BarcodeSymbology symbology,
        bool isPrimary)
    {
        StockKeepingUnitId = stockKeepingUnitId;
        Value = value;
        Symbology = symbology;
        IsPrimary = isPrimary;
    }

    private SkuBarcode()
    {
    }

    public Guid StockKeepingUnitId { get; private set; }

    public string Value { get; private set; } = null!;

    public BarcodeSymbology Symbology { get; private set; }

    public bool IsPrimary { get; private set; }

    public bool IsActive { get; private set; } = true;

    public void ClearPrimary()
    {
        if (!IsPrimary)
        {
            return;
        }

        IsPrimary = false;
        Touch();
    }

    public DomainValidationResult UpdateDetails(
        string? value,
        BarcodeSymbology symbology,
        bool isPrimary)
    {
        DomainValidationResult validationResult = ValidateDetails(
            value,
            symbology,
            isPrimary,
            IsActive);

        if (!validationResult.IsValid)
        {
            return validationResult;
        }

        Value = NormalizeValue(value);
        Symbology = symbology;
        IsPrimary = isPrimary;

        Touch();
        AddDomainEvent(new SkuBarcodeDetailsUpdatedDomainEvent(Id));

        return DomainValidationResult.Valid;
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            return;
        }

        IsPrimary = false;
        IsActive = false;
        Touch();
        AddDomainEvent(new SkuBarcodeDeactivatedDomainEvent(Id));
    }

    public void Reactivate()
    {
        if (IsActive)
        {
            return;
        }

        IsPrimary = false;
        IsActive = true;
        Touch();
        AddDomainEvent(new SkuBarcodeReactivatedDomainEvent(Id));
    }

    public static DomainValidationResult Create(
        Guid stockKeepingUnitId,
        string? value,
        BarcodeSymbology symbology,
        bool isPrimary,
        out SkuBarcode? skuBarcode)
    {
        DomainValidationResult validationResult = ValidateCreate(
            stockKeepingUnitId,
            value,
            symbology);

        if (!validationResult.IsValid)
        {
            skuBarcode = null;
            return validationResult;
        }

        skuBarcode = new SkuBarcode(
            stockKeepingUnitId,
            NormalizeValue(value),
            symbology,
            isPrimary);

        skuBarcode.AddDomainEvent(new SkuBarcodeCreatedDomainEvent(skuBarcode.Id));

        return DomainValidationResult.Valid;
    }

    public static DomainValidationResult ValidateCreate(
        Guid stockKeepingUnitId,
        string? value,
        BarcodeSymbology symbology)
    {
        List<DomainValidationFailure> errors = [];

        if (stockKeepingUnitId == Guid.Empty)
        {
            errors.Add(new(
                "SkuBarcode.StockKeepingUnitRequired",
                "SKU barcode must reference a stock keeping unit.",
                "stockKeepingUnitId"));
        }

        string normalizedValue = NormalizeValue(value);

        if (string.IsNullOrWhiteSpace(normalizedValue))
        {
            errors.Add(new(
                "SkuBarcode.ValueRequired",
                "SKU barcode value is required.",
                "value"));
        }
        else if (normalizedValue.Length > MaxValueLength)
        {
            errors.Add(new(
                "SkuBarcode.ValueTooLong",
                $"SKU barcode value must not exceed {MaxValueLength} characters.",
                "value"));
        }

        if (!Enum.IsDefined(symbology))
        {
            errors.Add(new(
                "SkuBarcode.SymbologyUnsupported",
                "SKU barcode symbology is not supported.",
                "symbology"));
        }

        return DomainValidationResult.From(errors);
    }

    public static DomainValidationResult ValidateDetails(
        string? value,
        BarcodeSymbology symbology,
        bool isPrimary,
        bool isActive)
    {
        List<DomainValidationFailure> errors = [];

        string normalizedValue = NormalizeValue(value);

        if (string.IsNullOrWhiteSpace(normalizedValue))
        {
            errors.Add(new(
                "SkuBarcode.ValueRequired",
                "SKU barcode value is required.",
                "value"));
        }
        else if (normalizedValue.Length > MaxValueLength)
        {
            errors.Add(new(
                "SkuBarcode.ValueTooLong",
                $"SKU barcode value must not exceed {MaxValueLength} characters.",
                "value"));
        }

        if (!Enum.IsDefined(symbology))
        {
            errors.Add(new(
                "SkuBarcode.SymbologyUnsupported",
                "SKU barcode symbology is not supported.",
                "symbology"));
        }

        if (!isActive && isPrimary)
        {
            errors.Add(new(
                "SkuBarcode.UnsupportedPrimaryChange",
                "Inactive SKU barcodes cannot be made primary.",
                "isPrimary"));
        }

        return DomainValidationResult.From(errors);
    }

    public static string NormalizeValue(string? value)
        => DomainText.NormalizeRequiredText(value);
}
