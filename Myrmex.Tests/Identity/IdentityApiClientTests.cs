using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Myrmex.Shared.Identity;
using Myrmex.WebApp.Identity;
using Myrmex.WebApp.Wms.Api;
using System.Net.Http.Json;
using System.Text.Json;

namespace Myrmex.Tests.Identity;

public sealed class IdentityApiClientTests
{
    [Fact]
    public async Task CreateUserAsync_PostsExpectedBodyAndMapsDetails()
    {
        IdentityUserDetails details = new(
            Guid.Parse("018f0000-0000-7000-8000-000000001001"),
            "operator@example.com",
            "Operator",
            [IdentityRoleNames.WmsOperator]);
        using StubHttpMessageHandler handler = new(_ =>
            new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = JsonContent.Create(details)
            });
        using HttpClient httpClient = CreateClient(handler);
        IdentityApiClient client = new(httpClient);

        ApiResult<IdentityUserDetails> result = await client.CreateUserAsync(
            new CreateIdentityUserRequest(
                "operator@example.com",
                "Operator",
                "Myrmex1!",
                [IdentityRoleNames.WmsOperator]),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(details.Id, result.Value.Id);
        Assert.Equal(details.Email, result.Value.Email);
        Assert.Equal(details.DisplayName, result.Value.DisplayName);
        Assert.Equal(details.Roles, result.Value.Roles);
        Assert.Equal(HttpMethod.Post, handler.RequestMethod);
        Assert.Equal("/api/identity/users", handler.RequestPath);
        using JsonDocument body = JsonDocument.Parse(handler.RequestBody);
        Assert.Equal("operator@example.com", body.RootElement.GetProperty("email").GetString());
        Assert.Equal("Operator", body.RootElement.GetProperty("displayName").GetString());
        Assert.Equal("Myrmex1!", body.RootElement.GetProperty("temporaryPassword").GetString());
        Assert.Equal(
            IdentityRoleNames.WmsOperator,
            body.RootElement.GetProperty("roles")[0].GetString());
    }

    [Fact]
    public async Task CreateUserAsync_WhenProblemReturned_MapsApiResultError()
    {
        ProblemDetails problem = new()
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Conflict",
            Detail = "An Identity user with this email already exists."
        };
        problem.Extensions["code"] = "IdentityUser.Duplicate";
        using StubHttpMessageHandler handler = new(_ =>
            new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = JsonContent.Create(problem)
            });
        using HttpClient httpClient = CreateClient(handler);
        IdentityApiClient client = new(httpClient);

        ApiResult<IdentityUserDetails> result = await client.CreateUserAsync(
            new CreateIdentityUserRequest(
                "operator@example.com",
                null,
                "Myrmex1!",
                [IdentityRoleNames.WmsOperator]),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(409, result.Error?.Status);
        Assert.Equal(
            "An Identity user with this email already exists.",
            result.Error?.Message);
        Assert.Equal("IdentityUser.Duplicate", result.Error?.Extensions["code"]);
    }

    [Fact]
    public async Task CreateUserAsync_PropagatesCancellation()
    {
        using HttpClient httpClient = CreateClient(new AsyncStubHttpMessageHandler(
            async cancellationToken =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.Created);
            }));
        IdentityApiClient client = new(httpClient);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.CreateUserAsync(
                new CreateIdentityUserRequest(
                    "operator@example.com",
                    null,
                    "Myrmex1!",
                    [IdentityRoleNames.WmsOperator]),
                cancellation.Token));
    }

    private static HttpClient CreateClient(HttpMessageHandler handler) =>
        new(handler)
        {
            BaseAddress = new Uri("https://api.example.test")
        };

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        public HttpMethod? RequestMethod { get; private set; }

        public string? RequestPath { get; private set; }

        public string RequestBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestMethod = request.Method;
            RequestPath = request.RequestUri?.AbsolutePath;
            if (request.Content is not null)
            {
                RequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            return handler(request);
        }
    }

    private sealed class AsyncStubHttpMessageHandler(
        Func<CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            handler(cancellationToken);
    }
}
