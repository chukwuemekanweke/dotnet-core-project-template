---
type: domain
title: Administrative Audit Journal
description: Unified append-only record of successful privileged human administrative mutations - scope, actor and tenant semantics, transactional recording, catalogs, change payloads, read/export API
resource: okf://knowledge/domains/administrative-audit
tags:
    - constraint
    - domain
    - auditing
    - administration
    - authorization
governance: constraint
code_refs:
    - src/BackendProjectTemplate.Domain/Auditing/**
    - src/BackendProjectTemplate.Application/Auditing/**
    - src/BackendProjectTemplate.Infrastructure/Persistence/AdministrativeAudit*
    - src/BackendProjectTemplate.Infrastructure/Persistence/Configurations/AdministrativeAuditConfiguration.cs
    - src/BackendProjectTemplate.Infrastructure/Persistence/AdminUserMutationExecutor.cs
    - src/BackendProjectTemplate.WebAPI/Features/Auditing/**
    - tests/unit/BackendProjectTemplate.Domain.UnitTests/Auditing/**
    - tests/unit/BackendProjectTemplate.Application.UnitTests/Auditing/**
    - tests/unit/BackendProjectTemplate.WebAPI.UnitTests/Features/Auditing/**
    - tests/integration/BackendProjectTemplate.WebAPI.IntegrationTests/Auditing/**
sources:
    - kind: file
      path: src/BackendProjectTemplate.Domain/Auditing/Entities/AdministrativeAudit.cs
    - kind: file
      path: src/BackendProjectTemplate.Infrastructure/Persistence/AdministrativeAuditRecorder.cs
generated:
    at: "2026-11-26T00:00:00Z"
    by: agent
status: stable
---
## Scope

`AdministrativeAudit` (`auditing.AdministrativeAudits`) records only **successfully committed, state-changing,
privileged administrative mutations performed by an authenticated human** — including delegated roles such
as FinanceOfficer. Never record reads, dashboard visits, ordinary user/customer operations (even when an
administrator performs them), payments, webhooks, Consumer/Jobs work, scheduled lifecycle transitions,
failed/denied/rejected operations, no-ops or idempotent replays. It is a record of administrative business
intent, not of database changes: no EF change-tracking diff, HTTP body capture, event sourcing, outbox or
async projection. It complements, and never replaces, domain history (`AdministrativeSecurityAudit`,
`SubscriptionAllocation`, `SubscriptionStatusHistory`), which must keep being written. The journal started
empty at deployment; never backfill it from other tables or telemetry.

## Recording contract

A slice calls `IAdministrativeAuditRecorder.RecordAsync(AdministrativeAuditEntry)` explicitly, **after**
its business change succeeded and **before** its own `SaveChanges`/commit. The recorder only stages the
row on the scoped `AppDbContext`; it never saves, so the mutation and its audit commit or roll back
together. Recording failure must fail the mutation. Multi-save operations wrap all saves in one explicit
transaction (`ActivateProviderHandler` uses `IUnitOfWork.BeginTransactionAsync` because the active-provider
partial unique index needs deactivations persisted first; `AdminUserMutationExecutor` records inside its
advisory-locked transaction, next to `AdministrativeSecurityAudit`). Each independently affected resource
gets its own record sharing the request `CorrelationId` (provider switch = one deactivated + one activated
record); composite actions such as revoke-all-sessions are one user-targeted record.

New privileged mutation checklist: add an action to `AdministrativeAuditActions` (and a target type if
needed), record only on the changed/success path, pick explicit safe `Changes` fields, carry the
administrator reason if one exists, and test recorded/no-op/failure paths.

## Identity and tenants

The actor is resolved server-side from `ActorContext.StakeholderId` (validated token claim) →
`Stakeholder.AppUserId`; `ActorUserId` is the Identity user and is never interchangeable with
`ActorStakeholderId`. Callers that already hold the Identity user (user administration) pass it and the
recorder rejects a mismatch. `ActorDisplayName` is a name snapshot with fallback `Unnamed administrator`;
never store emails or phone numbers. `ActorTenantId` is the request tenant (`X-Tenant-Id`);
`TargetTenantId` comes from the affected resource. `AdministrativeAuditTargetTypes` declares each type
tenant-scoped (`Subscription`, `AuthenticationSession`) or global (`User`, `SubscriptionPlan`, `Provider`,
`PaymentProvider`) and the factory rejects a missing tenant on scoped types and any tenant on global ones,
so null always means deliberately global. Identifiers are logical references: no FKs, no navigations.

## Catalogs and changes

Actions are stable `resource.verb` strings in `AdministrativeAuditActions` (validated on create; never
rename — move to `Retired`, which stays filterable). `Changes` is nullable jsonb shaped
`{"camelCaseField": {"previous": v, "current": v}}`, built with `AdministrativeAuditChanges` (drops
unchanged fields, keeps JSON types, max 16 KB). Pass enum names, never entities, credentials, provider
configuration, tokens, hashes or stamps. `Reason` is optional (max 1000) because not every operation
requires one; plan, provider, payment-provider and fulfillment-retry requests accept an optional `reason`.

## Read API

`GET /api/v1/admin/audit-logs` (`audit.read`) and `GET /api/v1/admin/audit-logs/export` (`audit.export`),
both Administrator-only and separate from mutation permissions; no create/update/delete endpoints and no
step-up. The journal is platform-wide (cross-tenant admin model); `tenantId` filters by target tenant and
never grants access. Filters: search (ILIKE with escaped wildcards over actor name, target name, reason),
action (current or retired catalog), actorUserId, targetType, targetId, tenantId, correlationId,
fromUtc (inclusive), toUtc (exclusive). Order `OccurredAtUtc DESC, Id DESC`, page/pageSize ≤ 100 with total
count, `AppReadDbContext` projections. Export uses the same filters, allowlisted columns (no `Changes`),
RFC 4180 quoting and apostrophe-prefixing of formula-leading cells, and returns 422 rather than a
truncated file above 10,000 rows. Append-only is an application guarantee, not database tamper-proofing.
