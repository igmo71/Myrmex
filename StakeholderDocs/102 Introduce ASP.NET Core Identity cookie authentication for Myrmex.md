# 102 Introduce ASP.NET Core Identity cookie authentication for Myrmex

## Context

Myrmex currently has an ASP.NET Core authentication and authorization pipeline with a temporary development/staging actor authentication scheme, authorization policies, and `IActorContext`.

This is enough for protected endpoint development, but it is not a production authentication model. Myrmex now needs a real first production authentication path for the browser-based WebApp and same-site API usage.

The current product direction is:

* use ASP.NET Core Identity and cookie authentication first;
* keep authorization policy-based access control;
* introduce a minimal role model immediately;
* postpone JWT Bearer until there is a real MAUI/TSD/mobile/external API client need;
* keep the solution simple and avoid introducing a dedicated identity server or external provider in this issue.

Identity is a platform capability, not a WMS domain concern. It should not be implemented inside the WMS module.

## Goal

Introduce production-ready ASP.NET Core Identity cookie authentication for Myrmex WebApp/API, with safe user creation, minimal role-based access, and compatibility with the existing `WmsOperator` authorization policy and `IActorContext`.

The result should allow an operator/admin to sign in through the WebApp, access protected WMS/API functionality according to roles, and allow an administrator to create additional users without relying on hidden production default credentials.

## Business Value

This issue removes the temporary authentication gap and allows Myrmex to move from development-only actor impersonation toward a secure operator-facing system.

The first production authentication model should be simple enough for the current Blazor Server/WebApp architecture, but structured so that JWT Bearer can be added later for mobile TSD clients or external integrations without replacing the authorization model.

## Architectural Decision

Create a dedicated project:

```text
Myrmex.Identity
```

This project owns the Identity capability.

Expected responsibilities include:

```text
Myrmex.Identity
  /Application
  /Infrastructure
  /Persistence
  /Endpoints or Account features
```

The exact folder structure may be refined during specification/planning, but the boundary is important:

* Identity persistence and Identity-specific services belong to `Myrmex.Identity`.
* WMS module must not own Identity tables, users, roles, or login behavior.
* `Myrmex.AspNetCore` may continue to contain shared ASP.NET Core/security helpers, but should not become the Identity persistence project.
* `Myrmex.ApiService` should compose/register Identity, not contain most Identity implementation details directly.

## Authentication Direction

Use ASP.NET Core Identity with cookie authentication as the first production authentication path.

Production default:

```text
ASP.NET Core Identity
+ application cookie
+ authenticated principal
+ authorization policies
+ IActorContext
```

JWT Bearer is explicitly out of scope for this issue and should be added later only when required by:

* MAUI/TSD mobile client;
* external API consumers;
* machine-to-machine integrations;
* non-browser clients.

The authorization model must remain policy/claims/roles based so that future JWT Bearer can reuse the same policies.

## Important Technical Constraint: WebApp/API Boundary

`Myrmex.WebApp` and `Myrmex.ApiService` are separate web projects. The implementation must explicitly define how the authenticated user principal is made available to protected API endpoints.

Do not assume that signing into the WebApp automatically authenticates calls to the ApiService.

The implementation must provide a deliberate, secure same-site/same-application strategy, such as a shared cookie configuration, reverse proxy/BFF-style routing, authenticated server-side API calls with intentional principal propagation, or another ASP.NET Core-supported approach chosen during planning.

Whatever approach is selected must satisfy:

* protected ApiService endpoints still use ASP.NET Core authorization;
* `IActorContext.ActorId` resolves from the authenticated Identity user;
* no anonymous bypass is introduced for existing protected WMS, OneC, or demo-data endpoints;
* future JWT Bearer support remains possible without redesigning roles/policies.

## Roles

Introduce a minimal role model immediately.

Required roles:

```text
MyrmexAdmin
WmsOperator
```

Role meaning:

### MyrmexAdmin

A platform/application administrator.

Initial responsibilities:

* can access admin-only user creation UI/API;
* can create users;
* can assign initial supported roles;
* implicitly satisfies WMS operator authorization where appropriate.

### WmsOperator

A warehouse operator role.

Initial responsibilities:

