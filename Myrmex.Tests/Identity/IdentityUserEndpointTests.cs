using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Results;
using Myrmex.Identity.Application.Users;
using Myrmex.Identity.Infrastructure;
using Myrmex.Shared.Identity;
using System.Net.Http.Json;

namespace Myrmex.Tests.Identity;

public sealed class IdentityUserEndpointTests
{
    [Fact]
    public async Task CreateUser_WhenAdmin_Returns201AndSerializedUser()
    {
        IdentityUserDetails details = new(
            Guid.Parse("018f0000-0000-7000-8000-000000001001"),
            "operator@example.com",
            "Operator",
            [IdentityRoleNames.WmsOperator]);
        RecordingCommandDispatcher dispatcher = RecordingCommandDispatcher.Success(details);
        await using WebApplication app = CreateApp(dispatcher);
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpResponseMessage response = await SendCreateAsync(
            app,
            app.Services.CreateApiSessionCookie([IdentityRoleNames.MyrmexAdmin]),
            new CreateIdentityUserRequest(
                "operator@example.com",
                "Operator",
                "Myrmex1!",
                [IdentityRoleNames.WmsOperator]));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"/api/identity/users/{details.Id}", response.Headers.Location?.ToString());
        IdentityUserDetails? body = await response.Content
            .ReadFromJsonAsync<IdentityUserDetails>(TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Equal(details.Id, body.Id);
        Assert.Equal(details.Email, body.Email);
        Assert.Equal(details.DisplayName, body.DisplayName);
        Assert.Equal(details.Roles.ToArray(), body.Roles.ToArray());
        Assert.Equal(1, dispatcher.CallCount);
        Assert.Equal("operator@example.com", dispatcher.LastCommand?.Email);
        Assert.Equal("Myrmex1!", dispatcher.LastCommand?.TemporaryPassword);
    }

    [Fact]
    public async Task CreateUser_WhenAnonymous_Returns401WithoutDispatch()
    {
        RecordingCommandDispatcher dispatcher = RecordingCommandDispatcher.Success();
        await using WebApplication app = CreateApp(dispatcher);
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpResponseMessage response = await SendCreateAsync(
            app,
            cookie: null,
            CreateRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, dispatcher.CallCount);
    }

    [Fact]
    public async Task CreateUser_WhenNonAdmin_Returns403WithoutDispatch()
    {
        RecordingCommandDispatcher dispatcher = RecordingCommandDispatcher.Success();
        await using WebApplication app = CreateApp(dispatcher);
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpResponseMessage response = await SendCreateAsync(
            app,
            app.Services.CreateApiSessionCookie([IdentityRoleNames.WmsOperator]),
            CreateRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, dispatcher.CallCount);
    }

    [Fact]
    public async Task CreateUser_WhenValidationFails_Returns400ProblemDetails()
    {
        RecordingCommandDispatcher dispatcher = RecordingCommandDispatcher.Failure(
            new ServiceError(
                ServiceErrorType.Invalid,
                "IdentityUser.Validation",
                "One or more validation errors occurred.",
                Details:
                [
                    new ServiceError(
                        ServiceErrorType.Invalid,
                        "IdentityUser.EmailRequired",
                        "Email is required.",
                        nameof(CreateUser.Command.Email))
                ]));
        await using WebApplication app = CreateApp(dispatcher);
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpResponseMessage response = await SendCreateAsync(
            app,
            app.Services.CreateApiSessionCookie([IdentityRoleNames.MyrmexAdmin]),
            CreateRequest());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        ValidationProblemDetails? problem = await response.Content
            .ReadFromJsonAsync<ValidationProblemDetails>(TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        Assert.Equal(400, problem.Status);
        Assert.Equal("IdentityUser.Validation", problem.Extensions["code"]?.ToString());
        Assert.True(problem.Errors.ContainsKey(nameof(CreateUser.Command.Email)));
    }

    [Fact]
    public async Task CreateUser_WhenDuplicate_Returns409ProblemDetails()
    {
        RecordingCommandDispatcher dispatcher = RecordingCommandDispatcher.Failure(
            new ServiceError(
                ServiceErrorType.Conflict,
                "IdentityUser.Duplicate",
                "An Identity user with this email already exists.",
                nameof(CreateUser.Command.Email)));
        await using WebApplication app = CreateApp(dispatcher);
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpResponseMessage response = await SendCreateAsync(
            app,
            app.Services.CreateApiSessionCookie([IdentityRoleNames.MyrmexAdmin]),
            CreateRequest());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        ProblemDetails? problem = await response.Content
            .ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);
        Assert.Equal(409, problem?.Status);
        Assert.Equal("IdentityUser.Duplicate", problem?.Extensions["code"]?.ToString());
    }

