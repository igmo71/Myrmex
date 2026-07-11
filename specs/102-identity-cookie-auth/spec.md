# Feature Specification: Identity Cookie Authentication

**Feature Branch**: `102-introduce-aspnet-core-identity-cookie-authentication-for-myrmex-webappapi`

**Created**: 2026-07-07

**Status**: Draft

**Input**: User description: "StakeholderDocs/102 Introduce ASP.NET Core Identity cookie authentication for Myrmex.md"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Sign In to Protected Operations (Priority: P1)

A registered warehouse operator signs in through the WebApp, sees who is signed in, and uses protected warehouse functionality. The same authenticated identity is recognized when the WebApp accesses protected application services, and operational actions receive that user's stable actor identifier.

**Why this priority**: Secure operator access is the primary production capability and removes reliance on development-only actor impersonation.

**Independent Test**: Provision an operator account, sign in, access a protected WMS page and operation, verify the displayed current user and recorded actor identifier, then sign out and verify access is no longer available.

**Acceptance Scenarios**:

1. **Given** an active user assigned the WMS operator role, **When** the user submits valid credentials, **Then** the user is signed in and can access WMS functionality governed by the WMS operator policy.
2. **Given** a signed-in operator, **When** a protected operation is performed, **Then** the operation receives the stable identifier of that operator as its actor identifier.
3. **Given** a signed-in user, **When** the user views the application layout, **Then** enough identity information is displayed to distinguish the current account.
4. **Given** a signed-in operator, **When** the user signs out, **Then** the browser session ends and subsequent protected page and service access requires a new sign-in.
5. **Given** an anonymous user requests a protected WebApp area, **When** the request is handled, **Then** the user is directed to sign in and can return to the originally requested safe local destination after successful authentication.

---

### User Story 2 - Enforce Role-Based Access (Priority: P1)

The application consistently permits warehouse operations to operators and administrators while denying anonymous users and signed-in users without an eligible role. Authorization remains enforced by protected application services rather than by UI visibility alone.

**Why this priority**: Authentication without consistent server-side authorization would expose operational and administrative capabilities.

**Independent Test**: Exercise representative protected WMS, OneC, demo-data, and user-administration operations as an anonymous user, an unprivileged authenticated user, an operator, and an administrator, and verify the expected allow or deny result for each role.

**Acceptance Scenarios**:

1. **Given** a user assigned the WMS operator role, **When** the user accesses an operation governed by the WMS operator policy, **Then** access is permitted.
2. **Given** a user assigned the application administrator role, **When** the user accesses an operation governed by the WMS operator policy, **Then** access is permitted.
3. **Given** an authenticated user with neither supported role, **When** the user accesses a WMS operator operation, **Then** access is denied and no operation is performed.
4. **Given** an authenticated non-administrator, **When** the user attempts to access user creation through either UI or direct service request, **Then** access is denied.
5. **Given** an authenticated user who lacks access, **When** a WebApp page is requested, **Then** the user sees a clear access-denied experience rather than a generic application failure.

---

### User Story 3 - Bootstrap the First Administrator Safely (Priority: P2)

An authorized deployer can explicitly bootstrap the first application administrator using deployment-provided secret configuration, without Myrmex shipping a known credential or silently changing an existing account.

**Why this priority**: A secure initial administrator is required to make production user provisioning usable without creating a hidden default credential.

**Independent Test**: Start from an empty identity store with bootstrap disabled, enabled but incomplete, enabled with valid secret values, and enabled after the administrator already exists; verify safe behavior in every case.

**Acceptance Scenarios**:

1. **Given** initial-administrator bootstrap is disabled, **When** the application starts, **Then** no administrator account is created.
2. **Given** bootstrap is enabled but a required identity or password value is missing, **When** startup occurs, **Then** administrator creation is refused with a clear diagnostic and no partial account is created.
3. **Given** bootstrap is explicitly enabled with valid secret-provided values and no matching account exists, **When** startup occurs, **Then** exactly one administrator account is created and assigned the administrator role.
4. **Given** the configured administrator already exists, **When** bootstrap runs again, **Then** no duplicate account is created and the existing password is not overwritten.
5. **Given** any bootstrap outcome, **When** diagnostics are recorded, **Then** they indicate whether bootstrap occurred or was skipped without exposing the password.

---

### User Story 4 - Administrator Creates Users (Priority: P2)

An application administrator creates an additional user with an email address, optional display name, temporary password, and one or more supported roles so warehouse staff can receive individual accounts.

**Why this priority**: Ongoing secure operation requires named accounts without direct database changes or shared credentials.

**Independent Test**: Sign in as an administrator, create an operator account, verify duplicate and invalid submissions are rejected clearly, and confirm the created account can sign in with only its assigned permissions.

**Acceptance Scenarios**:

