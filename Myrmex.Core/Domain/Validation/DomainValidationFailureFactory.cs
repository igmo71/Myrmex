namespace Myrmex.Core.Domain.Validation;

public static class DomainValidationFailureFactory
{
    public static DomainValidationFailure Required<TEntity>(string propertyName)
    {
        string entityName = typeof(TEntity).Name;
        return new DomainValidationFailure(
            Code: $"{entityName}.{propertyName}-Required",
            Message: $"{propertyName} is required.",
            Field: propertyName
        );
    }

    public static DomainValidationFailure TooLong<TEntity>(string propertyName, int maxLength)
    {
        string entityName = typeof(TEntity).Name;
        return new DomainValidationFailure(
            Code: $"{entityName}.{propertyName}-TooLong",
            Message: $"{propertyName} must not exceed {maxLength} characters.",
            Field: propertyName
        );
    }

    public static DomainValidationFailure MustBeNonNegative<TEntity>(string propertyName)
    {
        string entityName = typeof(TEntity).Name;
        return new DomainValidationFailure(
            Code: $"{entityName}.{propertyName}-MustBeNonNegative",
            Message: $"{propertyName} must be a non-negative value.",
            Field: propertyName
        );
    }
}