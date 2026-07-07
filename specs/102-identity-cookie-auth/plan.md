# Implementation Plan: Identity Cookie Authentication

**Branch**: `102-introduce-aspnet-core-identity-cookie-authentication-for-myrmex-webappapi` | **Date**: 2026-07-07 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/102-identity-cookie-auth/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

Add a dedicated `Myrmex.Identity` platform capability backed by ASP.NET Core Identity and SQL Server. The WebApp owns login, logout, access-denied, current-user, and admin-create-user UI. The ApiService owns protected identity administration endpoints and remains the server-side authorization boundary for WMS, OneC, and demo-data operations.

The WebApp/ApiService authenticated-session strategy is explicit: the browser receives only the standard WebApp Identity application cookie. Before every server-side typed-client call, a WebApp delegating handler reloads the signed-in user and roles through `Myrmex.Identity`, creates a two-minute nonpersistent authentication ticket for a distinct `Myrmex.ApiSession` cookie scheme, protects it with the shared ASP.NET Core Data Protection key ring, and adds that cookie only to the internal ApiService request. ApiService validates the protected API-session cookie, builds the principal, applies its own policies, and resolves `IActorContext.ActorId` from the Identity user-id claim. ApiService is not publicly exposed by the production topology. Missing, invalid, tampered, or expired API-session cookies return 401; valid users without an eligible role return 403. No raw browser-cookie forwarding, user-id/role headers, JWT bearer token, ambient circuit `HttpContext`, or anonymous fallback is used.

## Technical Context

**Language/Version**: C# / .NET 10 (`net10.0`)

**Primary Dependencies**: ASP.NET Core Identity, ASP.NET Core cookie authentication and Data Protection, Entity Framework Core 10 with SQL Server, Blazor Web App interactive server rendering, MudBlazor 9, .NET Aspire 13, existing Myrmex dispatching/result helpers

**Storage**: Existing SQL Server `MyrmexDatabase` connection; a dedicated Identity DbContext and `identity` schema own users, roles, claims, tokens, and persisted Data Protection keys. WMS persistence remains in `WmsDbContext`.

**Testing**: xUnit v3 and Microsoft Testing Platform in `Myrmex.Tests`; focused unit, persistence, endpoint, WebApp-client-handler, and two-host authentication-boundary integration tests

**Target Platform**: ASP.NET Core Linux/Windows server deployment over HTTPS; browser-based interactive server WebApp with private server-to-server ApiService traffic

**Project Type**: Modular-monolith web application composed from separate WebApp and ApiService processes plus a new in-solution Identity capability project

**Performance Goals**: Meet SC-008 for account-page transitions; add no network round trip for session-ticket issuance; bound each typed-client request to one fresh Identity user/role reload plus normal ApiService work

**Constraints**: ApiService must remain independently authenticated and authorized; API-session tickets expire after two minutes and are never persisted in the browser; the shared Data Protection key ring is encrypted at rest with deployment-provided certificate material in production; production has no DevelopmentActor fallback; passwords and protected tickets are never logged; JWT/OIDC and full user management remain out of scope

**Scale/Scope**: Two fixed roles, explicit initial-admin bootstrap, one create-user flow, existing WMS/OneC/demo-data protected endpoints, current operator-scale deployment; no tenant, warehouse-level permission, or external-client model

## Constitution Check

*GATE: PASS before research; PASS after design.*

- **Domain Model First — PASS**: Identity is explicitly a platform capability rather than a WMS domain concern. Stable user identity, role assignment, bootstrap idempotency, and user-provisioning invariants are named. Existing WMS commands continue receiving `IActorContext.ActorId` without domain-model changes.
- **Modular Monolith Boundaries — PASS**: `Myrmex.Identity` is an in-process/in-solution capability, not a new service. It owns identity behavior and persistence. `Myrmex.Shared.Identity` contains only cross-process transport records and role-name constants. WebApp and ApiService compose the capability through public registration methods.
- **Vertical Slice Delivery — PASS**: Login/logout are WebApp account slices; create-user includes shared request/response records, an Identity-owned command/handler, ApiService endpoint, WebApp client, and admin page. Bootstrap is an Identity-owned startup slice. Public contracts remain separate from internal commands.
- **Testing Discipline — PASS**: The highest-risk boundary—the server-rendered WebApp principal becoming an independently authenticated ApiService principal—has a focused two-host integration test. Policy, actor, bootstrap, persistence, endpoint, and client-handler risks are tested at their lowest owning layer without duplicating every WMS endpoint matrix.
- **Simplicity and Observability — PASS**: The design uses built-in Identity, cookie authentication, Data Protection, EF Core, and the existing typed clients. The internal API-session cookie is necessary because interactive server components cannot safely depend on a request `HttpContext` or assume a browser cookie accompanies server-side `HttpClient` calls. Bootstrap and propagation failures are logged without secrets.

