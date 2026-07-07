# Tasks: Identity Cookie Authentication

**Input**: Design documents from `specs/102-identity-cookie-auth/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/identity-api.md`, `contracts/session-boundary.md`, `quickstart.md`

**Tests**: Security, persistence, endpoint, client-handler, bootstrap, and two-host boundary tests are required because the specification and plan identify concrete authentication and authorization regression risks. Write each listed test before its protected implementation and confirm it fails for the intended reason.

**Organization**: Tasks are grouped into security-first shared foundations and then by independently testable user story. The authenticated-session boundary in `contracts/session-boundary.md` is a blocking foundation, not a late integration task.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it targets different files and has no dependency on an incomplete task
- **[Story]**: Maps implementation to the user stories in `spec.md`
- Every task names its concrete repository path

---

## Phase 1: Setup (Project and Contract Skeleton)

**Purpose**: Establish the dedicated Identity capability and compile-time boundaries without implementing authentication behavior.

- [ ] T001 Create `Myrmex.Identity/Myrmex.Identity.csproj` with the Application, Infrastructure, Persistence, and Persistence/Migrations directories, and add the project to `Myrmex.slnx`
- [ ] T002 Add required project/package references for Identity, EF Core SQL Server, Identity EF stores, Data Protection EF persistence, dispatching, and host composition in `Myrmex.Identity/Myrmex.Identity.csproj`, `Myrmex.WebApp/Myrmex.WebApp.csproj`, `Myrmex.ApiService/Myrmex.ApiService.csproj`, and `Myrmex.Tests/Myrmex.Tests.csproj`
- [ ] T003 [P] Add stable `MyrmexAdmin` and `WmsOperator` role constants in `Myrmex.Shared/Identity/IdentityRoleNames.cs`
- [ ] T004 [P] Add `CreateIdentityUserRequest` and non-sensitive `IdentityUserDetails` transport records in `Myrmex.Shared/Identity/CreateIdentityUserRequest.cs` and `Myrmex.Shared/Identity/IdentityUserDetails.cs`
- [ ] T005 [P] Add non-secret Identity, API-session, Data Protection certificate, and initial-admin configuration sections with bootstrap disabled in `Myrmex.ApiService/appsettings.json`, `Myrmex.ApiService/appsettings.Development.json`, `Myrmex.WebApp/appsettings.json`, and `Myrmex.WebApp/appsettings.Development.json`
- [ ] T006 Confirm ApiService has no external AppHost endpoint and wire only the required private service/database references in `Myrmex.AppHost/AppHost.cs`

---

## Phase 2: Foundational Security Boundary (Blocking Prerequisites)

**Purpose**: Implement and prove the explicit WebApp/ApiService authenticated-session boundary before account or administration stories build on it.

**⚠️ CRITICAL**: No user-story phase starts until the distinct cookie schemes, shared protected key ring, ticket issuance, ApiService validation, policies, actor mapping, and two-host tests in this phase are complete.

### Identity persistence foundation

- [ ] T007 [P] Define GUID-keyed `MyrmexUser` with bounded optional display name in `Myrmex.Identity/Persistence/MyrmexUser.cs`
- [ ] T008 [P] Define GUID-keyed `MyrmexRole` in `Myrmex.Identity/Persistence/MyrmexRole.cs`
- [ ] T009 Implement `MyrmexIdentityDbContext` with Identity user/role stores and `IDataProtectionKeyContext` in `Myrmex.Identity/Persistence/MyrmexIdentityDbContext.cs`
- [ ] T010 Add Identity table naming, `identity` schema, display-name bounds, normalized identity uniqueness, role relationships, and Data Protection key mappings in `Myrmex.Identity/Persistence/Configurations/IdentityModelConfiguration.cs`
- [ ] T011 Write persistence tests protecting GUID actor identity, normalized email/username uniqueness, separate Identity schema ownership, and Data Protection key mapping in `Myrmex.Tests/Identity/IdentityPersistenceTests.cs`
- [ ] T012 Implement Identity persistence/service registration against `ConnectionStrings:MyrmexDatabase` without runtime auto-migration in `Myrmex.Identity/Infrastructure/IdentityServiceCollectionExtensions.cs`

