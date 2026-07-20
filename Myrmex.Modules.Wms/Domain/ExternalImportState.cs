namespace Myrmex.Modules.Wms.Domain;

internal sealed class ExternalImportState
{
    public const int MaxDataVersionLength = 128;

    private byte[]? _dataVersion;

    private ExternalImportState()
    {
    }

    private ExternalImportState(
        Guid refKey,
        byte[]? dataVersion,
        DateTimeOffset importedAtUtc)
    {
        RefKey = ValidateRefKey(refKey);
        DataVersion = dataVersion;
        ImportedAtUtc = importedAtUtc.ToUniversalTime();
    }

    public Guid RefKey { get; private set; }

    public byte[]? DataVersion
    {
        get => _dataVersion?.ToArray();
        private set => _dataVersion = value?.ToArray();
    }

    public DateTimeOffset ImportedAtUtc { get; private set; }

    public static ExternalImportState Create(
        Guid refKey,
        byte[] dataVersion,
        DateTimeOffset importedAtUtc)
    {
        ValidateDataVersion(dataVersion);
        return new ExternalImportState(refKey, dataVersion, importedAtUtc);
    }

    internal static ExternalImportState Restore(
        Guid refKey,
        byte[]? dataVersion,
        DateTimeOffset importedAtUtc)
    {
        if (dataVersion is not null)
        {
            ValidateDataVersion(dataVersion);
        }

        return new ExternalImportState(refKey, dataVersion, importedAtUtc);
    }

    public bool HasDataVersion(ReadOnlySpan<byte> dataVersion) =>
        _dataVersion is not null && dataVersion.SequenceEqual(_dataVersion);

    public void RecordImport(
        byte[] dataVersion,
        DateTimeOffset importedAtUtc)
    {
        ValidateDataVersion(dataVersion);
        DataVersion = dataVersion;
        ImportedAtUtc = importedAtUtc.ToUniversalTime();
    }

    private static Guid ValidateRefKey(Guid refKey) =>
        refKey != Guid.Empty
            ? refKey
            : throw new ArgumentException("External reference key is required.", nameof(refKey));

    private static void ValidateDataVersion(byte[]? dataVersion)
    {
        ArgumentNullException.ThrowIfNull(dataVersion);

        if (dataVersion.Length is < 1 or > MaxDataVersionLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dataVersion),
                $"External data version length must be between 1 and {MaxDataVersionLength} bytes.");
        }
    }
}