**Post-design re-check**: The data model, contracts, session-boundary contract, and validation guide preserve all five principles. No constitution exception is required.

## Project Structure

### Documentation (this feature)

```text
specs/102-identity-cookie-auth/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── identity-api.md
│   └── session-boundary.md
└── tasks.md                 # Generated later by /speckit-tasks
```

### Source Code (repository root)

```text
Myrmex.Identity/
├── Application/
│   ├── Users/CreateUser.cs
│   └── Bootstrap/InitialAdminSeeder.cs
├── Infrastructure/
│   ├── IdentityServiceCollectionExtensions.cs
│   ├── IdentityEndpointRouteBuilderExtensions.cs
│   └── Sessions/IdentityApiSessionTicketIssuer.cs
├── Persistence/
│   ├── MyrmexIdentityDbContext.cs
│   ├── MyrmexUser.cs
│   ├── MyrmexRole.cs
│   ├── Configurations/
│   └── Migrations/
└── Myrmex.Identity.csproj

Myrmex.Shared/Identity/
├── CreateIdentityUserRequest.cs
├── IdentityUserDetails.cs
└── IdentityRoleNames.cs

Myrmex.AspNetCore/Security/
├── MyrmexAuthenticationSchemes.cs
├── MyrmexAuthorizationPolicies.cs
└── HttpContextActorContext.cs

Myrmex.ApiService/
├── Program.cs
└── appsettings*.json

Myrmex.WebApp/
├── Components/
│   ├── Account/
│   │   ├── Login.razor
│   │   ├── Logout.razor
│   │   └── AccessDenied.razor
│   ├── Pages/Admin/Users/Create.razor
│   ├── Layout/MainLayout.razor
│   ├── Routes.razor
│   └── App.razor
├── Identity/
│   ├── IdentityApiAuthenticationHandler.cs
│   ├── IdentityApiClient.cs
│   └── IdentityRevalidatingAuthenticationStateProvider.cs
├── Program.cs
└── Resources/Localization/SharedResource*.resx

Myrmex.AppHost/AppHost.cs

Myrmex.Tests/
├── Identity/
│   ├── IdentityPersistenceTests.cs
│   ├── InitialAdminSeederTests.cs
│   ├── CreateUserTests.cs
│   └── IdentitySessionBoundaryTests.cs
├── AspNetCore/Security/
│   ├── MyrmexAuthorizationPolicyTests.cs
│   └── HttpContextActorContextTests.cs
└── Testing/TestAuthentication.cs
```

**Structure Decision**: Add one class-library project, `Myrmex.Identity`, as the dedicated platform identity capability required by the feature. It owns Identity models, EF persistence, user provisioning, bootstrap, role initialization, and API-session ticket issuance. It does not become a separately deployed service. Cross-process DTOs live in `Myrmex.Shared.Identity`; reusable policy and actor helpers remain in `Myrmex.AspNetCore`. WebApp and ApiService retain their current host responsibilities.

## Architectural Design Notes