### Data Protection and scheme configuration

- [ ] T013 [P] Define shared Data Protection application-name, certificate, API-session lifetime, and production validation options in `Myrmex.Identity/Infrastructure/Configuration/IdentityDataProtectionOptions.cs` and `Myrmex.Identity/Infrastructure/Configuration/IdentityDataProtectionOptionsValidator.cs`
- [ ] T014 [P] Write option-validation tests proving production rejects absent certificate protection while Development can use explicit development configuration in `Myrmex.Tests/Identity/IdentityDataProtectionOptionsTests.cs`
- [ ] T015 Persist the shared key ring through `MyrmexIdentityDbContext`, protect production key XML with the configured X.509 certificate, and fail closed on invalid production configuration in `Myrmex.Identity/Infrastructure/IdentityDataProtectionExtensions.cs`
- [ ] T016 Add distinct browser and API-session scheme/cookie constants without JWT or identity-header schemes in `Myrmex.AspNetCore/Security/MyrmexAuthenticationSchemes.cs`
- [ ] T017 [P] Write host authentication configuration tests proving WebApp defaults to the Identity application cookie, ApiService defaults to `Myrmex.ApiSession`, API challenges return 401 rather than redirects, forbidden requests return 403, and Production has no DevelopmentActor fallback in `Myrmex.Tests/Identity/IdentityHostAuthenticationTests.cs`
- [ ] T018 Configure the WebApp Identity application cookie independently from the API-session scheme in `Myrmex.Identity/Infrastructure/IdentityWebAppAuthenticationExtensions.cs` and `Myrmex.WebApp/Program.cs`
- [ ] T019 Configure ApiService `Myrmex.ApiSession` cookie authentication with a two-minute absolute ticket lifetime, no sliding expiration, and status-code events in `Myrmex.Identity/Infrastructure/IdentityApiAuthenticationExtensions.cs` and `Myrmex.ApiService/Program.cs`

### Policies and stable actor identity

- [ ] T020 [P] Write authorization-policy tests for anonymous, missing/invalid user ID, unprivileged, `WmsOperator`, and `MyrmexAdmin` principals in `Myrmex.Tests/AspNetCore/Security/MyrmexAuthorizationPolicyTests.cs`
- [ ] T021 Strengthen `WmsOperator`, add `MyrmexAdmin`, and require a parseable non-empty Identity user ID plus the correct role set in `Myrmex.AspNetCore/Security/MyrmexAuthorizationPolicies.cs`
- [ ] T022 [P] Extend actor-context tests to prove the stable Identity user ID is returned and email/display-name-only principals fail in `Myrmex.Tests/AspNetCore/Security/HttpContextActorContextTests.cs`
- [ ] T023 Preserve stable user-ID-only actor resolution and reject missing/mutable identity claims in `Myrmex.AspNetCore/Security/HttpContextActorContext.cs`

### Ticket issuance and typed-client propagation

- [ ] T024 [P] Write ticket-issuer tests for fresh persistent roles, deleted/disallowed users, two-minute expiry, correct scheme, cancellation, and exclusion of passwords/raw browser-cookie data in `Myrmex.Tests/Identity/IdentityApiSessionTicketIssuerTests.cs`
- [ ] T025 Implement fresh user/role reload and protected nonpersistent `Myrmex.ApiSession` ticket creation in `Myrmex.Identity/Infrastructure/Sessions/IIdentityApiSessionTicketIssuer.cs` and `Myrmex.Identity/Infrastructure/Sessions/IdentityApiSessionTicketIssuer.cs`
- [ ] T026 [P] Write delegating-handler tests proving `AuthenticationStateProvider` use, cancellation propagation, one internal API-session cookie, no raw browser-cookie forwarding, no identity/role headers, no bearer token, and fail-closed anonymous/missing-ID behavior in `Myrmex.Tests/Identity/IdentityApiAuthenticationHandlerTests.cs`
- [ ] T027 Implement the circuit-safe outbound handler without `IHttpContextAccessor` in `Myrmex.WebApp/Identity/IdentityApiAuthenticationHandler.cs`
- [ ] T028 Attach the handler to protected WMS topology, catalog, inventory, OneC, and Identity typed clients while leaving intentionally public clients uncredentialed in `Myrmex.WebApp/Program.cs`

