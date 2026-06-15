namespace Myrmex.Core.Domain.Validation;

public sealed record DomainValidationFailure(string Code, string Message, string? Field = null)
{
    public static DomainValidationFailure Required<TEntity>(string propertyName) => new(
        Code: $"{typeof(TEntity).Name}.{propertyName}-Required",
        Message: $"{propertyName} is required.",
        Field: propertyName);

    public static DomainValidationFailure TooLong<TEntity>(string propertyName, int maxLength) => new(
        Code: $"{typeof(TEntity).Name}.{propertyName}-TooLong",
        Message: $"{propertyName} must not exceed {maxLength} characters.",
        Field: propertyName);

    public static DomainValidationFailure MustBeNonNegative<TEntity>(string propertyName) => new(
        Code: $"{typeof(TEntity).Name}.{propertyName}-MustBeNonNegative",
        Message: $"{propertyName} must be a non-negative value.",
        Field: propertyName);
}