- **Platform concepts first**: `User Account`, `Role`, `User Role Assignment`, `Authenticated Browser Session`, `Internal API Session`, and `Initial Administrator Configuration` drive the design. Identity does not enter `Myrmex.Modules.Wms` or `WmsDbContext`.
- **Browser session ownership**: WebApp registers the Identity application cookie as its default authenticate/challenge scheme. Login and logout execute as normal HTTP POST account flows with antiforgery and safe local return URLs, not as attempts to modify response cookies from an established interactive circuit. Account pages opt out of interactive routing, and `App.razor` selects a null render mode for excluded routes so those requests execute with static server rendering and an active HTTP response.
- **Explicit WebApp/ApiService boundary**: ApiService registers `Myrmex.ApiSession` as its production authenticate scheme and returns status codes instead of login redirects. `IdentityApiAuthenticationHandler` obtains the circuit principal through `AuthenticationStateProvider`, requires a valid Identity user-id claim, reloads that user and current roles from Identity storage, verifies the account can sign in, protects a new two-minute `AuthenticationTicket` through the configured `Myrmex.ApiSession` cookie ticket format, and adds it to the outgoing internal request. The selected ticket is scoped to the API-session scheme and is not the browser application cookie.
- **Trust and key management**: WebApp and ApiService use the same Data Protection application name and persist the key ring through `MyrmexIdentityDbContext`. Production key XML is encrypted at rest with the same deployment-provided X.509 certificate available to both hosts; production startup fails when required key-protection configuration is absent. Only those two trusted applications receive the key-ring/database/certificate configuration. ApiService stays private in AppHost/deployment networking. Internal traffic uses HTTPS. The ticket contains user-id, display claims, current role claims, issued time, and expiry; it contains no password or arbitrary client-supplied claims.
- **Independent API enforcement**: ApiService validates ticket protection, scheme, and expiry before authorization. The `WmsOperator` policy requires authentication, a parseable non-empty Identity user-id claim, and either `WmsOperator` or `MyrmexAdmin`. A separate `MyrmexAdmin` policy protects user creation. `HttpContextActorContext` continues to resolve only stable user-id claims.
- **Session failure behavior**: An anonymous WebApp circuit does not emit an API-session cookie. If authentication is required, the WebApp routes the user to login. ApiService returns 401 for missing, malformed, tampered, or expired API-session tickets and 403 for authenticated principals lacking the policy role. The WebApp API error layer preserves these distinctions and never retries with DevelopmentActor.
- **Typed-client registration**: Attach `IdentityApiAuthenticationHandler` to every WebApp client that calls protected ApiService routes: WMS topology, catalog, inventory, OneC integration, and identity administration. Public/unprotected clients do not receive a ticket unless they are intentionally registered through the protected-client path.
- **Revocation behavior**: A fresh Identity principal is created from persistent user and role state for every outgoing typed-client request. Deleted users, users no longer allowed to sign in, and removed roles therefore stop producing eligible API-session tickets. A WebApp revalidating authentication-state provider periodically invalidates stale browser sessions; ApiService remains the final authorization boundary even before UI state refreshes.
- **CSRF boundary**: The application cookie is accepted only by WebApp account/UI endpoints, which use antiforgery for state-changing browser requests. The API-session cookie is generated and attached only by the trusted WebApp server and is never set in the browser. ApiService remains private, so browser-originated cookie CSRF is not part of the API-session path.
- **Shared contract boundary**: `CreateIdentityUserRequest` and `IdentityUserDetails` live under `Myrmex.Shared.Identity`; they contain only email, optional display name, temporary password on the request, supported role names, stable user ID, and non-sensitive result fields. Password hashes, security stamps, protected tickets, EF types, Identity framework types, and MudBlazor types never cross this boundary.
- **Internal request boundary**: `CreateUser.Command`, role initialization, initial-admin seeding, and ticket issuance remain inside `Myrmex.Identity`. ApiService endpoint code maps the public request to the command and maps the service result to existing ProblemDetails conventions.
- **Persistence**: `MyrmexIdentityDbContext` uses the existing SQL Server connection but owns an `identity` schema and its own migrations. It uses GUID user/role keys and implements Data Protection key persistence. Migration generation/application remain explicit developer actions; runtime startup does not auto-migrate.
- **Initial roles and administrator**: ApiService is the single startup owner for role/bootstrap initialization. It idempotently ensures `MyrmexAdmin` and `WmsOperator` roles exist. Initial-admin options validate on start only when enabled. The password is supplied by environment/user-secrets/deployment secrets, never committed or logged. An existing matching user may receive a missing admin role but never has its password replaced.
- **User creation transaction**: The admin-only create-user handler normalizes and validates the email, validates requested roles against the fixed set, creates the user, and assigns all roles within one database transaction. Any role-assignment failure rolls back user creation. Duplicate normalized identity returns a conflict; password/field/role validation returns a bad request.
- **DevelopmentActor**: Identity is the default in production. DevelopmentActor registration is conditional on Development or Staging plus explicit enablement and emits the stable actor claim plus `WmsOperator` role needed by the strengthened policy. Test authentication remains a separate test-only scheme whose options explicitly control actor ID and roles. Neither scheme can be selected as a production fallback.
- **Localization**: All account, access-denied, current-user, and create-user text uses semantic resource keys present in neutral, `en-US`, and `ru-RU` resources. API/domain identifiers and role codes remain stable, non-localized values.
- **Cancellation and errors**: Create-user cancellation propagates WebApp → typed client → ApiService → Identity handler → EF. Expected cancellation is not shown as a user error. Writes return `ApiResult<IdentityUserDetails>` and ProblemDetails; login uses a generic invalid-credentials message. Bootstrap and session propagation log outcome categories without password/ticket contents.
- **Risk-based testing**: Automated coverage targets policy role logic, stable actor resolution, Identity uniqueness/transaction behavior, bootstrap safety/idempotency, the outbound ticket handler, and the two-host cookie boundary. Existing WMS endpoint suites retain representative authorization coverage; the same role matrix is not copied across every endpoint. UI markup/layout is manually smoke-tested because no component-test framework exists, while login HTTP behavior and admin endpoint authorization receive integration coverage.
- **Existing pattern precedence**: Use existing `ApiResult<T>`/ProblemDetails write conventions, typed API clients, explicit request/handler slices, `IActorContext`, authorization-policy constants, localization resources, EF migrations, and Aspire service discovery. No generic auth framework or identity server is introduced.
