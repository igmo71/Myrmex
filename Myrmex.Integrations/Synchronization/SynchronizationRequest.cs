namespace Myrmex.Integrations.Synchronization;

internal sealed class SynchronizationRequest
{
    public const int SourceSystemMaxLength = 32;
    public const int SourceInstanceMaxLength = 128;
    public const int EntityTypeMaxLength = 32;
    public const int ExternalIdMaxLength = 128;
    public const int ExternalDataVersionMaxLength = 128;
    public const int ExternalDocumentNumberMaxLength = 64;
    public const int TriggerMaxLength = 32;
    public const int StatusMaxLength = 32;
    public const int LastErrorMaxLength = 2048;

    public Guid Id { get; set; } = Guid.NewGuid();

    public required string SourceSystem { get; set; }

    public required string SourceInstance { get; set; }

    public required string EntityType { get; set; }

    public required string ExternalId { get; set; }

    public required byte[] ExternalDataVersion { get; set; }

    public string? ExternalDocumentNumber { get; set; }

    public DateTime? ExternalDocumentDate { get; set; }

    public string Trigger { get; set; } =
        SynchronizationTriggers.ChangeNotification;

    public SynchronizationStatus Status { get; set; } =
        SynchronizationStatus.Pending;

    public DateTimeOffset ReceivedAtUtc { get; set; }

    public DateTimeOffset? ProcessingStartedAtUtc { get; set; }

    public DateTimeOffset? CompletedAtUtc { get; set; }

    public int AttemptCount { get; set; }

    public DateTimeOffset? NextAttemptAtUtc { get; set; }

    public string? LastError { get; set; }
}