1. **Given** an authenticated application administrator, **When** valid new-user details and the WMS operator role are submitted, **Then** one user is created with that role and can sign in.
2. **Given** an authenticated application administrator, **When** the administrator role is assigned during creation, **Then** the new user can access administrator-only user creation and WMS operator functionality.
3. **Given** an existing account with the same normalized sign-in identity, **When** another creation request uses that identity, **Then** creation is rejected without altering the existing account.
4. **Given** an unsupported role or a password that fails the active password rules, **When** user creation is submitted, **Then** no user is created and the administrator receives actionable validation feedback.

---

### User Story 5 - Retain Explicit Non-Production Test Access (Priority: P3)

Developers and automated endpoint tests can continue using explicitly enabled non-production authentication support while production always requires the real user authentication path.

**Why this priority**: Existing protected endpoint development and testing must remain practical without allowing a development shortcut to become a production bypass.

**Independent Test**: Verify the development actor works only in an allowed environment with explicit enablement, remains disabled otherwise, and cannot become the production default.

**Acceptance Scenarios**:

1. **Given** an allowed non-production environment and explicit enablement, **When** the development actor is used, **Then** protected test or development scenarios continue to work.
2. **Given** production or absent explicit enablement, **When** a request attempts to use the development actor, **Then** authentication is refused.

### Edge Cases

- A signed-in account is deleted, disabled, or has roles changed while a browser session remains open; subsequent protected requests must be reevaluated and denied when the account is no longer eligible.
- A return destination supplied to sign-in is external or malformed; only safe local destinations may be used after authentication.
- Multiple application instances attempt initial-administrator bootstrap concurrently; the result must still be at most one account with no password overwrite.
- User creation partially fails while assigning roles; the system must not report success or leave an account that appears fully provisioned when required role assignment failed.
- The WebApp session is valid but protected application-service authentication cannot be established; the request must fail closed rather than fall back to anonymous or development identity.
- The authenticated principal lacks a stable user identifier; WMS operator authorization and actor resolution must fail rather than invent an identifier from email or display name.
- A password or other secret contains characters requiring encoding; its value must be handled without alteration or disclosure.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide a production sign-in flow using registered user credentials and a secure browser session.
- **FR-002**: The system MUST provide sign-out that invalidates the current browser session for subsequent protected WebApp and application-service access.
- **FR-003**: The WebApp MUST provide localized sign-in, sign-out, access-denied, and current-user experiences consistent with existing application conventions.
- **FR-004**: Anonymous users requesting protected WebApp areas MUST be directed to sign in, with only safe local return destinations accepted.
- **FR-005**: Protected application-service operations MUST independently authenticate and authorize the current user; UI visibility MUST NOT be treated as authorization.
- **FR-006**: The authenticated identity used by the WebApp MUST be deliberately and securely recognized by protected application-service requests without introducing an anonymous bypass.
- **FR-007**: The system MUST define the `MyrmexAdmin` and `WmsOperator` roles.
- **FR-008**: The existing WMS operator policy MUST require an authenticated user, a non-empty stable user identifier, and either the `WmsOperator` or `MyrmexAdmin` role.
- **FR-009**: Existing protected WMS, OneC, and demo-data operations MUST remain protected after the authentication change.
- **FR-010**: Actor resolution MUST use the authenticated user's stable identity identifier and MUST NOT use email, display name, or another mutable label as the actor identifier.
- **FR-011**: The system MUST provide an administrator-only flow to create a user with an email sign-in identity, optional display name, temporary password, and one or more supported roles.
- **FR-012**: User creation MUST reject duplicate normalized sign-in identities, unsupported roles, invalid passwords, and incomplete required data without altering an existing user.
- **FR-013**: User creation MUST be authorized server-side for `MyrmexAdmin` users only.
- **FR-014**: The system MUST support explicit initial-administrator bootstrap using an enable flag, administrator identity, and password supplied through deployment configuration.
- **FR-015**: Initial-administrator bootstrap MUST be disabled by default and MUST NOT rely on committed, hardcoded, or otherwise hidden production credentials.
- **FR-016**: When bootstrap is enabled with missing or invalid required values, the system MUST refuse bootstrap clearly and MUST NOT create a partial administrator.
- **FR-017**: Initial-administrator bootstrap MUST be idempotent, MUST NOT create duplicates, and MUST NOT overwrite an existing user's password.
- **FR-018**: Passwords and equivalent secrets MUST never be written to logs, user-visible diagnostics, source-controlled configuration, or routine telemetry.
- **FR-019**: The development actor MAY remain available only in explicitly enabled Development or Staging environments and MUST NOT be the production default or a production fallback.
- **FR-020**: Existing endpoint test authentication support MUST remain available without weakening production authentication or authorization.
- **FR-021**: Authentication and authorization decisions MUST remain role-, claim-, and policy-based so a future non-browser authentication method can reuse the authorization model.

### Domain Rules