### Two-host proof of the boundary

- [ ] T029 Create a two-host test fixture with shared test Identity storage, shared temporary Data Protection application/key ring, production API-session scheme, WebApp handler composition, and a protected actor-echo endpoint in `Myrmex.Tests/Identity/IdentitySessionBoundaryFixture.cs`
- [ ] T030 Write two-host success/policy tests for `WmsOperator`, `MyrmexAdmin`, unprivileged users, current-role reload, and exact `IActorContext.ActorId` propagation in `Myrmex.Tests/Identity/IdentitySessionBoundaryTests.cs`
- [ ] T031 Write two-host rejection tests for anonymous, missing-ID, unknown user, expired, tampered, wrong-key, wrong-application-name, wrong-scheme, and production DevelopmentActor fallback attempts in `Myrmex.Tests/Identity/IdentitySessionBoundaryTests.cs`
- [ ] T032 Add an architecture assertion that production code contains no raw browser-cookie forwarding, trusted identity headers, JWT bearer registration, or anonymous ApiService bypass in `Myrmex.Tests/Identity/IdentitySessionArchitectureTests.cs`

**Checkpoint**: The separate WebApp application cookie and ApiService API-session cookie boundary is implemented and proven end-to-end before UI stories begin.

---

## Phase 3: User Story 1 — Sign In to Protected Operations (Priority: P1) 🎯 MVP

**Goal**: A registered operator signs in through WebApp, sees the current identity, reaches protected WMS operations through the proven ApiService boundary, and signs out.

**Independent Test**: Provision an operator in test storage, sign in through the non-interactive account flow, access one protected WMS operation and verify its actor ID, confirm current-user display, sign out, and verify protected access again requires sign-in.

### Tests for User Story 1

- [ ] T033 [P] [US1] Write HTTP account-flow tests for valid/invalid login, application-cookie issuance, antiforgery, safe local return URL, external return rejection, logout, and post-logout denial in `Myrmex.Tests/Identity/WebAppAccountFlowTests.cs`
- [ ] T034 [P] [US1] Write route-authorization tests for anonymous login redirect, authenticated protected-page access, and access-denied routing in `Myrmex.Tests/Identity/WebAppRouteAuthorizationTests.cs`
- [ ] T035 [P] [US1] Write authentication-state revalidation tests for deleted users and changed security stamps in `Myrmex.Tests/Identity/IdentityRevalidatingAuthenticationStateProviderTests.cs`

### Implementation for User Story 1

