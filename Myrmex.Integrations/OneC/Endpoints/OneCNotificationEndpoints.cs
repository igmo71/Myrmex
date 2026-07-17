using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Myrmex.AspNetCore.Security;
using Myrmex.Integrations.OneC.Notifications;
using Myrmex.Integrations.Synchronization;

namespace Myrmex.Integrations.OneC.Endpoints;

public static class OneCNotificationEndpoints
{
    private const string LoggerCategory = "Myrmex.Integrations.OneC.Notifications";

    public static IEndpointRouteBuilder MapOneCNotificationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints
            .MapGroup("/api/integrations/1c")
            .WithTags("Integrations 1C")
            .RequireAuthorization(MyrmexAuthorizationPolicies.OneCIntegration);

        group.MapPost(
            "/receiving-orders/changed",
            (
                [FromBody] OneCChangeNotificationRequest request,
                OneCChangeNotificationValidator validator,
                SynchronizationRequestFactory factory,
                SynchronizationRequestStore store,
                ILoggerFactory loggerFactory,
                CancellationToken cancellationToken) =>
                AcceptAsync(
                    request,
                    SynchronizationEntityTypes.ReceivingOrder,
                    validator,
                    factory,
                    store,
                    loggerFactory,
                    cancellationToken))
            .WithName("AcceptOneCReceivingOrderChanged")
            .WithSummary("Accept a 1C receiving order change notification")
            .WithDescription(
                "Persists the notification and returns an empty 202 Accepted response after durable commit. Malformed notifications return validation problem details.")
            .Produces(StatusCodes.Status202Accepted)
            .ProducesValidationProblem();

        group.MapPost(
            "/shipping-orders/changed",
            (
                [FromBody] OneCChangeNotificationRequest request,
                OneCChangeNotificationValidator validator,
                SynchronizationRequestFactory factory,
                SynchronizationRequestStore store,
                ILoggerFactory loggerFactory,
                CancellationToken cancellationToken) =>
                AcceptAsync(
                    request,
                    SynchronizationEntityTypes.ShippingOrder,
                    validator,
                    factory,
                    store,
                    loggerFactory,
                    cancellationToken))
            .WithName("AcceptOneCShippingOrderChanged")
            .WithSummary("Accept a 1C shipping order change notification")
            .WithDescription(
                "Persists the notification and returns an empty 202 Accepted response after durable commit. Malformed notifications return validation problem details.")
            .Produces(StatusCodes.Status202Accepted)
            .ProducesValidationProblem();

        MapReferenceChanged(
            group,
            "/warehouses/changed",
            "AcceptOneCWarehouseChanged",
            "warehouse",
            SynchronizationEntityTypes.Warehouse);
        MapReferenceChanged(
            group,
            "/uoms/changed",
            "AcceptOneCUnitOfMeasureChanged",
            "unit of measure",
            SynchronizationEntityTypes.UnitOfMeasure);
        MapReferenceChanged(
            group,
            "/skus/changed",
            "AcceptOneCStockKeepingUnitChanged",
            "stock keeping unit",
            SynchronizationEntityTypes.StockKeepingUnit);

        return endpoints;
    }

    private static void MapReferenceChanged(
        RouteGroupBuilder group,
        string route,
        string endpointName,
        string referenceName,
        string entityType)
    {
        group.MapPost(
            route,
            (
                [FromBody] OneCChangeNotificationRequest request,
                OneCChangeNotificationValidator validator,
                SynchronizationRequestFactory factory,
                SynchronizationRequestStore store,
                ILoggerFactory loggerFactory,
                CancellationToken cancellationToken) =>
                AcceptAsync(
                    request,
                    entityType,
                    validator,
                    factory,
                    store,
                    loggerFactory,
                    cancellationToken))
            .WithName(endpointName)
            .WithSummary($"Accept a 1C {referenceName} change notification")
            .WithDescription(
                "Persists the notification and returns an empty 202 Accepted response after durable commit. Malformed notifications return validation problem details.")
            .Produces(StatusCodes.Status202Accepted)
            .ProducesValidationProblem();
    }

    private static async Task<IResult> AcceptAsync(
        OneCChangeNotificationRequest request,
        string entityType,
        OneCChangeNotificationValidator validator,
        SynchronizationRequestFactory factory,
        SynchronizationRequestStore store,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        ILogger logger = loggerFactory.CreateLogger(LoggerCategory);

        OneCChangeNotificationValidationResult validation =
            validator.Validate(request);
        if (!validation.Succeeded)
        {
            logger.LogWarning(
                "Rejected 1C {EntityType} change notification with validation fields {ValidationFields}.",
                entityType,
                string.Join(",", validation.Errors.Keys));

            return TypedResults.ValidationProblem(
                validation.Errors.ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value));
        }

        SynchronizationRequest synchronizationRequest =
            factory.Create(request, validation, entityType);
        SynchronizationRequestIntakeResult intakeResult =
            await store.InsertAsync(synchronizationRequest, cancellationToken);

        logger.LogInformation(
            "Accepted 1C {EntityType} change notification as synchronization request {SynchronizationRequestId}.",
            entityType,
            intakeResult.Request.Id);

        return TypedResults.StatusCode(StatusCodes.Status202Accepted);
    }
}
