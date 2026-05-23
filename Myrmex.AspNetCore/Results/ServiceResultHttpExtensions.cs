using Microsoft.AspNetCore.Http;
using Myrmex.Core.Results;

namespace Myrmex.AspNetCore.Results;

public static class ServiceResultHttpExtensions
{
    public static IResult ToHttpResult(this IServiceResult result)
    {
        if (result.IsSuccess)
            return TypedResults.NoContent();

        return result.Error.ToHttpResult();
    }

    public static IResult ToHttpResult<TValue>(this IServiceResult<TValue> result)
    {
        if (result.IsSuccess)
            return TypedResults.Ok(result.Value);

        return result.Error.ToHttpResult();
    }

    private static IResult ToHttpResult(this ServiceError error)
    {
        return error.Type switch
        {
            ServiceErrorType.Invalid => TypedResults.ValidationProblem(
                errors: error.ToValidationDictionary(),
                title: error.Message,
                type: error.Code),

            ServiceErrorType.NotFound => TypedResults.NotFound(new ProblemDetailsDto(
                error.Code,
                error.Message)),

            ServiceErrorType.Conflict => TypedResults.Conflict(new ProblemDetailsDto(
                error.Code,
                error.Message)),

            ServiceErrorType.Unauthorized => TypedResults.Problem(
                title: error.Message,
                type: error.Code,
                statusCode: StatusCodes.Status401Unauthorized),

            ServiceErrorType.Forbidden => TypedResults.Problem(
                title: error.Message,
                type: error.Code,
                statusCode: StatusCodes.Status403Forbidden),

            ServiceErrorType.Failure => TypedResults.Problem(
                title: error.Message,
                type: error.Code,
                statusCode: StatusCodes.Status500InternalServerError),

            _ => TypedResults.Problem(
                title: "An unexpected error occurred.",
                statusCode: StatusCodes.Status500InternalServerError)
        };
    }

    private static Dictionary<string, string[]> ToValidationDictionary(this ServiceError error)
    {
        IEnumerable<ServiceError> validationErrors = error.DetailList.Count > 0 ? error.DetailList : [error];

        return validationErrors
            .GroupBy(e => e.Field ?? "_error")
            .ToDictionary(g => g.Key, g => g.Select(e => e.Message).ToArray());
    }

    private sealed record ProblemDetailsDto(string Code, string Message);
}