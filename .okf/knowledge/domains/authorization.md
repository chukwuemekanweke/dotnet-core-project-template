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
    - src/BackendProjectTemplate.Infrastructure/Authorization/**
    - src/BackendProjectTemplate.Infrastructure/Authentication/ServiceCollectionExtensions.cs
    - src/BackendProjectTemplate.Infrastructure/Persistence/DatabaseInitializationExtensions.cs
    - src/BackendProjectTemplate.WebAPI/Features/Providers/ProvidersController.cs
    - src/BackendProjectTemplate.WebAPI/Features/Payments/Providers/PaymentProvidersController.cs
    - tests/unit/BackendProjectTemplate.Domain.UnitTests/Authorization/**
    - tests/unit/BackendProjectTemplate.Infrastructure.UnitTests/Authorization/**
    - tests/integration/BackendProjectTemplate.WebAPI.IntegrationTests/Authorization/**
sources:
    - kind: file
      path: src/BackendProjectTemplate.Domain/Common/Authorization/RolePermissionMappings.cs
generated:
    at: "2026-11-14T00:00:00Z"
    by: agent
status: stable
---
## Boundary

ASP.NET Core Identity roles and the existing `AppUser` are authoritative. Roles, permissions, and
role-to-permission mappings are static source-controlled catalogs; do not add runtime IAM CRUD, a
parallel admin identity, or an `IsAdmin` flag. User-role assignment APIs remain outside this boundary.

## Evaluation

Permission policies require authentication plus both `ActiveSessionRequirement` and
`PermissionRequirement`. `IdentityUserPermissionService` resolves current server-side role membership
on every authorized request and evaluates `RolePermissionMappings`; JWT permission or role claims are
not authoritative. Role additions/removals converge on the next request with no permission cache.
Unknown permissions, deleted/missing users, and dependency failures fail closed. Resource and tenant
authorization still applies independently.

## Persistence and bootstrap

Database initialization reconciles known Identity roles and their `permission` role claims under a
PostgreSQL transaction-scoped advisory lock. Reconciliation is idempotent, removes stale/duplicate
permission claims, preserves unrelated claims and user-role membership, and runs after EF migrations.
Initial Administrator bootstrap targets an existing verified user by GUID from privileged deployment
configuration. A role claim marker makes bootstrap globally one-time; recovery requires deliberate
removal of the marker through controlled database maintenance before rerunning the migrator.

## Current endpoint matrix

General provider activation requires `providers.manage` (Administrator only). Payment-provider
activation requires `payments.manage` (Administrator and FinanceOfficer). Customer payment initiation
and stakeholder-owned wallet history are not administrative operations and retain their existing
active-session and resource scoping.
