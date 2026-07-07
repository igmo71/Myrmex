# Quickstart Validation: Identity Cookie Authentication

This guide validates the completed feature. Commands are developer-controlled; do not run migration, startup, or infrastructure commands automatically.

## Prerequisites

- SQL Server connection configured as `ConnectionStrings:MyrmexDatabase` for both WebApp and ApiService.
- HTTPS/service discovery available through AppHost or equivalent deployment routing.
- Identity migration generated and reviewed in `Myrmex.Identity/Persistence/Migrations`.
- Initial administrator password supplied through user-secrets, environment variables, or deployment secret configuration. Do not put it in committed settings or command history.
- Production Data Protection certificate configuration supplied securely and available to both WebApp and ApiService; certificate/private-key material is not committed.
- ApiService remains private; WebApp is the browser entry point.

## 1. Review the explicit session boundary

Before running, verify implementation matches [session-boundary.md](contracts/session-boundary.md):

- distinct browser application and internal API-session cookie schemes;
- API-session lifetime is two minutes, absolute, nonpersistent;
- shared persisted Data Protection key ring/application name;
- production key ring encrypted at rest with shared deployment certificate protection;
- `AuthenticationStateProvider` rather than circuit `IHttpContextAccessor`;
- account routes excluded from interactive routing so login/logout can issue/delete cookies on normal HTTP responses;
- fresh user/role reload before ticket issuance;
- ApiService 401/403 status behavior and no production DevelopmentActor fallback.

## 2. Build and automated tests

```powershell
dotnet build Myrmex.slnx -nologo
dotnet test Myrmex.Tests\Myrmex.Tests.csproj -nologo
```

Required automated evidence:

- policy matrix passes for operator, admin, unprivileged, missing-ID, and anonymous principals;
- actor context resolves exactly the Identity GUID;
- initial-admin bootstrap tests cover disabled, invalid, create, existing, repeated, and concurrent execution;
- create-user tests cover validation, duplicate identity, authorization, and role-assignment rollback;
- two-host boundary tests prove typed WebApp call → protected cookie → ApiService authentication/policy → actor ID;
- tampered, expired, wrong-key, and wrong-scheme tickets return 401;
- removed roles are reflected on the next outgoing WebApp API request.
- explicitly enabled DevelopmentActor/test principals carry roles required by the strengthened policy, while production cannot select those schemes.

## 3. Review and apply migration

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

## 4. Configure initial administrator safely

Use a secret source. Conceptual environment variables:

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

## 5. Run through AppHost

```powershell
dotnet run --project Myrmex.AppHost\Myrmex.AppHost.csproj
```

Verify ApiService is not externally published as a browser entry point. Navigate only to the WebApp endpoint.

## 6. Manual account and authorization scenarios

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

### Access denied and localization

1. Verify login, logout, access-denied, current-user, and create-user UI in `ru-RU` and `en-US`.
2. Expected: all text is localized; role codes and API identifiers remain stable and untranslated.

## 7. Production configuration review

- Identity application cookie is WebApp default.
- `Myrmex.ApiSession` is ApiService production default and returns 401/403, not redirects.
- DevelopmentActor is unavailable unless explicitly enabled in Development/Staging.
- No default admin email/password is committed.
- Data Protection keys persist and are shared only by WebApp/ApiService.
- Production Data Protection key XML is encrypted at rest and both hosts can load the configured certificate without logging its secret material.
- ApiService has no external ingress.
- HTTPS is enforced for browser and internal service traffic.
- Logs contain no password, protected ticket, cookie, key material, or password-validation details.
