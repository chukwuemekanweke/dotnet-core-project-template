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
| `providers.manage` | Yes | No | No | No |

`payments.manage` is separate from `payments.read`; a read capability never authorizes payment-provider
activation. `providers.manage` is restricted to Administrator because general provider activation can
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

Current protected operations are:

| Operation | Policy |
| --- | --- |
| General provider activation | `providers.manage` |
| Payment-provider activation | `payments.manage` |

Customer payment initiation and access to a stakeholder's own wallet transactions remain active-session
and resource-scoped operations. The user, KYC, and subscription permissions are foundations for their
future feature endpoints; this work does not add those feature sets.

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

CAF-BE-015 may assign the seeded roles to users through an authenticated administrative workflow; it
must not make role definitions or permission mappings runtime-editable.

## Verification matrix

HTTP integration coverage asserts unauthenticated `401`, authenticated-without-permission `403`, the
allow/deny matrix for all four roles across both administrative mutations, and immediate denial after
role removal with the same JWT. Separate integration coverage verifies repeatable role/claim seeding,
stale-claim reconciliation, user-role preservation, and one-time bootstrap behavior. Unit coverage
verifies catalog uniqueness, valid mappings, least privilege, multi-role unions, unknown-permission
denial, and dependency fail-closed behavior.