- [ ] T036 [US1] Implement periodic Identity security-stamp revalidation for interactive circuits in `Myrmex.WebApp/Identity/IdentityRevalidatingAuthenticationStateProvider.cs`
- [ ] T037 [US1] Configure cascading authentication state, authentication middleware, authorization middleware, and account endpoint composition in `Myrmex.WebApp/Program.cs`
- [ ] T038 [US1] Make root render mode respect account-route interactive exclusion so cookie-changing pages receive normal HTTP responses in `Myrmex.WebApp/Components/App.razor`
- [ ] T039 [US1] Implement localized non-interactive login GET/POST flow with antiforgery, generic credential errors, safe local return URLs, and no self-registration in `Myrmex.WebApp/Components/Account/Login.razor`
- [ ] T040 [US1] Implement localized non-interactive antiforgery-protected logout flow in `Myrmex.WebApp/Components/Account/Logout.razor`
- [ ] T041 [P] [US1] Implement localized access-denied page in `Myrmex.WebApp/Components/Account/AccessDenied.razor`
- [ ] T042 [US1] Replace plain route rendering with authorized route handling and login/access-denied navigation in `Myrmex.WebApp/Components/Routes.razor` and `Myrmex.WebApp/Components/Account/RedirectToLogin.razor`
- [ ] T043 [US1] Add signed-in user identity, admin-aware navigation visibility, and POST logout control in `Myrmex.WebApp/Components/Layout/MainLayout.razor` and `Myrmex.WebApp/Components/Layout/NavMenu.razor`
- [ ] T044 [P] [US1] Add neutral, `en-US`, and `ru-RU` login/logout/access-denied/current-user resource keys in `Myrmex.WebApp/Resources/Localization/SharedResource.resx`, `Myrmex.WebApp/Resources/Localization/SharedResource.en-US.resx`, and `Myrmex.WebApp/Resources/Localization/SharedResource.ru-RU.resx`
- [ ] T045 [US1] Developer-controlled manual checkpoint: request validation of the complete operator login → protected typed-client call → ApiService actor ID → logout flow from `specs/102-identity-cookie-auth/quickstart.md`; record corrections there without starting AppHost automatically

**Checkpoint**: User Story 1 works independently as the first production browser authentication path.

---

## Phase 4: User Story 2 — Enforce Role-Based Access (Priority: P1)

**Goal**: ApiService consistently permits operators/admins and denies anonymous or ineligible users across representative protected WMS, OneC, demo-data, and admin operations.

**Independent Test**: Exercise representative protected endpoints with anonymous, unprivileged, operator, and admin principals; verify 401 versus 403, permitted roles, no operation on denial, and admin-only isolation.

### Tests for User Story 2

- [ ] T046 [P] [US2] Add representative WMS authorization integration cases for anonymous, unprivileged, operator, and admin API-session principals in `Myrmex.Tests/Wms/Authorization/WmsAuthorizationEndpointTests.cs`
- [ ] T047 [P] [US2] Add representative OneC and demo-data authorization cases, including no operation on denial, in `Myrmex.Tests/Integrations/Authorization/IntegrationAuthorizationEndpointTests.cs` and `Myrmex.Tests/Wms/DemoData/WmsDemoDataAuthorizationTests.cs`
- [ ] T048 [P] [US2] Add WebApp API error tests preserving authentication-required versus forbidden outcomes without converting them to generic success or retry in `Myrmex.Tests/Identity/ProtectedApiAuthorizationErrorTests.cs`

### Implementation for User Story 2

- [ ] T049 [US2] Audit and preserve `WmsOperator` policy requirements on all existing WMS, OneC, and demo-data endpoint groups in `Myrmex.Modules.Wms/WmsModule.cs`, `Myrmex.Integrations/OneC/Endpoints/OneCEndpoints.cs`, and `Myrmex.Modules.Wms/DemoData/Endpoints/DemoDataAdminEndpoints.cs`
- [ ] T050 [US2] Map protected-client 401 and 403 responses to the existing WebApp authentication/access-denied behavior without DevelopmentActor retry in `Myrmex.WebApp/Wms/Api/WmsApiClientHttp.cs` and `Myrmex.WebApp/Identity/ProtectedApiAuthorizationHandler.cs`
- [ ] T051 [US2] Developer-controlled manual checkpoint: request role-matrix and access-denied validation from `specs/102-identity-cookie-auth/quickstart.md`; do not start AppHost automatically

**Checkpoint**: User Stories 1 and 2 provide production sign-in plus independently enforced role authorization.

---

## Phase 5: User Story 3 — Bootstrap the First Administrator Safely (Priority: P2)

**Goal**: A deployer can explicitly and idempotently create the first administrator from secret configuration without hidden credentials or password overwrite.

