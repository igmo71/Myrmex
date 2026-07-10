# Quickstart Validation: Identity Cookie Authentication

This guide validates the completed feature. Commands are developer-controlled; do not run migration, startup, or infrastructure commands automatically.

## Prerequisites

- SQL Server connection configured as `ConnectionStrings:MyrmexDatabase` for both WebApp and ApiService.
- HTTPS/service discovery available through AppHost or equivalent deployment routing.
- Identity migration generated, reviewed, and applied by the developer when runtime validation is needed. Migration generation and database update remain manual steps; do not run them from automation.
- Initial administrator password supplied through user-secrets, environment variables, or deployment secret configuration. Do not put it in committed settings or command history.
- Production Data Protection certificate configuration supplied securely and available to both WebApp and ApiService; certificate/private-key material is not committed.
- ApiService remains private; WebApp is the browser entry point.

Implemented Identity configuration keys:

| Key | Owner | Notes |
|---|---|---|
| `ConnectionStrings:MyrmexDatabase` | WebApp and ApiService | Existing SQL Server database; Identity and Data Protection persistence use this connection. |
| `Myrmex:Identity:ApiSession:LifetimeMinutes` | WebApp and ApiService | Must be exactly `2`. |
| `Myrmex:Identity:DataProtection:ApplicationName` | WebApp and ApiService | Must match on both hosts; current committed value is `Myrmex`. |
| `Myrmex:Identity:DataProtection:AllowUnprotectedKeysInDevelopment` | WebApp and ApiService | Allowed only in `Development`; committed Development value is `true`, non-Development value is `false`. |
| `Myrmex:Identity:DataProtection:Certificate:Thumbprint` | WebApp and ApiService | Required outside Development unless the development opt-out applies; identifies a certificate with a private key. |
| `Myrmex:Identity:DataProtection:Certificate:StoreName` | WebApp and ApiService | Current committed value is `My`. |
| `Myrmex:Identity:DataProtection:Certificate:StoreLocation` | WebApp and ApiService | Current committed value is `CurrentUser`. |
| `Myrmex:Identity:InitialAdmin:Enabled` | ApiService bootstrap | Defaults to `false`. |
| `Myrmex:Identity:InitialAdmin:Email` | ApiService bootstrap | Required only when bootstrap is enabled. |
| `Myrmex:Identity:InitialAdmin:DisplayName` | ApiService bootstrap | Optional and non-secret. |
| `Myrmex:Identity:InitialAdmin:Password` | ApiService bootstrap | Required only when bootstrap is enabled; secret source only, never committed. |

## 1. Review the explicit session boundary

Before running, verify implementation matches [session-boundary.md](contracts/session-boundary.md):

- distinct browser application and internal API-session cookie schemes:
  - WebApp browser scheme: `Identity.Application`;
  - WebApp browser cookie name: `.Myrmex.Identity.Application`;
  - ApiService scheme: `Myrmex.ApiSession`;
  - internal API-session cookie name: `Myrmex.ApiSession`;
- API-session lifetime is two minutes, absolute, nonpersistent;
- shared persisted Data Protection key ring/application name;
- production key ring encrypted at rest with shared deployment certificate protection;
- `AuthenticationStateProvider` rather than circuit `IHttpContextAccessor`;
- account routes excluded from interactive routing so login/logout can issue/delete cookies on normal HTTP responses;
- fresh user/role reload before ticket issuance;
- ApiService 401/403 status behavior and no development-auth fallback.

## 2. Build and automated tests

These commands are developer-controlled validation commands. Do not run them automatically.

```powershell
dotnet build Myrmex.slnx -nologo
dotnet test Myrmex.Tests\Myrmex.Tests.csproj -nologo
```

Required automated evidence:

