using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Myrmex.Core.Results;

namespace Myrmex.AspNetCore.Results;

public static class ServiceResultHttpExtensions
{
    public static IResult ToHttpResult(this IServiceResult result)
    {
        if (result.IsSuccess)
        {
            return TypedResults.NoContent();
        }

        return result.Error.ToHttpResult();
    }

    public static IResult ToHttpResult<TValue>(this IServiceResult<TValue> result)
    {
        if (result.IsSuccess)
        {
            return TypedResults.Ok(result.Value);
        }

        return result.Error.ToHttpResult();
    }

    private static IResult ToHttpResult(this ServiceError error)
    {
        if (error.Type == ServiceErrorType.Invalid)
        {
            return CreateValidationProblemResult(error);
        }

        return CreateProblemResult(error);
    }

    private static IResult CreateProblemResult(ServiceError error)
    {
        int statusCode = GetStatusCode(error.Type);

        ProblemDetails problemDetails = new()
        {
            Type = GetTypeUri(statusCode),
            Title = GetTitle(statusCode),
            Status = statusCode,
            Detail = error.Message
        };

        problemDetails.Extensions["code"] = error.Code;

        if (!string.IsNullOrWhiteSpace(error.Field))
        {
            problemDetails.Extensions["field"] = error.Field;
        }

        return TypedResults.Json(
            problemDetails,
            statusCode: statusCode,
            contentType: "application/problem+json");
    }

    private static IResult CreateValidationProblemResult(ServiceError error)
    {
        const int statusCode = StatusCodes.Status400BadRequest;

        ValidationProblemDetails validationProblemDetails = new(error.ToValidationDictionary())
        {
            Type = GetTypeUri(statusCode),
            Title = "Validation failed",
            Status = statusCode,
            Detail = error.Message
        };

        validationProblemDetails.Extensions["code"] = error.Code;

        if (!string.IsNullOrWhiteSpace(error.Field))
        {
            validationProblemDetails.Extensions["field"] = error.Field;
        }

        return TypedResults.Json(
            validationProblemDetails,
            statusCode: statusCode,
            contentType: "application/problem+json");
    }

    private static int GetStatusCode(ServiceErrorType errorType)
    {
        return errorType switch
        {
            ServiceErrorType.Invalid => StatusCodes.Status400BadRequest,
            ServiceErrorType.NotFound => StatusCodes.Status404NotFound,
            ServiceErrorType.Conflict => StatusCodes.Status409Conflict,
            ServiceErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ServiceErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ServiceErrorType.Failure => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError
        };
    }

    private static string GetTitle(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => "Bad Request",
            StatusCodes.Status401Unauthorized => "Unauthorized",
            StatusCodes.Status403Forbidden => "Forbidden",
            StatusCodes.Status404NotFound => "Not Found",
            StatusCodes.Status409Conflict => "Conflict",
            StatusCodes.Status500InternalServerError => "Internal Server Error",
            _ => "Error"
        };
    }

    private static string GetTypeUri(int statusCode)
    {
        return $"https://httpstatuses.com/{statusCode}";
    }

    private static Dictionary<string, string[]> ToValidationDictionary(this ServiceError error)
    {
        IEnumerable<ServiceError> validationErrors = error.DetailList.Count > 0
            ? error.DetailList
            : [error];

        return validationErrors
            .GroupBy(e => e.Field ?? "_error")
            .ToDictionary(g => g.Key, g => g.Select(e => e.Message).ToArray());
    }
}