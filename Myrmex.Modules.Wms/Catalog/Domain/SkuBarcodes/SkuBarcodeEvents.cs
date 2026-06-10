using Myrmex.Core.Events;

namespace Myrmex.Modules.Wms.Catalog.Domain.SkuBarcodes;

internal sealed record SkuBarcodeCreatedDomainEvent(Guid SkuBarcodeId) : IDomainEvent;

internal sealed record SkuBarcodeDetailsUpdatedDomainEvent(Guid SkuBarcodeId) : IDomainEvent;

internal sealed record SkuBarcodeDeactivatedDomainEvent(Guid SkuBarcodeId) : IDomainEvent;

internal sealed record SkuBarcodeReactivatedDomainEvent(Guid SkuBarcodeId) : IDomainEvent;