* can access protected WMS operations currently guarded by `WmsOperator` policy;
* can access WMS API endpoints required by the current WebApp.

## Authorization Policy Requirements

The existing `WmsOperator` policy should remain the main policy for WMS operational endpoints.

After this issue, the policy should pass when:

```text
user is authenticated
AND
user has a stable actor/user id claim
AND
(
    user is in WmsOperator role
    OR user is in MyrmexAdmin role
)
```

`IActorContext.ActorId` must resolve to a stable user identifier, preferably the Identity user id, not email or display name.

Do not introduce fine-grained warehouse permissions in this issue.

## DevelopmentActor

The existing DevelopmentActor authentication scheme may remain temporarily.

Rules:

* DevelopmentActor must never be the production default authentication path.
* DevelopmentActor may remain available only for Development/Staging and only when explicitly enabled by configuration.
* Production must use Identity cookie authentication.
* Removing DevelopmentActor entirely may be handled by a later cleanup issue.

## Login and Account UI Scope

Implement minimal WebApp account UI.

Required:

* login page;
* logout action/page;
* access denied page;
* current signed-in user display/status in the layout or another appropriate place;
* redirect unauthenticated users to login when they access protected WebApp areas.

Keep the UI simple and consistent with existing Myrmex WebApp/MudBlazor conventions.

## User Creation Scope

Implement minimal admin-only user creation.

Required:

* admin-only page or UI flow to create a user;
* fields:

  * email or username;
  * optional display name;
  * temporary password;
  * role assignment from supported roles;
* support assigning at least:

  * `MyrmexAdmin`;
  * `WmsOperator`;
* prevent non-admin users from accessing user creation.

This is intentionally not a full user-management system.

## Initial Admin Creation

Myrmex must not ship hidden production default credentials.

Initial admin creation must be explicit, configuration-driven, and safe.

Required behavior:

* no committed default admin password;
* no `admin/admin`;
* no hardcoded production credentials;
* initial admin seeding only runs when explicitly enabled;
* required configuration must include admin identity and password;
* password must come from a secret source such as user-secrets, environment variables, or deployment secret configuration, not committed `appsettings.json`;
* if initial admin seeding is enabled but required values are missing, the app must fail closed or clearly refuse seeding;
* seeding must be idempotent;
* if the admin user already exists, do not silently overwrite the password;
* log that initial admin seeding occurred or was skipped, but never log the password.

Example configuration shape may be refined during planning, but conceptually:

```text
Myrmex:Identity:InitialAdmin:Enabled
Myrmex:Identity:InitialAdmin:Email
Myrmex:Identity:InitialAdmin:Password
```

The exact names are not mandatory, but the safety requirements are.

## Persistence

Use ASP.NET Core Identity with EF Core SQL persistence.

Expected model direction:

```text
MyrmexUser : IdentityUser<Guid>
MyrmexRole : IdentityRole<Guid>
MyrmexIdentityDbContext
```

The exact key type may be confirmed during planning, but `Guid` is preferred for consistency with the rest of the domain model.

Identity tables should be owned by the Identity capability.

This issue may introduce Identity schema migrations.

Do not place Identity tables inside the WMS DbContext.

## In Scope

* Create `Myrmex.Identity` project.
* Add ASP.NET Core Identity user/role model.
* Add EF Core persistence for Identity.
* Add cookie authentication as the production default.
* Preserve existing authorization policy approach.
* Update `WmsOperator` policy to work with Identity roles.
* Map authenticated Identity user to `IActorContext.ActorId`.
* Add minimal login/logout/access-denied UI.
* Add minimal current-user display/status.
* Add safe explicit initial admin seeding.
* Add minimal admin-only user creation page/API.
* Add tests for authentication, authorization, role policy behavior, and safe initial admin seeding where practical.
* Keep existing test-auth support for endpoint tests.

## Out of Scope

* JWT Bearer authentication.
* MAUI/TSD/mobile authentication.
* external OAuth/OIDC providers.
* IdentityServer/OpenIddict or dedicated auth server.
* self-registration.
* email confirmation.
* password reset email flow.
* two-factor authentication.
* lockout policy tuning beyond safe defaults.
* full user management UI.
* edit/delete/deactivate user flows.
* role management UI beyond assigning supported roles during creation.
* warehouse-level permissions.
* tenant/organization-level permissions.
* audit dashboard.
* replacing all existing authorization policies.
* removing DevelopmentActor entirely.