    [Fact]
    public async Task CreateUser_ResponseDoesNotLeakSensitiveRequestValues()
    {
        const string temporaryPassword = "DoNotReturn1!";
        RecordingCommandDispatcher dispatcher = RecordingCommandDispatcher.Success();
        await using WebApplication app = CreateApp(dispatcher);
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpResponseMessage response = await SendCreateAsync(
            app,
            app.Services.CreateApiSessionCookie([IdentityRoleNames.MyrmexAdmin]),
            new CreateIdentityUserRequest(
                "operator@example.com",
                null,
                temporaryPassword,
                [IdentityRoleNames.WmsOperator]));

        string body = await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);
        Assert.DoesNotContain(temporaryPassword, body, StringComparison.Ordinal);
        Assert.DoesNotContain("passwordHash", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("securityStamp", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cookie", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ticket", body, StringComparison.OrdinalIgnoreCase);
    }

    private static WebApplication CreateApp(RecordingCommandDispatcher dispatcher)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton<ICommandDispatcher>(dispatcher);
        builder.Services.AddTestApiSessionAuthentication();

        WebApplication app = builder.Build();
        app.UseTestApiSessionAuthentication();
        app.MapMyrmexIdentityEndpoints();
        return app;
    }

    private static async Task<HttpResponseMessage> SendCreateAsync(
        WebApplication app,
        string? cookie,
        CreateIdentityUserRequest request)
    {
        using HttpClient client = CreateClient(app);
        using HttpRequestMessage message = new(
            HttpMethod.Post,
            "/api/identity/users")
        {
            Content = JsonContent.Create(request)
        };
        if (cookie is not null)
        {
            message.Headers.TryAddWithoutValidation("Cookie", cookie);
        }

        return await client.SendAsync(
            message,
            TestContext.Current.CancellationToken);
    }

    private static HttpClient CreateClient(WebApplication app)
    {
        string address = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses.Single();
        return new HttpClient { BaseAddress = new Uri(address) };
    }

    private static CreateIdentityUserRequest CreateRequest() =>
        new("operator@example.com", null, "Myrmex1!", [IdentityRoleNames.WmsOperator]);

    private sealed class RecordingCommandDispatcher(
        ServiceResult<IdentityUserDetails> result) : ICommandDispatcher
    {
        public int CallCount { get; private set; }

        public CreateUser.Command? LastCommand { get; private set; }

        public static RecordingCommandDispatcher Success(
            IdentityUserDetails? details = null) =>
            new(ServiceResult<IdentityUserDetails>.Success(details ?? new IdentityUserDetails(
                Guid.Parse("018f0000-0000-7000-8000-000000001001"),
                "operator@example.com",
                null,
                [IdentityRoleNames.WmsOperator])));

        public static RecordingCommandDispatcher Failure(ServiceError error) =>
            new(ServiceResult<IdentityUserDetails>.Fail(error));

        public Task<TResult> DispatchAsync<TCommand, TResult>(
            TCommand command,
            CancellationToken cancellationToken = default)
            where TCommand : ICommand<TResult>
            where TResult : IServiceResult
        {
            CallCount++;
            LastCommand = Assert.IsType<CreateUser.Command>(command);
            return Task.FromResult((TResult)(object)result);
        }
    }
}
