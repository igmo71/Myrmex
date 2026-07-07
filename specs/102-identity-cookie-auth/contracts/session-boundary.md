# WebApp/ApiService Authenticated-Session Boundary

## Deployment trust boundary

```text
Browser
  HTTPS + WebApp Identity application cookie
    -> WebApp (public)
       AuthenticationStateProvider principal
       + Identity user/role reload
       + shared Data Protection ticket protection
       -> internal HTTPS request
          Cookie: Myrmex.ApiSession=<2-minute protected ticket>
          -> ApiService (private)
             cookie authentication
             -> authorization policy
             -> IActorContext
             -> protected endpoint
```

- WebApp is the only public browser entry point.
- ApiService is reachable only from trusted application infrastructure and test hosts.
- Both processes use the same persisted Data Protection key ring and application name; production key XML is encrypted at rest with deployment-provided certificate protection.
- Browser application cookie and internal API-session cookie use different names and schemes.
- ApiService does not trust user ID, email, display name, or roles supplied through ordinary request headers or bodies.
- No JWT bearer token is introduced.

## Issuance contract

For each typed-client request to ApiService, `IdentityApiAuthenticationHandler` MUST:

1. Obtain the current `ClaimsPrincipal` through the circuit-safe `AuthenticationStateProvider`, not `IHttpContextAccessor`.
2. Require an authenticated identity and parseable Identity user ID.
3. Reload the user from Identity persistence.
4. Verify the user still exists and is allowed to sign in.
5. Rebuild the principal through Identity so current role and security claims are used.
6. Create a nonpersistent authentication ticket for `Myrmex.ApiSession` with issued time and an absolute expiry two minutes later.
7. Protect the ticket with the configured API-session `TicketDataFormat` backed by the shared key ring.
8. Add only the API-session cookie to the internal request and never log its value.
9. Propagate cancellation through all Identity reads and the outgoing request.

Anonymous calls to endpoints intentionally allowed by ApiService may proceed without a ticket. Protected WebApp API clients fail closed when no authenticated session can be issued; they never add DevelopmentActor or identity headers.

## Validation contract

ApiService MUST:

1. Authenticate production requests with `Myrmex.ApiSession`.
2. Unprotect the ticket through the same scheme purpose, application name, and shared key ring.
3. Reject missing, malformed, tampered, wrong-scheme, or expired tickets with 401 and no redirect.
4. Apply endpoint authorization after authentication.
5. Return 403 for a valid principal that lacks the required role.
6. Resolve `IActorContext.ActorId` only from the stable Identity user-id claim.
7. Never fall back to DevelopmentActor in production.

## Test matrix

| WebApp state / ticket | ApiService policy | Expected result |
|---|---|---|
| Anonymous; no ticket | WmsOperator | 401 |
| Authenticated but missing user ID; no ticket issued | WmsOperator | fail closed; no endpoint execution |
| Unknown/deleted user; no ticket issued | WmsOperator | fail closed; no endpoint execution |
| Valid WmsOperator ticket | WmsOperator | 200; actor ID equals Identity user ID |
| Valid MyrmexAdmin ticket | WmsOperator | 200; actor ID equals Identity user ID |
| Valid unprivileged ticket | WmsOperator | 403 |
| Valid WmsOperator ticket | MyrmexAdmin | 403 |
| Valid MyrmexAdmin ticket | MyrmexAdmin | permitted |
| Tampered ticket | Any protected policy | 401 |
| Expired ticket | Any protected policy | 401 |
| Ticket protected with different key/application name/scheme | Any protected policy | 401 |
| Role removed before next WebApp API request | WmsOperator | newly issued principal lacks role; 403 |
| DevelopmentActor enabled in Production | Any | registration/configuration refused; no fallback |

## Integration-test shape

The boundary suite starts WebApp-side service composition and an ApiService test host with:

- one shared test Identity database;
- one shared temporary Data Protection key ring/application name;
- the production API-session scheme and delegating handler;
- test users representing operator, admin, and unprivileged roles;
- a protected test endpoint returning the authenticated user ID and `IActorContext.ActorId`.

The test invokes the typed client through the WebApp handler, not by manually constructing a cookie, and asserts both IDs and HTTP results. Separate negative cases mutate/expire the protected ticket only to verify ApiService rejection.