**Independent Test**: Run the bootstrap service against disabled, incomplete, first-run, existing-user, repeated, and concurrent scenarios and verify exactly one admin, unchanged existing password, and secret-free diagnostics.

### Tests for User Story 3

- [ ] T052 [P] [US3] Write initial-admin option tests for disabled defaults, enabled missing email/password, invalid email/password, and production secret requirements in `Myrmex.Tests/Identity/InitialAdminOptionsTests.cs`
- [ ] T053 [P] [US3] Write role-initialization and bootstrap tests for first run, existing user, missing admin role, repeated runs, concurrent runs, no password overwrite, rollback, and password-free logs in `Myrmex.Tests/Identity/InitialAdminSeederTests.cs`

### Implementation for User Story 3

- [ ] T054 [P] [US3] Implement conditional initial-admin configuration and fail-fast validation in `Myrmex.Identity/Application/Bootstrap/InitialAdminOptions.cs` and `Myrmex.Identity/Application/Bootstrap/InitialAdminOptionsValidator.cs`
- [ ] T055 [US3] Implement idempotent creation of `MyrmexAdmin` and `WmsOperator` roles in `Myrmex.Identity/Application/Bootstrap/IdentityRoleInitializer.cs`
- [ ] T056 [US3] Implement transactional, concurrency-safe initial-admin creation/role assignment without password replacement in `Myrmex.Identity/Application/Bootstrap/InitialAdminSeeder.cs`
- [ ] T057 [US3] Make ApiService the single role/bootstrap startup owner after schema availability without automatic migration in `Myrmex.Identity/Infrastructure/IdentityApplicationExtensions.cs` and `Myrmex.ApiService/Program.cs`
- [ ] T058 [US3] Add bootstrap outcome logging that excludes passwords, hashes, protected tickets, and certificate secrets in `Myrmex.Identity/Application/Bootstrap/InitialAdminSeeder.cs`
- [ ] T059 [US3] Document only non-secret bootstrap keys and secret-source requirements in `Myrmex.ApiService/appsettings.json` and `specs/102-identity-cookie-auth/quickstart.md`

**Checkpoint**: The first administrator can be created safely and repeatedly without a production default credential.

---

## Phase 6: User Story 4 — Administrator Creates Users (Priority: P2)

**Goal**: An administrator creates an account with a temporary password and supported roles through an admin-only WebApp/API vertical slice.

**Independent Test**: Authenticate as admin, create an operator, verify its roles and sign-in, then verify duplicate, invalid-password, unsupported-role, transactional-failure, operator, and anonymous cases.

### Tests for User Story 4

- [ ] T060 [P] [US4] Write create-user handler/persistence tests for normalization, duplicate identity, password validation, supported roles, atomic multi-role assignment, cancellation, and rollback in `Myrmex.Tests/Identity/CreateUserTests.cs`
- [ ] T061 [P] [US4] Write endpoint contract tests for 201/400/401/403/409 responses, ProblemDetails, serialization, and no sensitive output in `Myrmex.Tests/Identity/IdentityUserEndpointTests.cs`
- [ ] T062 [P] [US4] Write WebApp client tests for request body, cancellation, and `ApiResult<IdentityUserDetails>` error mapping in `Myrmex.Tests/Identity/IdentityApiClientTests.cs`

### Implementation for User Story 4

