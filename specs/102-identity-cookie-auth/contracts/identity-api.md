# Identity API and WebApp Contract

## POST `/api/identity/users`

Creates one user and assigns all requested supported roles atomically.

**Authorization**: `MyrmexAdmin` policy. Authentication is performed by ApiService through the internal `Myrmex.ApiSession` cookie before endpoint execution.

### Request

```json
{
  "email": "operator@example.com",
  "displayName": "Warehouse Operator",
  "temporaryPassword": "<secret>",
  "roles": ["WmsOperator"]
}
```

Rules:

- `email`: required, trimmed, valid email, unique after normalization.
- `displayName`: optional, trimmed, bounded according to the Identity model.
- `temporaryPassword`: required and validated by configured password rules; never echoed or logged.
- `roles`: required, non-empty, unique values; each value is exactly `MyrmexAdmin` or `WmsOperator`.

### Success — `201 Created`

```json
{
  "id": "c63f7655-9894-4664-b33f-877a18fd8748",
  "email": "operator@example.com",
  "displayName": "Warehouse Operator",
  "roles": ["WmsOperator"]
}
```

The response includes a location for the created identity resource if a read endpoint is introduced during implementation; this feature does not require a user-list or edit endpoint.

### Failure contract

| Status | Condition | Required behavior |
|---:|---|---|
| 400 | Invalid email, password, display name, empty roles, or unsupported role | ProblemDetails with stable validation code and non-secret field errors |
| 401 | Missing, expired, malformed, or tampered API-session cookie | No login redirect; no user creation |
| 403 | Authenticated user lacks `MyrmexAdmin` | No user creation |
| 409 | Normalized email/username already exists | ProblemDetails conflict; existing user unchanged |
| 500 | Unexpected persistence/role assignment failure | Transaction rolls back; generic ProblemDetails; diagnostics exclude password |

WebApp client behavior follows the existing write convention and returns `ApiResult<IdentityUserDetails>`.

## WebApp account routes

| Route | Rendering/interaction | Behavior |
|---|---|---|
| `/account/login` | Non-interactive HTTP GET + antiforgery-protected POST | Opts out of interactive routing, validates credentials, issues WebApp Identity application cookie, accepts only local return URL |
| `/account/logout` | Non-interactive antiforgery-protected POST | Opts out of interactive routing, deletes WebApp application cookie, and redirects to login/home |
| `/account/access-denied` | GET | Shows localized access-denied explanation without exposing policy internals |
| `/admin/users/create` | Authenticated WebApp page | Requires admin role in UI routing and calls protected ApiService endpoint; API remains final authority |

## Role constants

`MyrmexAdmin` and `WmsOperator` are stable transport/authorization codes shared by WebApp, ApiService, tests, and Identity. Display labels are localized separately in WebApp resources.