- **DR-001**: A user sign-in identity is unique after the system's standard normalization rules are applied.
- **DR-002**: `MyrmexAdmin` implicitly satisfies WMS operator access, but `WmsOperator` does not implicitly satisfy administrator access.
- **DR-003**: Every authenticated user used for protected WMS work has one stable, non-empty actor identifier for the lifetime of that account.
- **DR-004**: Only the supported roles `MyrmexAdmin` and `WmsOperator` may be assigned through the initial user-creation flow.
- **DR-005**: Initial-administrator bootstrap never resets or replaces credentials for an existing account.
- **DR-006**: Identity is a platform capability and its users, roles, credentials, and persistence are not owned by the WMS domain.

### Contract and Boundary Requirements

- **CB-001**: Public account and user-creation requests MUST expose only the fields required for the defined flows and MUST NOT expose password hashes, security tokens, or internal credential state.
- **CB-002**: Identity behavior and identity persistence MUST remain within a dedicated platform capability outside the WMS module; application hosts compose that capability without owning its business behavior.
- **CB-003**: The WebApp-to-application-service authentication boundary MUST have one explicit, secure, testable strategy selected during planning; it MUST NOT assume that separate applications automatically share authenticated state.
- **CB-004**: Authorization policies and stable actor resolution MUST behave consistently regardless of whether the authenticated principal originates from the browser-session flow, approved test authentication, or a future supported authentication method.
- **CB-005**: Account and administrative failures MUST follow existing WebApp and application-service error conventions without disclosing whether a supplied password was correct or exposing sensitive account state unnecessarily.

### Observability & Error Handling

- **OE-001**: Sign-in failures MUST provide a clear, non-sensitive message without revealing whether a particular account exists.
- **OE-002**: Access denials MUST be distinguishable from authentication-required outcomes for both WebApp users and protected service callers.
- **OE-003**: Initial-administrator bootstrap MUST record whether it was disabled, skipped because the account exists, completed, or refused because configuration is invalid, without recording the password.
- **OE-004**: User creation MUST record successful creation and authorization or validation failures with enough context for operators to troubleshoot, while excluding passwords and credential material.
- **OE-005**: Failure to carry an authenticated identity across the WebApp/application-service boundary MUST fail closed and provide diagnostics sufficient to identify the boundary failure.

### Scope Boundaries

- JWT bearer authentication, mobile/TSD authentication, external API authentication, machine-to-machine authentication, and external OAuth/OIDC providers are out of scope.
- Dedicated identity servers, self-registration, email confirmation, password-reset email, two-factor authentication, and custom lockout tuning are out of scope.
- Full user management, user edit/delete/deactivate flows, role-management UI, warehouse-level permissions, tenant permissions, and an audit dashboard are out of scope.
- Removing the development actor entirely is out of scope; restricting it to explicitly enabled non-production use is in scope.

### Key Entities

- **User Account**: A named person allowed to authenticate; includes a stable identifier, unique normalized email sign-in identity, optional display name, credential state, and account security metadata.
- **Role**: A supported application access category. Initial roles are `MyrmexAdmin` and `WmsOperator`.
- **User Role Assignment**: The association granting one supported role to one user account.
- **Authenticated Session**: The browser's signed-in state for a user, including expiration and invalidation behavior without exposing credential material.
- **Initial Administrator Configuration**: Explicit deployment-provided enablement, identity, and secret values used only to create the first administrator safely and idempotently.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A provisioned operator can sign in, identify the active account, reach a protected WMS page, and sign out in under two minutes during an acceptance test.
- **SC-002**: Across the acceptance matrix, 100% of representative protected WMS, OneC, demo-data, and user-administration operations reject anonymous and ineligible users and permit the intended eligible roles.
- **SC-003**: In 100% of tested protected write operations, the resolved actor identifier equals the stable identifier of the signed-in user and is never derived from email or display name.
- **SC-004**: Repeating initial-administrator bootstrap at least three times creates exactly one administrator account and never changes that account's password after initial creation.
- **SC-005**: An administrator can create a valid operator account with an assigned supported role in under two minutes, and the new account can sign in without direct data-store intervention.
- **SC-006**: All tested missing, invalid, duplicate, unauthorized, and access-denied cases produce a clear outcome without exposing passwords or credential material.
- **SC-007**: Production configuration validation demonstrates zero available development-actor or hidden-default-credential authentication paths.
- **SC-008**: At least 95% of sign-in, sign-out, and access-denied page transitions complete within two seconds under normal acceptance-test conditions.

## Assumptions

- Email is the initial sign-in identifier; username-only sign-in and self-service identity changes are deferred.
- An administrator supplies a temporary password to the new user through an operationally appropriate channel outside this feature; forced first-login password change is not included.
- Standard secure password, session, expiration, and lockout defaults are acceptable unless planning identifies a conflict with deployment requirements.
- The WebApp and application service remain separate application processes, so planning must select and document an explicit authenticated-session propagation or routing strategy.
- The stable user identifier is immutable for the lifetime of an account and compatible with existing actor identifiers; its storage representation is selected during planning.
- Existing policy names and protected endpoint coverage remain stable except for extending the WMS operator policy to recognize the two supported roles and stable user identifier.
- User-facing account and administration text is provided in all currently supported WebApp cultures.
