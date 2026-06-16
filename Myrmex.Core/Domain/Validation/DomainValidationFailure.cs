namespace Myrmex.Core.Domain.Validation;

public sealed record DomainValidationFailure(string Code, string Message, string? Field = null)
{
    public static DomainValidationFailure Required<TEntity>(string field) => new(
        Code: $"Required-{typeof(TEntity).Name}-{field}",
        Message: $"{field} is required.",
        Field: field);

    public static DomainValidationFailure Unsupported<TEntity>(string field) => new(
        Code: $"Unsupported-{typeof(TEntity).Name}-{field}",
        Message: $"{field} is not supported.",
        Field: field);

    public static DomainValidationFailure IncorrectState<TEntity>(string field) => new(
        Code: $"IncorrectState-{typeof(TEntity).Name}-{field}",
        Message: $"{field} is in an incorrect state.",
        Field: field);

    public static DomainValidationFailure TooLong<TEntity>(string field, int maxLength) => new(
        Code: $"TooLong-{typeof(TEntity).Name}-{field}",
        Message: $"{field} must not exceed {maxLength} characters.",
        Field: field);

    public static DomainValidationFailure MustBeNonNegative<TEntity>(string field) => new(
        Code: $"MustBeNonNegative-{typeof(TEntity).Name}-{field}",
        Message: $"{field} must be a non-negative value.",
        Field: field);
}