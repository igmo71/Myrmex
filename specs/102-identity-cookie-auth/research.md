# Phase 0 Research: Identity Cookie Authentication

## Decision 1: Identity capability boundary

**Decision**: Create `Myrmex.Identity` as an in-solution class library owning Identity user/role types, Identity DbContext, migrations, role initialization, initial-admin bootstrap, user provisioning, and API-session ticket issuance. It is composed by WebApp and ApiService and is not a separately deployed service.

**Rationale**: Identity is a platform capability, not WMS behavior. A dedicated project keeps credentials and Identity persistence out of `Myrmex.Modules.Wms`, `WmsDbContext`, and host `Program.cs` files while retaining the modular-monolith deployment model.

**Alternatives considered**:

- Put Identity in `Myrmex.Modules.Wms`: rejected because users and credentials are not WMS domain concepts.
- Put all Identity code in ApiService: rejected because WebApp must own browser login/logout and both hosts need consistent Identity configuration.
- Add a dedicated identity server: rejected as unnecessary scope and explicitly excluded.

## Decision 2: WebApp/ApiService authenticated-session strategy

**Decision**: Use two cookie schemes with distinct purposes:

1. WebApp authenticates the browser with the standard Identity application cookie.
2. Each server-side typed-client request uses a WebApp delegating handler to reload the current Identity user and roles, mint a two-minute nonpersistent `Myrmex.ApiSession` authentication ticket, protect it with the shared Data Protection key ring, and attach it as an internal cookie to the ApiService request.
3. ApiService authenticates only the `Myrmex.ApiSession` scheme in production, independently applies authorization policies, and resolves actor ID from the resulting principal.

ApiService remains private to WebApp/internal infrastructure. The API-session cookie is never set in or exposed to the browser.

**Rationale**: Current Razor components run on the WebApp server, and all typed API clients issue server-to-server `HttpClient` calls. A browser cookie is not automatically present on those requests. Interactive server circuits also cannot safely depend on an ambient request `HttpContext`. A freshly minted, short-lived protected ticket explicitly bridges the trusted WebApp principal into ApiService while preserving independent API authentication and avoiding JWT before a non-browser client exists.

The handler reloads user and role state for each outgoing request rather than trusting stale circuit role claims. This makes deletion, sign-in disallowance, and role removal effective at the propagation boundary. ApiService still cryptographically authenticates the ticket and makes the final policy decision.

**Alternatives considered**:

- Shared browser cookie only: rejected because server-side typed-client calls do not carry the browser's cookie.
- Forward the raw browser cookie through `IHttpContextAccessor`: rejected because an interactive server circuit outlives the initiating HTTP request, cookie renewal is not reliably reflected, and ambient `HttpContext` is not a safe circuit dependency.
- Forward user ID and roles in headers: rejected because unsigned identity headers create an impersonation vulnerability.
- JWT bearer between WebApp and ApiService: rejected because JWT is explicitly deferred until a real mobile/external client exists.
- BFF/reverse proxy with browser-originated API calls: rejected because current components execute API calls server-side; converting all data access to browser fetches is a broad redesign.
- Co-host WebApp and ApiService in one process: rejected because it removes the explicit service boundary and conflicts with the current separate-host architecture.

**Primary platform references for implementation review**:

