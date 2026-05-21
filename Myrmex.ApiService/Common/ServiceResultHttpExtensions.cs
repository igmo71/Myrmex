using Myrmex.Core.Results;

namespace Myrmex.ApiService.Common;

public static class ServiceResultHttpExtensions
{
    public static IResult ToHttpResult(this ServiceResult result)
    {
        if (result.IsSuccess)
            return Results.NoContent();

        return result.Error.ToHttpResult();
    }

    public static IResult ToHttpResult<TValue>(this ServiceResult<TValue> result)
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

            ServiceErrorType.Unauthorized => Results.Unauthorized(),

            ServiceErrorType.Forbidden => Results.Forbid(),

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
            .GroupBy(e => e.Field ?? string.Empty)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Message).ToArray());
    }

    private sealed record ProblemDetailsDto(string Code, string Message);
}