- [ ] T063 [US4] Implement explicit `CreateUser.Command`, validation, transaction, user creation, current-role assignment, and non-sensitive result mapping in `Myrmex.Identity/Application/Users/CreateUser.cs`
- [ ] T064 [US4] Register the Identity application handler assembly with existing dispatching in `Myrmex.ApiService/Program.cs`
- [ ] T065 [US4] Implement `POST /api/identity/users` with `MyrmexAdmin` policy and existing ProblemDetails conventions in `Myrmex.Identity/Infrastructure/Endpoints/IdentityUserEndpoints.cs`
- [ ] T066 [US4] Map the Identity endpoint group from ApiService in `Myrmex.Identity/Infrastructure/IdentityEndpointRouteBuilderExtensions.cs` and `Myrmex.ApiService/Program.cs`
- [ ] T067 [US4] Implement the typed create-user client in `Myrmex.WebApp/Identity/IdentityApiClient.cs` and register it through the protected API-session handler in `Myrmex.WebApp/Program.cs`
- [ ] T068 [US4] Implement localized admin-only create-user form, supported-role selection, validation, saving state, and non-sensitive errors in `Myrmex.WebApp/Components/Pages/Admin/Users/Create.razor`
- [ ] T069 [US4] Protect `/admin/users/create` with the admin role and expose navigation only to admins in `Myrmex.WebApp/Components/Pages/Admin/Users/Create.razor` and `Myrmex.WebApp/Components/Layout/NavMenu.razor`
- [ ] T070 [P] [US4] Add neutral, `en-US`, and `ru-RU` user-creation, role-label, validation, success, and error resources in `Myrmex.WebApp/Resources/Localization/SharedResource.resx`, `Myrmex.WebApp/Resources/Localization/SharedResource.en-US.resx`, and `Myrmex.WebApp/Resources/Localization/SharedResource.ru-RU.resx`
- [ ] T071 [US4] Developer-controlled manual checkpoint: request validation of admin creation, operator denial, duplicate identity, unsupported roles, and created-user sign-in from `specs/102-identity-cookie-auth/quickstart.md`; do not start AppHost automatically

**Checkpoint**: Administrators can provision named users while ApiService remains the final authorization authority.

---

## Phase 7: User Story 5 — Retain Explicit Non-Production Test Access (Priority: P3)

**Goal**: Explicit Development/Staging actor support and test authentication remain usable without becoming a production default or bypass.

**Independent Test**: Verify DevelopmentActor succeeds only when explicitly enabled in Development/Staging with stable actor and `WmsOperator` role, fails in Production, and test principals can independently select actor/roles.

### Tests for User Story 5

- [ ] T072 [P] [US5] Extend DevelopmentActor tests for explicit enablement, allowed environments, `WmsOperator` role claim, missing actor ID, and production refusal in `Myrmex.Tests/AspNetCore/Security/DevelopmentActorAuthenticationTests.cs`
- [ ] T073 [P] [US5] Extend test-authentication helper tests for configurable user ID and role sets, including unprivileged and admin principals, in `Myrmex.Tests/Testing/TestAuthenticationTests.cs`

### Implementation for User Story 5

- [ ] T074 [US5] Add the `WmsOperator` role claim while preserving stable actor claims and explicit environment/configuration gates in `Myrmex.AspNetCore/Security/DevelopmentActorAuthenticationHandler.cs`
- [ ] T075 [US5] Register DevelopmentActor only for explicitly enabled Development/Staging and update test authentication to accept explicit roles without any production fallback in `Myrmex.ApiService/Program.cs` and `Myrmex.Tests/Testing/TestAuthentication.cs`

**Checkpoint**: Non-production shortcuts remain explicit and testable while production uses only Identity/API-session authentication.

---

## Phase 8: Polish, Security Review, and Developer-Controlled Validation

**Purpose**: Final cross-cutting review without expanding into excluded identity features or automatically executing controlled operations.