- policy matrix passes for operator, admin, unprivileged, missing-ID, and anonymous principals;
- actor context resolves exactly the Identity GUID;
- account flow tests cover existing-user login, invalid password rejection, external return URL rejection, and logout clearing the WebApp application session;
- WebApp route-authorization tests cover anonymous protected-route login challenges, authenticated operator access, access-denied routing for authenticated users without the required role, and anonymous account-page access;
- interactive circuit revalidation tests cover deleted users, changed security stamps, and stale circuit principals being rejected on revalidation;
- initial-admin bootstrap tests cover disabled, invalid, create, existing, repeated, and concurrent execution;
- create-user tests cover validation, duplicate identity, authorization, and role-assignment rollback;
- two-host boundary tests prove typed WebApp call → protected cookie → ApiService authentication/policy → actor ID;
- tampered, expired, wrong-key, and wrong-scheme tickets return 401;
- removed roles are reflected on the next outgoing WebApp API request;
- browser application cookies sent directly to ApiService are ignored and protected endpoints return 401.

## 3. Review Data Protection certificate setup

Outside Development, WebApp and ApiService must be able to load the same X.509 certificate with a private key from the configured store. The application code reads:

- store name: `Myrmex:Identity:DataProtection:Certificate:StoreName`;
- store location: `Myrmex:Identity:DataProtection:Certificate:StoreLocation`;
- thumbprint: `Myrmex:Identity:DataProtection:Certificate:Thumbprint`.

Manual Windows setup example for a developer-controlled certificate:

```powershell
$cert = New-SelfSignedCertificate `
  -DnsName 'myrmex.local' `
  -CertStoreLocation 'Cert:\CurrentUser\My' `
  -KeyExportPolicy Exportable `
  -KeySpec Signature

$cert.Thumbprint
```

Use a deployment-approved certificate source for production. Install the certificate, including its private key, into the configured store for both WebApp and ApiService identities. Then set the thumbprint through user-secrets, environment variables, or deployment secret configuration:

```powershell
$env:Myrmex__Identity__DataProtection__Certificate__Thumbprint = '<certificate-thumbprint>'
$env:Myrmex__Identity__DataProtection__Certificate__StoreName = 'My'
$env:Myrmex__Identity__DataProtection__Certificate__StoreLocation = 'CurrentUser'
```

Do not commit certificate files, private keys, exported PFX files, or thumbprints that reveal production deployment details. Clear temporary environment variables after validation.

## 4. Review and apply migration

Generate only if the reviewed migration is not already present:

```powershell
dotnet ef migrations add InitialIdentity `
  --project Myrmex.Identity\Myrmex.Identity.csproj `
  --startup-project Myrmex.ApiService\Myrmex.ApiService.csproj `
  --context MyrmexIdentityDbContext `
  --output-dir Persistence\Migrations
```

Review that only Identity-owned tables, indexes, role/user relationships, and Data Protection keys are added under the intended schema. Apply explicitly:

```powershell
dotnet ef database update `
  --project Myrmex.Identity\Myrmex.Identity.csproj `
  --startup-project Myrmex.ApiService\Myrmex.ApiService.csproj `
  --context MyrmexIdentityDbContext
