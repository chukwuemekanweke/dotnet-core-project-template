# Administrative authorization

Administrative authorization uses ASP.NET Core Identity roles, static application permissions, and
policy-based authorization. `AppUser` remains the only user identity. The application does not expose
role, permission, or role-mapping CRUD APIs.

## Catalogs and mappings

Role names and stable fresh-install identifiers live in `ApplicationRoles`. Permission identifiers
live in `ApplicationPermissions` and follow `resource.action`. `RolePermissionMappings` is the
source-controlled mapping:

| Permission | Administrator | ComplianceOfficer | SupportAgent | FinanceOfficer |
| --- | --- | --- | --- | --- |
| `users.read` | Yes | Yes | Yes | No |
| `users.manage` | Yes | No | No | No |
| `kyc.read` | Yes | Yes | No | No |
| `kyc.manage` | Yes | Yes | No | No |
| `payments.read` | Yes | No | No | Yes |
| `payments.manage` | Yes | No | No | Yes |
| `subscriptions.read` | Yes | No | Yes | Yes |
| `subscriptions.manage` | Yes | No | No | Yes |
| `subscriptions.allocate` | Yes | No | No | Yes |
| `subscriptions.plans.manage` | Yes | No | No | No |
| `providers.manage` | Yes | No | No | No |
| `audit.read` | Yes | No | No | No |
| `audit.export` | Yes | No | No | No |

`payments.manage` is separate from `payments.read`; a read capability never authorizes payment-provider
activation. `PaymentProvidersController` therefore applies its policies per action: listing requires
`payments.read`, while activation requires `payments.manage` and a step-up proof. `providers.manage` is restricted to Administrator because general provider activation can
change non-payment infrastructure.

## Policy enforcement

Protect an administrative controller or action with a constant from `PermissionPolicyNames`, for
example:

```csharp
[Authorize(Policy = PermissionPolicyNames.PaymentsManage)]
```

Every registered permission policy requires authentication, `ActiveSessionRequirement`, and a
`PermissionRequirement`. The permission handler resolves the JWT subject to the current `AppUser`,
loads current Identity role membership, and evaluates the static mapping. It does not trust JWT role
or permission claims. Unknown permissions, missing/deleted users, and dependency failures deny access.

Role additions and removals therefore apply on the next authorized request, including requests made
with an already-issued access token. Existing session revocation and expiry remain independently
enforced. Tenant and resource checks are not replaced by an administrative permission.

Privileged state-changing actions additionally carry `[RequireStepUpMfa(<operation>)]`. This resource
filter runs after normal authentication, active-session, and permission authorization and before MVC
model binding or the business handler. It requires an enrolled authenticator and atomically consumes
one proof from `X-Step-Up-Authorization`. MFA never replaces permission evaluation: removing a role or
revoking a session denies the request before the proof is used. Administrative reads do not require a
step-up proof.

Current protected operations are:

| Operation | Policy |
| --- | --- |
| General provider activation | `providers.manage` |
| Payment-provider activation | `payments.manage` |
| Payment-provider listing (`GET /payment-providers`) | `payments.read` |
| Admin KYC list, detail and summary (`GET /admin/kyc`, `/admin/kyc/{id}`, `/admin/kyc/summary`) | `kyc.read` |
| Admin subscription list and detail (`GET /admin/subscriptions[/{id}]`) | `subscriptions.read` |
| Admin plan catalog including inactive plans (`GET /admin/subscription-plans`) | `subscriptions.read` |
| Subscription allocation (`POST /admin/subscriptions/allocations`) | `subscriptions.allocate` |
| Immediate / period-end cancellation, blocked-purchase fulfillment retry | `subscriptions.manage` |
| Plan create, update, activation/deactivation | `subscriptions.plans.manage` |
| Admin user list, detail, and role reads | `users.read` |
| Admin user lock/unlock, session revocation, and seeded-role changes | `users.manage` |
| Administrative audit history (`GET /admin/audit-logs`) | `audit.read` |
| Administrative audit CSV export (`GET /admin/audit-logs/export`) | `audit.export` |

The audit rows are reads and do not require step-up MFA. Being allowed to perform an administrative
mutation never grants reading or exporting the audit history; see
[administrative-audit.md](administrative-audit.md). All mutation rows in this table require step-up MFA. Their stable operations are
`admin.providers.activate`, `admin.payment-providers.set-activation`,
`admin.subscriptions.allocate`, `admin.subscriptions.cancel-immediately`,
`admin.subscriptions.cancel-at-period-end`, `admin.subscriptions.retry-purchase-fulfillment`,
`admin.subscription-plans.create`, `admin.subscription-plans.update`, and
`admin.subscription-plans.set-activation`. Administrative user mutations use
`admin.users.lock`, `admin.users.unlock`, `admin.users.sessions.revoke`,
`admin.users.sessions.revoke-all`, `admin.users.roles.assign`, and
`admin.users.roles.remove`. Their proof fingerprint binds the target user, optional session or role,
and the reason body, so a proof cannot be redirected to another account or operation.