- [ ] T076 [P] Audit all new logs and error responses for passwords, hashes, cookies, protected tickets, Data Protection keys, certificate secrets, and account-enumeration detail in `Myrmex.Identity/`, `Myrmex.ApiService/`, and `Myrmex.WebApp/Identity/`
- [ ] T077 [P] Verify all account/admin text keys and placeholders match across neutral, `en-US`, and `ru-RU` files in `Myrmex.WebApp/Resources/Localization/SharedResource.resx`, `Myrmex.WebApp/Resources/Localization/SharedResource.en-US.resx`, and `Myrmex.WebApp/Resources/Localization/SharedResource.ru-RU.resx`
- [ ] T078 Confirm no JWT bearer, external provider, IdentityServer/OpenIddict, self-registration, password-reset email, 2FA, full user management, warehouse/tenant permissions, raw cookie forwarding, or trusted identity headers were introduced by reviewing `Myrmex.Identity/`, `Myrmex.ApiService/Program.cs`, and `Myrmex.WebApp/Program.cs`
- [ ] T079 Update exact implemented configuration names, certificate setup, test commands, and expected 401/403/session outcomes in `specs/102-identity-cookie-auth/quickstart.md`
- [ ] T080 Developer-controlled checkpoint: request generation of `InitialIdentity` into `Myrmex.Identity/Persistence/Migrations/` using the command in `specs/102-identity-cookie-auth/quickstart.md`; do not run migration generation automatically
- [ ] T081 Developer-controlled checkpoint: review the generated files in `Myrmex.Identity/Persistence/Migrations/` for Identity-only `identity` schema objects, uniqueness indexes, role joins, Data Protection keys, and no WMS changes; do not apply the migration automatically
- [ ] T082 Developer-controlled checkpoint: report `dotnet build Myrmex.slnx -nologo` and `dotnet test Myrmex.Tests/Myrmex.Tests.csproj -nologo` from `specs/102-identity-cookie-auth/quickstart.md`; do not run them automatically
- [ ] T083 Developer-controlled checkpoint: report the database-update and `Myrmex.AppHost` startup commands and manual acceptance matrix from `specs/102-identity-cookie-auth/quickstart.md`; do not run database updates, AppHost, Docker, or infrastructure automatically

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 — Setup**: Starts immediately.
- **Phase 2 — Foundational Security Boundary**: Depends on Phase 1 and blocks every user story.
- **Phase 3 — US1 Sign In**: Depends on Phase 2; this is the suggested MVP.
- **Phase 4 — US2 Role Enforcement**: Depends on Phase 2 and can be implemented alongside US1, though combined acceptance uses the US1 login flow.
- **Phase 5 — US3 Bootstrap**: Depends on Phase 2 and can proceed independently from US1/US2.
- **Phase 6 — US4 User Creation**: Depends on Phase 2 for API security and role constants; endpoint/handler work can proceed independently, while full WebApp acceptance depends on US1 and a provisioned admin from US3 or test setup.
- **Phase 7 — US5 Non-Production Access**: Depends on Phase 2 policy/scheme work and should follow conflicting edits to `Myrmex.ApiService/Program.cs`.
- **Phase 8 — Polish/Validation**: Depends on all selected user stories.

### User Story Dependency Graph

```text
Setup
  -> Foundational session boundary
       -> US1 Sign In (MVP)
       -> US2 Role Enforcement
       -> US3 Initial Admin Bootstrap
       -> US4 Admin Creates Users
       -> US5 Non-Production Test Access

US1 + US3 + US4 -> complete production operator-provisioning journey
US1 + US2       -> complete authenticated WMS authorization journey
```

### Within Each Phase

- Write each listed automated test before its protected implementation and confirm the intended failure.
- Create models/configuration before services that consume them.
- Configure the distinct schemes and shared protected key ring before ticket issuance.
- Implement ticket issuance before attaching it to typed clients.
- Complete the two-host boundary tests before account UI work.
- Keep shared transport records separate from Identity internal commands and persistence types.
- ApiService remains the final policy/actor authority even when WebApp hides inaccessible UI.
- Never substitute DevelopmentActor, anonymous access, identity headers, raw browser-cookie forwarding, or JWT when API-session issuance fails.

### Parallel Opportunities