## Functional Requirements

### FR-001: Sign in

A user can sign in using Identity credentials.

Successful sign-in creates an authenticated application cookie.

### FR-002: Sign out

A signed-in user can sign out.

After sign-out, protected pages and API calls must no longer be available.

### FR-003: Access denied

An authenticated user without the required role sees an access denied result/page instead of a generic failure.

### FR-004: Current user display

The WebApp displays enough current-user information to confirm who is signed in.

### FR-005: Role-based WMS access

A user with `WmsOperator` can access existing WMS operational functionality protected by the `WmsOperator` policy.

### FR-006: Admin satisfies WMS operator policy

A user with `MyrmexAdmin` also satisfies the existing `WmsOperator` policy.

### FR-007: Non-operator blocked

A signed-in user without `WmsOperator` or `MyrmexAdmin` must not access WMS endpoints protected by `WmsOperator`.

### FR-008: Stable actor context

`IActorContext.ActorId` resolves from the authenticated Identity user.

Commands that currently rely on actor id continue to receive a stable actor id.

### FR-009: Safe initial admin creation

The system can create the first admin only when explicitly configured.

No hidden production credentials are allowed.

### FR-010: Admin creates user

A `MyrmexAdmin` can create a new user and assign one or more supported roles.

### FR-011: Non-admin cannot create users

A non-admin user cannot access the user creation UI/API.

### FR-012: DevelopmentActor remains non-production shortcut

DevelopmentActor may remain enabled only in explicit Development/Staging configuration and must not be the production default.

## UX Requirements

The UI should be simple and functional.

Minimum pages/flows:

```text
/account/login
/account/logout or logout action
/account/access-denied
/admin/users/create
```

Exact routes may be adjusted to project conventions.

Use existing WebApp styling, layout, localization, and MudBlazor conventions.

Do not introduce a large admin area in this issue.

## Security Requirements

* No hardcoded production credentials.
* No committed default password.
* Passwords never logged.
* Initial admin seeding must be explicit.
* Authentication cookies should use secure ASP.NET Core defaults.
* Production must not depend on DevelopmentActor.
* Authorization must remain enforced server-side.
* API endpoints must not trust client-side UI visibility as authorization.
* User id used as actor id must be stable and non-empty.
* Role checks must be server-side.

## Testing Expectations

Add focused tests where practical for:

* `WmsOperator` policy with `WmsOperator` role;
* `WmsOperator` policy with `MyrmexAdmin` role;
* policy rejection for authenticated user without required roles;
* policy rejection for anonymous user;
* `IActorContext.ActorId` from Identity principal;
* initial admin seeding disabled;
* initial admin seeding enabled with missing required values;
* initial admin seeding idempotency;
* non-admin blocked from user creation;
* admin can create user with supported role.

Existing endpoint tests should remain supported through test authentication helpers.

## Acceptance Criteria

* `Myrmex.Identity` project exists and is referenced by the application composition layer.
* ASP.NET Core Identity is configured with persistent EF Core storage.
* Cookie authentication is the production default.
* Login/logout/access denied flows exist.
* `MyrmexAdmin` and `WmsOperator` roles exist.
* Existing `WmsOperator` policy accepts `WmsOperator` and `MyrmexAdmin`.
* Existing protected WMS/OneC/demo-data endpoint authorization is preserved.
* `IActorContext` resolves from the authenticated Identity user.
* Initial admin creation is explicit and safe.
* Minimal admin-only user creation exists.
* No hidden production default credentials exist.
* JWT Bearer is not introduced in this issue.
* DevelopmentActor is not used as the production default.
* Build and relevant tests pass.

## Notes for Specification

This issue should define the exact WebApp/API cookie authentication strategy during planning.

The specification must not assume that the WebApp and ApiService automatically share authenticated state just because both use cookies. The selected approach must be explicit, secure, and testable.

The goal is a pragmatic first production authentication model, not a full enterprise IAM system.
