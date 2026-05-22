using Microsoft.AspNetCore.Http;
using Myrmex.Core.Results;

namespace Myrmex.Modules.Wms.Common.Http;

internal static class ServiceResultHttpExtensions
{
    public static IResult ToHttpResult(this IServiceResult result)
    {
        if (result.IsSuccess)
            return Results.NoContent();

        return result.Error.ToHttpResult();
    }

    public static IResult ToHttpResult<TValue>(this IServiceResult<TValue> result)
    {
        if (result.IsSuccess)
            return Results.Ok(result.Value);

        return result.Error.ToHttpResult();
    }

    private static IResult ToHttpResult(this ServiceError error)
    {
        return error.Type switch
        {
            ServiceErrorType.Invalid => Results.ValidationProblem(
                errors: error.ToValidationDictionary(),
                title: error.Message,
                type: error.Code),

            ServiceErrorType.NotFound => Results.NotFound(new ProblemDetailsDto(
                error.Code,
                error.Message)),

            ServiceErrorType.Conflict => Results.Conflict(new ProblemDetailsDto(
                error.Code,
                error.Message)),

            ServiceErrorType.Unauthorized => Results.Problem(
                title: error.Message,
                type: error.Code,
                statusCode: StatusCodes.Status401Unauthorized),

            ServiceErrorType.Forbidden => Results.Problem(
                title: error.Message,
                type: error.Code,
                statusCode: StatusCodes.Status403Forbidden),

            ServiceErrorType.Failure => Results.Problem(
                title: error.Message,
                type: error.Code,
                statusCode: StatusCodes.Status500InternalServerError),

            _ => Results.Problem(
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