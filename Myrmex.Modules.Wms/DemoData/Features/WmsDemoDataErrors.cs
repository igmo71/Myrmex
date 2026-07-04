using Myrmex.Core.Results;

namespace Myrmex.Modules.Wms.DemoData.Features;

internal static class WmsDemoDataErrors
{
    public static ServiceError ConfirmationRequired() => new(
        ServiceErrorType.Invalid,
        "DemoData.ConfirmationRequired",
        "Demo data clear confirmation is required.",
        "Confirmation");

    public static ServiceError ClearForbidden() => new(
        ServiceErrorType.Forbidden,
        "DemoData.ClearForbidden",
        "Demo data clearing is not permitted.");

    public static ServiceError InvalidConfiguration() => new(
        ServiceErrorType.Invalid,
        "DemoData.InvalidConfiguration",
        "Demo data clear confirmation is not configured.");

    public static ServiceError OperationInProgress() => new(
        ServiceErrorType.Conflict,
        "DemoData.OperationInProgress",
        "Another demo data operation is already in progress.");

    public static ServiceError IdentityConflict(string area, string identity) => new(
        ServiceErrorType.Conflict,
        "DemoData.IdentityConflict",
        "A stable demo data identity belongs to incompatible data.");

    public static ServiceError DatabaseNotReady() => new(
        ServiceErrorType.Failure,
        "DemoData.DatabaseNotReady",
        "The demo database schema is unavailable or not current.");

    public static ServiceError ExecutionFailed() => new(
        ServiceErrorType.Failure,
        "DemoData.ExecutionFailed",
        "The demo data operation could not be completed.");
}