## Administrative user management

`GET /api/v1/admin/users` provides server-side search, role/account-lock/tenant filters, deterministic
ordering, offset pagination, total count, and explicit safe projections. `GET /api/v1/admin/users/{id}`
returns the bounded Identity, stakeholder, latest KYC, active-session, latest-login, and current
stakeholder-subscription summaries. `GET /api/v1/admin/users/{id}/roles` returns current server-side
Identity membership. These reads require `users.read`; they never return password/token hashes,
security stamps, authenticator material, recovery codes, or refresh tokens.

The privileged endpoints are explicit:

| Endpoint | Behavior |
| --- | --- |
| `POST /api/v1/admin/users/{id}/lock` | Administrative lock and revoke all target sessions |
| `POST /api/v1/admin/users/{id}/unlock` | Remove only the administrative lock; revoked sessions remain revoked |
| `DELETE /api/v1/admin/users/{id}/sessions/{sessionId}` | Revoke one session after verifying target ownership |
| `DELETE /api/v1/admin/users/{id}/sessions` | Revoke all sessions resolved through the target's stakeholder records |
| `PUT /api/v1/admin/users/{id}/roles/{roleName}` | Assign one known seeded role idempotently |
| `DELETE /api/v1/admin/users/{id}/roles/{roleName}` | Remove one known seeded role idempotently |

Each requires `users.manage`, a request-bound step-up proof, and a non-empty reason. Role names are
restricted to `ApplicationRoles.All`; role definition, renaming, deletion, permission editing, and
role-claim editing remain unavailable through HTTP.

Administrative lock is dedicated persistent state on `AppUser`, distinct from Identity's temporary
failed-login lockout. Shared account-eligibility checks treat either state as locked. Locking rotates the
security stamp and revokes every active session resolved through the user's stakeholder association;
active-session authorization and refresh-token rotation therefore fail for previously issued sessions.
Unlocking does not resurrect them. Password, Google, passkey, MFA-continuation, and refresh workflows all
reuse the same Identity eligibility service.

Locking yourself and removing your own Administrator role are forbidden. Locking an Administrator or
removing that role takes a PostgreSQL transaction-scoped advisory lock and rechecks the number of usable,
email-confirmed Administrators inside the transaction. This serializes competing requests and prevents
the final usable Administrator from being removed or locked. The one-time bootstrap marker is never reset;
recovery still follows the controlled maintenance procedure below.

Successful security mutations write `authentication.AdministrativeSecurityAudits` in the same database
transaction as the committed change. Records include actor and target AppUser IDs, action, reason,
timestamp, request tenant/correlation/flow context, and previous/resulting role sets. Failed and unchanged
operations do not create misleading success rows. Tenant-scoped stakeholder, KYC, subscription, and
session information is always joined from the global AppUser relationship; AppUser and Stakeholder IDs
are never treated as interchangeable.

## Privileged-action step-up MFA

An active administrator requests a proof with
`POST /api/v1/authentication/security/step-up/verifications`. The JSON request contains
`verificationMethod: "authenticator"`, the six-digit `code`, a registered `operation`, and an
`intendedRequest` containing the exact HTTP `method`, versioned `path`, optional query values, and
optional JSON body of the mutation. Actor, stakeholder, and session identifiers always come from the
authenticated JWT and server-side session; clients never supply them. Recovery codes and passkeys are
not accepted by this endpoint.

The server validates the operation contract and creates the request fingerprint as SHA-256 over:

```text
UPPERCASE_METHOD\n
operation\n
lowercase_versioned_path_without_trailing_slash\n
query\n
canonical_json_body
```

Query keys and each key's values are ordinal-sorted and percent-encoded. JSON object properties are
ordinal-sorted recursively; array order and JSON scalar representations are preserved. Clients do not
calculate or submit a fingerprint. They submit the intended request fields, and the server applies the
same canonicalizer to those fields and to the eventual protected HTTP request. Changing the method,
operation, route GUID, query value, or payload changes the fingerprint.