- T003–T005 can run in parallel after T001–T002.
- T007 and T008 can run in parallel before T009–T010.
- T013–T014, T020, T022, T024, and T026 target separate files and can be prepared in parallel after their model dependencies exist.
- US1, US2, US3, and the backend portion of US4 can begin in parallel after Phase 2.
- Localization tasks T044 and T070 can run alongside their corresponding UI implementation when resource-key contracts are agreed.
- US5 tests T072–T073 can run in parallel before the shared registration updates.
- Final audits T076–T077 can run in parallel.

---

## Parallel Examples by User Story

### User Story 1

```text
Task T033: HTTP account flow tests in Myrmex.Tests/Identity/WebAppAccountFlowTests.cs
Task T034: Route authorization tests in Myrmex.Tests/Identity/WebAppRouteAuthorizationTests.cs
Task T035: Circuit revalidation tests in Myrmex.Tests/Identity/IdentityRevalidatingAuthenticationStateProviderTests.cs
Task T044: Account localization resources in Myrmex.WebApp/Resources/Localization/SharedResource*.resx
```

### User Story 2

```text
Task T046: WMS endpoint authorization matrix in Myrmex.Tests/Wms/Authorization/WmsAuthorizationEndpointTests.cs
Task T047: OneC/demo-data authorization cases in Myrmex.Tests/Integrations/Authorization/ and Myrmex.Tests/Wms/DemoData/
Task T048: WebApp 401/403 mapping tests in Myrmex.Tests/Identity/ProtectedApiAuthorizationErrorTests.cs
```

### User Story 3

```text
Task T052: Initial-admin option tests in Myrmex.Tests/Identity/InitialAdminOptionsTests.cs
Task T053: Bootstrap/idempotency tests in Myrmex.Tests/Identity/InitialAdminSeederTests.cs
Task T054: Initial-admin option model/validator in Myrmex.Identity/Application/Bootstrap/
```

### User Story 4

```text
Task T060: Create-user handler/persistence tests in Myrmex.Tests/Identity/CreateUserTests.cs
Task T061: Endpoint contract tests in Myrmex.Tests/Identity/IdentityUserEndpointTests.cs
Task T062: WebApp client tests in Myrmex.Tests/Identity/IdentityApiClientTests.cs
Task T070: Admin UI localization resources in Myrmex.WebApp/Resources/Localization/SharedResource*.resx
```

### User Story 5

```text
Task T072: DevelopmentActor environment/role tests in Myrmex.Tests/AspNetCore/Security/DevelopmentActorAuthenticationTests.cs
Task T073: Configurable test-principal tests in Myrmex.Tests/Testing/TestAuthenticationTests.cs
```

---

## Implementation Strategy

### Security Foundation First

1. Complete Phase 1 project/contracts setup.
2. Complete all of Phase 2 in order.
3. Stop if the two-host boundary matrix does not prove distinct cookies, protected ticket validation, exact actor ID, 401/403 behavior, and forbidden-mechanism absence.
4. Do not compensate for boundary failures with raw browser cookies, headers, JWT, anonymous access, or DevelopmentActor fallback.

### MVP First

1. Complete Setup and Foundational phases.
2. Complete US1 sign-in/current-user/sign-out flow.
3. Validate one operator journey through a protected ApiService operation.
4. Stop and review before adding bootstrap/admin workflows.

### Incremental Delivery

1. Add US2 to complete the policy/denial matrix.
2. Add US3 to enable safe first-admin provisioning.
3. Add US4 to enable ongoing named-user provisioning.
4. Add US5 to restore explicit non-production convenience under the strengthened policy.
5. Complete security review and developer-controlled migration/build/test/runtime checkpoints.

---

## Notes

- `[P]` means different files and no dependency on an unfinished task.
- User-story tasks always carry `[USn]`; setup, foundational, and final tasks do not.
- The API-session ticket is an internal short-lived Identity cookie, not a browser cookie and not JWT.
- ApiService returns 401 for missing/invalid authentication and 403 for insufficient roles.
- Migrations, database update, builds/tests, AppHost, Docker, and infrastructure execution remain developer-controlled.
- Excluded identity features must not be added opportunistically.