```

## 5. Configure initial administrator safely

Committed `appsettings*.json` files document only non-secret initial-admin keys:

- `Myrmex:Identity:InitialAdmin:Enabled`
- `Myrmex:Identity:InitialAdmin:Email`
- `Myrmex:Identity:InitialAdmin:DisplayName`

Bootstrap is disabled by default. Do not commit `Myrmex:Identity:InitialAdmin:Password`
or any password value. Supply the password only through a secret source such as
.NET User Secrets, environment variables, or deployment secret configuration.

ApiService is the single startup owner for Identity role initialization and optional
initial-admin bootstrap. It does not run migrations or `EnsureCreated`; the Identity
migration must already be applied to `ConnectionStrings:MyrmexDatabase`.

Conceptual environment variables:

```powershell
$env:Myrmex__Identity__InitialAdmin__Enabled = 'true'
$env:Myrmex__Identity__InitialAdmin__Email = '<admin-email>'
$env:Myrmex__Identity__InitialAdmin__Password = '<secret-from-secure-source>'
```

Do not commit these values. Clear the password variable after validation.

Negative validation first:

1. Enable bootstrap without password and start ApiService.
2. Expected: startup refuses bootstrap clearly; no user is created; no password appears in logs.

Positive/idempotency validation:

1. Supply valid secret values and start ApiService.
2. Expected: roles and one admin are created; logs contain outcome but no password.
3. Restart at least twice.
4. Expected: no duplicate user and no password reset.

## 6. Run through AppHost

```powershell
dotnet run --project Myrmex.AppHost\Myrmex.AppHost.csproj
```

Verify ApiService is not externally published as a browser entry point. Navigate only to the WebApp endpoint.

## 7. Manual account and authorization scenarios

### Anonymous

1. Open a protected WMS URL.
2. Expected: redirect to localized login with a safe local return URL.
3. Attempt a malformed/external return URL.
4. Expected: external redirect is refused.

### Administrator

1. Sign in with the explicitly bootstrapped administrator.
2. Expected: current-user display identifies the account; WMS pages are available.
3. Open `/admin/users/create` and create a `WmsOperator` with a valid temporary password.
4. Expected: one user is created; password is never echoed; duplicate email receives a clear conflict.

### Operator

1. Sign out, then sign in as the new operator.
2. Expected: WMS operations work; admin user creation is absent/denied.
3. Perform a protected write and verify its recorded actor ID is the operator's Identity GUID.

### Session boundary failures

1. Sign out and retry a protected action.
2. Expected: no API-session ticket is issued and protected access fails.
3. Remove the operator role while the session remains open, then trigger another API call.
4. Expected: the fresh propagated principal lacks the role and ApiService returns 403; no operation executes.

Expected protected ApiService outcomes:

| Request state | Expected status | Expected operation behavior |
|---|---:|---|
| No `Myrmex.ApiSession` cookie | 401 | Endpoint handler is not executed. |
| Expired, malformed, tampered, wrong-scheme, wrong-key, or wrong-application protected ticket | 401 | Endpoint handler is not executed. |
| Browser `.Myrmex.Identity.Application` cookie sent directly to ApiService | 401 | Browser Identity cookie is ignored by ApiService. |
| Valid API-session ticket without required role | 403 | Endpoint handler is not executed. |
| Valid `WmsOperator` API-session ticket to WMS/OneC/demo-data protected operation | Success | ApiService resolves `IActorContext.ActorId` from the Identity GUID claim. |
| Valid `MyrmexAdmin` API-session ticket to WMS/OneC/demo-data protected operation | Success | Admin satisfies WMS operator policy. |
| Valid `WmsOperator` API-session ticket to `/api/identity/users` | 403 | User creation is not executed. |
| Valid `MyrmexAdmin` API-session ticket to `/api/identity/users` | 201 on valid input | Response contains only `id`, `email`, `displayName`, and `roles`. |

### Access denied and localization

1. Verify login, logout, access-denied, current-user, and create-user UI in `ru-RU` and `en-US`.
2. Expected: all text is localized; role codes and API identifiers remain stable and untranslated.

## 8. Production configuration review

- Identity application cookie is WebApp default.
- `Myrmex.ApiSession` is ApiService production default and returns 401/403, not redirects.
- No development-auth application scheme is registered.
- No default admin email/password is committed.
- Data Protection keys persist and are shared only by WebApp/ApiService.
- Production Data Protection key XML is encrypted at rest and both hosts can load the configured certificate without logging its secret material.
- ApiService has no external ingress.
- HTTPS is enforced for browser and internal service traffic.
- Logs and public error responses contain no submitted passwords, password hashes, protected tickets, cookie values, Data Protection key material, or certificate private-key material.
- Login failures remain generic. Duplicate identity feedback is limited to the admin-only create-user path and does not expose password or protected-session material.
