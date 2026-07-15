using System.Globalization;
using Myrmex.Integrations.Synchronization;

namespace Myrmex.Integrations.OneC.Notifications;

internal sealed class OneCChangeNotificationValidator
{
    private static readonly string[] DateFormats =
    [
        "yyyy-MM-dd'T'HH:mm:ss",
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF"
    ];

    public OneCChangeNotificationValidationResult Validate(
        OneCChangeNotificationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        Dictionary<string, string[]> errors = [];

        Guid refKey = ValidateRefKey(request.RefKey, errors);
        byte[] dataVersion = ValidateDataVersion(request.DataVersion, errors);
        DateTime? documentDate = ValidateDate(request.Date, errors);
        ValidateNumber(request.Number, errors);

        if (errors.Count > 0)
        {
            return OneCChangeNotificationValidationResult.Invalid(errors);
        }

        return OneCChangeNotificationValidationResult.Valid(
            refKey,
            dataVersion,
            documentDate);
    }

    private static Guid ValidateRefKey(
        string? value,
        Dictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors["Ref_Key"] = ["Ref_Key is required."];
            return Guid.Empty;
        }

        if (!Guid.TryParse(value, out Guid refKey) || refKey == Guid.Empty)
        {
            errors["Ref_Key"] = ["Ref_Key must be a valid non-empty GUID."];
            return Guid.Empty;
        }

        return refKey;
    }

    private static byte[] ValidateDataVersion(
        string? value,
        Dictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors["DataVersion"] = ["DataVersion is required."];
            return [];
        }

        byte[] decoded;
        try
        {
            decoded = Convert.FromBase64String(value);
        }
        catch (FormatException)
        {
            errors["DataVersion"] = ["DataVersion must be valid Base64."];
            return [];
        }

        if (decoded.Length == 0)
        {
            errors["DataVersion"] = ["DataVersion must decode to a non-empty value."];
            return [];
        }

        if (decoded.Length > SynchronizationRequest.ExternalDataVersionMaxLength)
        {
            errors["DataVersion"] =
            [
                $"DataVersion must decode to no more than " +
                $"{SynchronizationRequest.ExternalDataVersionMaxLength} bytes."
            ];
        }

        return decoded;
    }

    private static void ValidateNumber(
        string? value,
        Dictionary<string, string[]> errors)
    {
        if (value is not null &&
            value.Length > SynchronizationRequest.ExternalDocumentNumberMaxLength)
        {
            errors["Number"] =
            [
                $"Number must not exceed " +
                $"{SynchronizationRequest.ExternalDocumentNumberMaxLength} characters."
            ];
        }
    }

    private static DateTime? ValidateDate(
        string? value,
        Dictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!DateTime.TryParseExact(
            value,
            DateFormats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime parsed))
        {
            errors["Date"] = ["Date must be a valid source-local date value."];
            return null;
        }

        return DateTime.SpecifyKind(parsed, DateTimeKind.Unspecified);
    }
}

internal sealed class OneCChangeNotificationValidationResult
{
    private OneCChangeNotificationValidationResult(
        bool succeeded,
        IReadOnlyDictionary<string, string[]> errors,
        Guid refKey,
        byte[] dataVersion,
        DateTime? documentDate)
    {
        Succeeded = succeeded;
        Errors = errors;
        RefKey = refKey;
        DataVersion = dataVersion;
        DocumentDate = documentDate;
    }

    public bool Succeeded { get; }

    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public Guid RefKey { get; }

    public byte[] DataVersion { get; }

    public DateTime? DocumentDate { get; }

    public static OneCChangeNotificationValidationResult Valid(
        Guid refKey,
        byte[] dataVersion,
        DateTime? documentDate) =>
        new(
            succeeded: true,
            new Dictionary<string, string[]>(),
            refKey,
            dataVersion,
            documentDate);

    public static OneCChangeNotificationValidationResult Invalid(
        IReadOnlyDictionary<string, string[]> errors) =>
        new(
            succeeded: false,
            errors,
            Guid.Empty,
            [],
            null);
}
