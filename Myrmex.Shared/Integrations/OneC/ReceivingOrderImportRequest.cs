namespace Myrmex.Shared.Integrations.OneC;

public sealed record ReceivingOrderImportRequest(DateOnly? StartDate, DateOnly? EndDate);
