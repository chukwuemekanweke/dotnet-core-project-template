---
type: domain
title: Administrative Authorization Domain
description: Static roles and permissions, active-session-composed policies, deterministic Identity seeding, and first-administrator bootstrap
resource: okf://knowledge/domains/authorization
tags:
    - constraint
    - domain
    - authorization
    - identity
    - permissions
governance: constraint
code_refs:
    - src/BackendProjectTemplate.Domain/Common/Authorization/**
    - src/BackendProjectTemplate.Domain/Common/Authentication/StepUpMfa*
    - src/BackendProjectTemplate.Application/Authentication/Features/VerifyStepUpMfa/**
    - src/BackendProjectTemplate.Application/Authentication/Features/*AdminUser*/**
    - src/BackendProjectTemplate.Domain/Authentication/Administration/**
    - src/BackendProjectTemplate.Infrastructure/Persistence/AdminUser*
    - src/BackendProjectTemplate.WebAPI/Features/Authentication/AdminUsers/**
    - src/BackendProjectTemplate.Infrastructure/Authorization/**
    - src/BackendProjectTemplate.Infrastructure/Authentication/ServiceCollectionExtensions.cs
    - src/BackendProjectTemplate.Infrastructure/Authentication/RedisStepUpMfaProofStore.cs
    - src/BackendProjectTemplate.Infrastructure/Persistence/DatabaseInitializationExtensions.cs
    - src/BackendProjectTemplate.WebAPI/Features/Providers/ProvidersController.cs
    - src/BackendProjectTemplate.WebAPI/Features/Payments/Providers/PaymentProvidersController.cs
    - src/BackendProjectTemplate.WebAPI/Features/Payments/AdminPayments/Analytics/**
    - src/BackendProjectTemplate.WebAPI/Features/Kyc/AdminKyc/**
    - src/BackendProjectTemplate.WebAPI/Features/Subscriptions/AdminSubscriptions/AdminSubscriptionsController.cs
    - src/BackendProjectTemplate.WebAPI/Features/Subscriptions/AdminSubscriptionPlans/AdminSubscriptionPlansController.cs
    - src/BackendProjectTemplate.WebAPI/Features/Authentication/Security/StepUpMfaController.cs
    - src/BackendProjectTemplate.WebAPI/Infrastructure/StepUpMfa/**
    - tests/unit/BackendProjectTemplate.Domain.UnitTests/Authorization/**
    - tests/unit/BackendProjectTemplate.Infrastructure.UnitTests/Authorization/**
    - tests/integration/BackendProjectTemplate.WebAPI.IntegrationTests/Authorization/**
    - tests/integration/BackendProjectTemplate.WebAPI.IntegrationTests/Authentication/Security/StepUpMfa/**
sources:
    - kind: file
      path: src/BackendProjectTemplate.Domain/Common/Authorization/RolePermissionMappings.cs
generated:
    at: "2026-11-15T00:00:00Z"
    by: agent
status: stable
---
## Boundary

ASP.NET Core Identity roles and the existing `AppUser` are authoritative. Roles, permissions, and
role-to-permission mappings are static source-controlled catalogs; do not add runtime IAM CRUD, a
parallel admin identity, or an `IsAdmin` flag. Administrative role assignment may only change membership
in the four known roles; it never changes the role or permission catalogs.

## Evaluation

Permission policies require authentication plus both `ActiveSessionRequirement` and
`PermissionRequirement`. `IdentityUserPermissionService` resolves current server-side role membership
on every authorized request and evaluates `RolePermissionMappings`; JWT permission or role claims are
not authoritative. Role additions/removals converge on the next request with no permission cache.
Unknown permissions, deleted/missing users, and dependency failures fail closed. Resource and tenant
authorization still applies independently.

Privileged administrative mutations additionally use `RequireStepUpMfa` with a stable operation from
`StepUpMfaOperations`. The resource filter runs after the existing authorization policies and before
MVC model binding, requires authenticator enrollment, fingerprints the method, registered operation,
versioned path, sorted query, and recursively property-sorted JSON body, and atomically consumes the
proof. Reads and customer-scoped mutations do not receive this requirement.

`POST /api/v1/authentication/security/step-up/verifications` accepts only Identity authenticator TOTP,
never recovery codes or passkeys. Proofs are opaque, last exactly two minutes, and bind AppUser,
stakeholder, active session, Identity security stamp, operation, and request fingerprint. Redis stores
only a SHA-256 token verifier and bound metadata; the Redis library's native atomic `GETDEL` operation
gives consume-once behavior across instances. Binding validation follows retrieval, so mismatches also
consume the one-time proof. Redis failure fails closed. A distributed replay marker prevents the same TOTP code
from issuing multiple proofs during the configured replay window. Security-stamp changes and session
revocation invalidate outstanding authorization.

## Persistence and bootstrap

Database initialization reconciles known Identity roles and their `permission` role claims under a
PostgreSQL transaction-scoped advisory lock. Reconciliation is idempotent, removes stale/duplicate
permission claims, preserves unrelated claims and user-role membership, and runs after EF migrations.
Initial Administrator bootstrap targets an existing verified user by GUID from privileged deployment
configuration. A role claim marker makes bootstrap globally one-time; recovery requires deliberate
removal of the marker through controlled database maintenance before rerunning the migrator.

CAF-BE-017 administrative user security mutations share a PostgreSQL transaction-scoped advisory lock.
Locking an Administrator and removing the Administrator role count usable (verified, non-deleted,
non-administratively-locked) administrators while that lock is held, preventing concurrent requests from
removing or locking the final usable Administrator. The bootstrap marker is never reset by these APIs.
Successful mutations and `AdministrativeSecurityAudit` creation commit in the same database transaction.

## Current endpoint matrix

General provider activation requires `providers.manage` (Administrator only). Payment-provider
activation requires `payments.manage` (Administrator and FinanceOfficer); payment-provider listing on the
same controller requires `payments.read`, so policies are applied per action rather than at controller
level — never reintroduce a controller-level `payments.manage` that would make the read require it. Customer payment initiation
and stakeholder-owned wallet history are not administrative operations and retain their existing
active-session and resource scoping.

Administrative payment analytics requires the dedicated `payments.analytics.read` permission, granted
only to Administrator and FinanceOfficer. These read-only reports are cross-tenant by design and may be
narrowed with an explicit tenant filter; authorization is evaluated before any tenant-scoped cache read.

Administrative KYC list/detail/summary monitoring requires `kyc.read` and is cross-tenant by the same platform-admin
model; an explicit tenant filter narrows reporting but is not an authorization grant. These endpoints are
read-only and do not require step-up MFA.

Administrative subscriptions: list/detail and the full plan catalog require `subscriptions.read`
(Administrator, FinanceOfficer, SupportAgent); allocation requires `subscriptions.allocate`
(Administrator, FinanceOfficer); immediate/period-end cancellation and blocked-purchase fulfillment
retries require `subscriptions.manage` (Administrator, FinanceOfficer); plan create/update/activation
requires `subscriptions.plans.manage` (Administrator only). These endpoints are cross-tenant by design:
the target tenant is an explicit input, and permission policies — not the caller's own tenant — gate
access. Permissions are applied per action because one controller mixes permission levels.

Administrative subscription analytics requires the dedicated `subscriptions.analytics.read` permission,
granted only to Administrator and FinanceOfficer. Its five read-only endpoints are cross-tenant by design;
tenant and owner filters narrow the report but never grant access, and authorization completes before any
analytics cache lookup.

Every mutation described above also requires its operation-specific step-up proof. The current stable
operation catalog covers general/payment provider activation, subscription allocation, both
administrative cancellation forms, fulfillment retry, and plan create/update/activation. A new
privileged mutation is incomplete until its operation contract, `RequireStepUpMfa` attribute, and proof
binding tests are added. Proof consumption precedes MVC validation and business execution, so validation
or execution failure requires a new proof; domain idempotency remains responsible for lost responses and
retries because Redis and PostgreSQL do not share a distributed transaction.

Administrative users: list/detail/current-role reads require `users.read`. Lock/unlock, revoke one/all
sessions, and assign/remove a role require `users.manage` plus operation-specific step-up proofs. Roles
are limited to `ApplicationRoles.All`; the APIs do not edit the catalog, permission mappings, or Identity
role claims. Request fingerprints bind the target AppUser, session or role path where applicable, and the
reason body. Account mutation resolves AppUser-to-Stakeholder relationships before session operations.
Administrative lock is dedicated AppUser state rather than failed-login lockout; locking rotates the
security stamp and revokes active sessions, while unlock never reactivates revoked sessions.

Administrative registration and active-user analytics require `users.analytics.read`, granted only to
Administrator. These read-only endpoints follow the platform cross-tenant model: an optional tenant filter
narrows data but does not grant access, and permission authorization completes before aggregate cache access.
