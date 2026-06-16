namespace Myrmex.Core.Domain.Validation;

public sealed record DomainValidationFailure(string Code, string Message, string? Property = null)
{
    public static DomainValidationFailure Required<TEntity>(string property) => new(
        Code: $"Required-{typeof(TEntity).Name}-{property}",
        Message: $"{property} is required.",
        Property: property);

    public static DomainValidationFailure Unsupported<TEntity>(string property) => new(
        Code: $"Unsupported-{typeof(TEntity).Name}-{property}",
        Message: $"{property} is not supported.",
        Property: property);

    public static DomainValidationFailure IncorrectState<TEntity>(string property) => new(
        Code: $"IncorrectState-{typeof(TEntity).Name}-{property}",
        Message: $"{property} is in an incorrect state.",
        Property: property);

    public static DomainValidationFailure TooLong<TEntity>(string property, int maxLength) => new(
        Code: $"TooLong-{typeof(TEntity).Name}-{property}",
        Message: $"{property} must not exceed {maxLength} characters.",
        Property: property);

    public static DomainValidationFailure MustBeNonNegative<TEntity>(string property) => new(
        Code: $"MustBeNonNegative-{typeof(TEntity).Name}-{property}",
        Message: $"{property} must be a non-negative value.",
        Property: property);
}