Successful verification returns an opaque 256-bit `authorizationToken` and `expiresAtUtc`, with
`Cache-Control: no-store`. The raw token is returned once and never logged or stored. Redis keys use a
SHA-256 token verifier and store the user, stakeholder, session, Identity security stamp, operation,
fingerprint, verification time, and expiry. The Redis library's native atomic `GETDEL` operation consumes
the proof before the application compares its bindings, so concurrent API instances cannot accept it
twice. A binding mismatch therefore also consumes the one-time proof. Consumed and expired markers remain
briefly to provide stable errors. Redis failure denies the action. The same TOTP code cannot issue a
second proof during the configured replay window; Identity failed-access tracking and the existing MFA
verification rate limit also apply.

Proof lifetime is fixed at two minutes under `Authentication:StepUpMfa`; `ReplayWindow` defaults to two
minutes. Startup rejects a different proof lifetime or a replay window outside 30 seconds to 5 minutes.
Stable failures include `mfa_enrollment_required`, `mfa_verification_required`,
`mfa_verification_failed`, `mfa_proof_expired`, `mfa_proof_invalid`, `mfa_proof_consumed`,
`mfa_operation_mismatch`, and `mfa_verification_rate_limited`.

Authorization consumes the proof before MVC request validation. If validation or business execution
then fails, the proof remains consumed and the client must verify again before retrying. A lost success
response is handled by the operation's existing idempotency key or domain idempotency behavior; the MFA
proof is never reusable. There is deliberately no distributed transaction between Redis consumption
and PostgreSQL mutation.

To protect a new privileged mutation, add a stable constant to `StepUpMfaOperations`, register its
method and versioned path in `StepUpMfaOperationRegistry`, and apply
`[RequireStepUpMfa(StepUpMfaOperations.YourOperation)]` alongside its permission policy. Add canonical
request, proof lifecycle, and endpoint coverage. Do not apply step-up globally to customer mutations.

Administrative subscription endpoints are cross-tenant: the target tenant and subscription are explicit
request inputs, the acting administrator is always taken from the authenticated session, and permission
policies are applied per action. Plan management is restricted to Administrator because it changes the
commercial catalog for every tenant.

Customer payment initiation, subscription purchase/current-subscription/cancellation, and access to a
stakeholder's own wallet transactions remain active-session and resource-scoped operations. The user and
KYC permissions are foundations for their future feature endpoints.

## Deterministic seeding

Database initialization creates the four known Identity roles and reconciles their `permission` role
claims after EF Core migrations. Seeding holds a PostgreSQL transaction-scoped advisory lock, is safe
to repeat, removes stale or duplicate permission claims, preserves unrelated claims, and never changes
user-role membership. Changing a mapping requires a code review and a database-migrator run.

To add a permission, add its stable identifier to `ApplicationPermissions`, add the least-privilege
mapping in `RolePermissionMappings`, add a policy constant when an endpoint uses it, apply that policy,
and extend the catalog and HTTP authorization tests.

## Initial Administrator bootstrap

1. Register and verify the intended existing account normally.
2. Supply these settings only to the trusted DatabaseMigrator deployment:

   - `Authorization__AdministratorBootstrap__Enabled=true`
   - `Authorization__AdministratorBootstrap__UserId=<AppUser GUID>`

3. Run DatabaseMigrator and confirm its safe audit log identifies the bootstrapped user ID.
4. Remove or disable the bootstrap settings after the successful run.

Bootstrap refuses missing, deleted, or email-unverified users and never creates an account or password.
It records `authorization.bootstrap=completed` as a non-permission claim on the Administrator role.
That global marker prevents a later deployment from silently reassigning Administrator, even if the
original user's role is removed. Recovery is an explicit privileged operation: confirm no valid
Administrator remains, remove that marker in a controlled database maintenance transaction, configure
the verified recovery user ID, run DatabaseMigrator once, and disable the settings again.

CAF-BE-017 assigns and removes the seeded roles through the authenticated, audited, step-up-protected
administrative workflow described above. Role definitions and permission mappings remain source controlled.

## Verification matrix

HTTP integration coverage asserts unauthenticated `401`, authenticated-without-permission `403`, the
allow/deny matrix for all four roles across both administrative mutations, and immediate denial after
role removal with the same JWT. Separate integration coverage verifies repeatable role/claim seeding,
stale-claim reconciliation, user-role preservation, and one-time bootstrap behavior. Unit coverage
verifies catalog uniqueness, valid mappings, least privilege, multi-role unions, unknown-permission
denial, and dependency fail-closed behavior. Administrative-user coverage additionally verifies safe
read projections and filters, seeded-role assignment, unknown-role rejection, session invalidation,
lock/unlock authentication effects, durable audits, and concurrent last-Administrator protection.