- [Share authentication cookies among ASP.NET apps](https://learn.microsoft.com/aspnet/core/security/cookie-sharing?view=aspnetcore-10.0)
- [ASP.NET Core Blazor additional security scenarios](https://learn.microsoft.com/aspnet/core/blazor/security/additional-scenarios?view=aspnetcore-10.0)
- [ASP.NET Core Blazor authentication and authorization](https://learn.microsoft.com/aspnet/core/blazor/security/?view=aspnetcore-10.0)
- [Configure ASP.NET Core Data Protection](https://learn.microsoft.com/aspnet/core/security/data-protection/configuration/overview?view=aspnetcore-10.0)

## Decision 3: Key management and cookie configuration

**Decision**: Persist Data Protection keys in the Identity-owned SQL context, configure the same Data Protection application name in WebApp and ApiService, and protect production key XML at rest with a deployment-provided X.509 certificate shared by the two trusted hosts. Production startup fails if required key-protection configuration is absent. The browser application cookie uses secure, HTTP-only, same-site settings. The distinct API-session cookie is never emitted to a browser; the WebApp handler attaches it directly to internal HTTPS requests with a two-minute absolute ticket lifetime and no sliding expiration. ApiService cookie events return 401/403 rather than redirects.

**Rationale**: Both trusted processes must protect/unprotect the internal ticket across restarts and multiple instances. SQL persistence is already required for Identity, avoids adding a new infrastructure dependency, and keeps key ownership in the Identity capability. Certificate protection prevents database access alone from exposing active key material. A distinct short-lived non-browser cookie prevents accidental confusion with the browser application session and sharply limits replay exposure on internal traffic.

**Alternatives considered**:

- Ephemeral keys: rejected because tickets fail across restarts and instances.
- Local filesystem keys: rejected because multi-instance deployment would require separate shared-volume management.
- Redis key storage: viable, but rejected because Identity SQL persistence is already mandatory and is the more durable ownership boundary for cryptographic keys.

## Decision 4: Browser account flow

**Decision**: Implement login and logout as normal antiforgery-protected HTTP POST flows in statically rendered account components/endpoints. Account routes opt out of interactive routing, and the root app selects no interactive render mode for those requests. Use only local return URLs. Add an access-denied page, route authorization, current-user display, and periodic Identity security-stamp revalidation for interactive server circuits.

**Rationale**: Cookie issuance and deletion require a live HTTP response and should not be attempted after an interactive circuit response has started. Static account flows align with the server-rendered architecture and built-in Identity behavior.

**Alternatives considered**:

- Set/delete cookies inside an interactive component event: rejected because response headers may already be committed.
- Client-side credential storage: rejected as unnecessary and less secure.

## Decision 5: Persistence model

**Decision**: Use GUID-keyed `MyrmexUser`, `MyrmexRole`, and `MyrmexIdentityDbContext` in the `identity` schema of the existing SQL database connection. `MyrmexUser` adds optional `DisplayName`; framework-owned normalized identity, password hash, security stamp, concurrency stamp, lockout, claims, logins, tokens, and role joins remain framework-managed. The context also persists Data Protection keys.

**Rationale**: GUID keys match existing actor identifiers. A separate DbContext/schema preserves capability ownership without requiring a new database service. Framework-owned security fields avoid custom credential logic.

**Alternatives considered**:

- String keys: supported but inconsistent with current stable actor identifiers.
- Identity tables in `WmsDbContext`: rejected because it breaks capability ownership.
- Separate physical database: viable later, but unnecessary operational complexity for the current deployment.

## Decision 6: Role and policy model

**Decision**: Define fixed role names `MyrmexAdmin` and `WmsOperator`. `WmsOperator` policy requires authenticated identity, a parseable non-empty user-id claim, and either role. `MyrmexAdmin` policy requires the admin role. Role codes are stable and not localized.

**Rationale**: This preserves the existing policy name and actor contract while adding the minimum requested access model. Future JWT principals can carry the same user-id and role claims.

**Alternatives considered**:

- Admin-only access for all WMS work: rejected because warehouse operators need distinct least-privilege accounts.
- Fine-grained warehouse permissions: rejected as explicitly out of scope.

## Decision 7: Initial role and administrator bootstrap

**Decision**: ApiService is the single startup owner. It idempotently creates the two supported roles, then optionally bootstraps the configured administrator. Enabling bootstrap requires valid email and password options at startup. Existing users are never password-reset; an explicitly configured existing user may receive the missing admin role. Concurrent duplicate creation is resolved through normalized-email uniqueness and re-read behavior.

**Rationale**: A single owner avoids routine WebApp/ApiService races. Fail-fast option validation prevents an enabled but incomplete bootstrap from silently leaving deployment unsecured. Database uniqueness is the final concurrency guard.

**Alternatives considered**:

- Hardcoded/default administrator: rejected as a critical credential vulnerability.
- Bootstrap from both hosts: rejected because it creates avoidable startup races and duplicate logging.
- Automatically migrate the database at runtime: rejected by repository workflow and operational control requirements.

## Decision 8: User creation contract and transaction

**Decision**: Add `POST /api/identity/users`, protected by `MyrmexAdmin`. It accepts email, optional display name, temporary password, and one or more roles from the fixed set. It returns non-sensitive user details through `ApiResult<T>`/ProblemDetails conventions. User creation and role assignment run in one explicit database transaction.

**Rationale**: The API is a write action and must preserve existing Myrmex error conventions. A transaction prevents a role-assignment failure from leaving a misleading partially provisioned user.

**Alternatives considered**:

- WebApp writes Identity storage directly for admin creation: rejected because server-side authorization and the application-service boundary must remain authoritative.
- Full user-management endpoints: rejected as out of scope.

## Decision 9: Test authentication

**Decision**: Remove the temporary development/staging actor authentication path. Identity/API-session cookies are the only application authentication path for protected ApiService access. Keep a separate configurable test-only authentication scheme with explicit stable GUID actor and role options for endpoint tests.

**Rationale**: The real WebApp-issued `Myrmex.ApiSession` boundary is now implemented and tested, so retaining a development application scheme adds bypass risk and duplicate behavior. Focused endpoint tests remain usable through test-only authentication without coupling tests to password login.

**Alternatives considered**:

- Keep a development/staging actor shortcut: rejected because the real Identity/API-session path now exists and should be the only application path.
- Let non-production tests use login UI flows only: rejected as too broad for focused endpoint tests.

## Decision 10: Test ownership

**Decision**: Add focused tests at these boundaries:

- policy and actor unit tests for role/user-id rules;
- Identity persistence tests for normalized uniqueness and mappings;
- bootstrap tests for disabled, invalid, first-run, existing-user, idempotent, and concurrency behavior;
- create-user handler tests for supported roles, duplicates, password validation, and transaction rollback;
- endpoint tests for admin/operator/unprivileged/anonymous outcomes and ProblemDetails;
- outbound handler tests for anonymous, missing ID, cancellation, fresh roles, and no secret logging;
- one two-host integration suite sharing a test key ring and Identity store to prove WebApp-minted cookie → ApiService authentication → policy → `IActorContext` actor ID, including tampered and expired tickets;
- manual UI smoke checks for localized login/logout/access-denied/current-user/create-user rendering.

**Rationale**: The cookie boundary is security-critical and cannot be proven by isolated unit tests. UI rendering does not justify introducing a new component-test framework, while HTTP account behavior and server authorization do justify integration coverage.

**Alternatives considered**:

- Duplicate the role matrix through every existing WMS endpoint test: rejected as low-value duplication.
- Manual-only session testing: rejected because ticket protection and principal propagation are regression-prone security boundaries.
