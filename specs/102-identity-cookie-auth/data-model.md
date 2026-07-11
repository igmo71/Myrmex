# Data Model: Identity Cookie Authentication

## Overview

Identity data is owned by `Myrmex.Identity` through `MyrmexIdentityDbContext`. It uses the existing SQL Server connection but a dedicated `identity` schema and migrations. It does not reference or modify WMS entities.

## MyrmexUser

Represents one person who can authenticate.

| Field | Type | Required | Rules |
|---|---|---:|---|
| Id | Guid | Yes | Stable, immutable account and actor identifier |
| Email | string | Yes | Initial sign-in identity; valid email form |
| NormalizedEmail | string | Yes | Unique index; framework normalization |
| UserName | string | Yes | Set from email for the initial model |
| NormalizedUserName | string | Yes | Unique index; framework normalization |
| DisplayName | string? | No | Trimmed; bounded length; user-facing only, never actor identity |
| PasswordHash | string | Yes after creation | Framework-managed; never returned or logged |
| SecurityStamp | string | Yes | Framework-managed session revalidation marker |
| ConcurrencyStamp | string | Yes | Framework-managed optimistic concurrency marker |
| EmailConfirmed | bool | Yes | Initial release does not require confirmation; value remains framework-owned |
| LockoutEnd / LockoutEnabled / AccessFailedCount | framework fields | Yes | Safe framework defaults; no custom tuning in this feature |

### Invariants

- `Id` is the only value mapped to `IActorContext.ActorId`.
- Email and username normalization enforce case-insensitive uniqueness.
- Display name, email, and username are never substituted for the actor ID.
- Password material exists only at input boundaries and framework password services.

## MyrmexRole

Represents a fixed application authorization role.

| Field | Type | Required | Rules |
|---|---|---:|---|
| Id | Guid | Yes | Stable role identifier |
| Name | string | Yes | Exactly `MyrmexAdmin` or `WmsOperator` for roles managed by this feature |
| NormalizedName | string | Yes | Unique framework-normalized role name |
| ConcurrencyStamp | string | Yes | Framework-managed |

Roles are initialized idempotently by ApiService startup. They are stable codes and are not localized.

## UserRoleAssignment

Framework-managed many-to-many association between `MyrmexUser` and `MyrmexRole`.

### Invariants

- The create-user flow assigns at least one supported role.
- `MyrmexAdmin` satisfies both admin and WMS operator policies.
- `WmsOperator` satisfies only WMS operator policy.
- User creation and all requested role assignments commit atomically.

## DataProtectionKey

Persists the shared ASP.NET Core Data Protection key ring used by trusted WebApp and ApiService processes.

| Field | Type | Required | Rules |
|---|---|---:|---|
| FriendlyName | string? | No | Framework-generated key label |
| Xml | string | Yes | Key descriptor encrypted at rest in production with deployment certificate protection; never returned by application contracts |

Keys live in the Identity schema and are accessible only to trusted application processes. Production key protection uses deployment-provided X.509 certificate material available to WebApp and ApiService. Key material and certificate secrets are excluded from logs and public APIs.

## InitialAdminOptions

Configuration model; not a persisted entity.

| Field | Type | Required | Rules |
|---|---|---:|---|
| Enabled | bool | Yes | Defaults to false |
| Email | string? | When enabled | Valid, normalized email identity |
| Password | secret string? | When enabled | Must satisfy active password policy; secret source only |
| DisplayName | string? | No | Optional initial administrator display name |

### Bootstrap state transitions

```text
Disabled
  -> log skipped; no writes

Enabled + invalid/missing options
  -> startup validation failure; no writes

Enabled + user absent
  -> ensure roles -> create user -> assign MyrmexAdmin -> commit -> log created

Enabled + user present
  -> never change password -> ensure MyrmexAdmin assignment -> log existing/updated-role
```

Normalized-email uniqueness is the final guard when multiple instances race. A duplicate result triggers a re-read and idempotent completion; it never triggers password replacement.

## AuthenticatedBrowserSession

Ephemeral framework authentication ticket stored in the browser's WebApp Identity application cookie. It is not persisted as an application entity.

Required principal claims:

- stable user ID (`ClaimTypes.NameIdentifier`);
- display name/email claims needed by the WebApp;
- current role claims;
- Identity security-stamp claim for revalidation.

## InternalApiSession

Ephemeral two-minute authentication ticket created per outgoing WebApp typed-client request and carried only over internal HTTPS to ApiService as the `Myrmex.ApiSession` cookie.

Required ticket properties:

- authentication scheme: `Myrmex.ApiSession`;
- issued UTC and absolute expiry UTC;
- nonpersistent, no sliding expiration;
- stable user-id claim;
- current supported role claims;
- no password, password hash, raw browser cookie, or arbitrary client-provided claims.

### State transitions

```text
Authenticated WebApp principal
  -> reload user/current roles
  -> verify user can sign in
  -> create and protect short-lived API ticket
  -> attach to one internal request
  -> ApiService unprotects/authenticates
  -> policy authorizes or returns 403
  -> IActorContext resolves stable user ID

Anonymous/stale/deleted/disallowed user
  -> no ticket issued
  -> protected API access fails closed

Tampered/expired ticket
  -> ApiService authentication fails with 401
```

## Public Transport Models

### CreateIdentityUserRequest

- `Email` (required)
- `DisplayName` (optional)
- `TemporaryPassword` (required, write-only input)
- `Roles` (required non-empty set containing only supported role names)

### IdentityUserDetails

- `Id`
- `Email`
- `DisplayName`
- `Roles`

No password, hash, security stamp, concurrency stamp, token, Data Protection key, or protected session ticket is returned.
