---
type: domain
title: Authenticated User Activity Domain
description: Tenant-aware qualifying activity observation, Redis window deduplication, durable event delivery, and daily PostgreSQL aggregation
resource: okf://knowledge/domains/user-activity
tags:
    - constraint
    - domain
    - analytics
    - user-activity
    - redis
    - messaging
sources:
    - resource: ""
generated:
    at: "2026-11-20T00:00:00Z"
    by: agent
status: stable
code_refs:
    - src/BackendProjectTemplate.Application/Analytics/**
    - src/BackendProjectTemplate.Contracts/Events/UserActivityObserved.cs
    - src/BackendProjectTemplate.Domain/Analytics/**
    - src/BackendProjectTemplate.Infrastructure/Analytics/**
    - src/BackendProjectTemplate.Infrastructure/Persistence/Configurations/User*Activity*.cs
    - src/BackendProjectTemplate.WebAPI/Infrastructure/*UserActivity*.cs
    - src/BackendProjectTemplate.Consumer/Analytics/**
    - src/BackendProjectTemplate.Consumer/Authentication/UserSignInSuccessfulHandler.cs
    - tests/**/Analytics/**
governance: constraint
---
## Qualification and identity

Observe authenticated product requests only after the downstream pipeline returns a 2xx response.
Resolve the stakeholder from authenticated identity and reconcile the request tenant with persisted
stakeholder ownership; never use the tenant header alone. `ExcludeFromUserActivity` marks passive or
nonqualifying endpoints. Refresh, health, metrics, documentation, webhook, and telemetry traffic do not
qualify.

Successful password, Google, passkey, and completed MFA sign-ins reuse `UserSignInSuccessful` and are
persisted as login activity. Failed credentials, incomplete challenges, and refreshes never count. Do
not publish a second login observation from the HTTP middleware.

## Request throttling and delivery

Authenticated request activity uses fixed, Unix-epoch-aligned five-minute windows. StackExchange.Redis
ownership-aware lock operations claim and conditionally release the tenant/stakeholder/reporting-date/window
key with a ten-minute TTL. Redis failure is fail-open
to the transactional outbox. A definite outbox-save failure conditionally releases only the matching
claim value; RabbitMQ failures are retried from the durable outbox.

The Consumer is the durable writer. `UserActivityObserved.MessageId` is the observation identity.
Tracked EF Core entities insert the receipt and create or update the daily aggregate inside a serializable
transaction. A bounded retry handles PostgreSQL serialization, deadlock, and unique-constraint races, so
duplicate, concurrent, and out-of-order delivery preserves one `(TenantId, StakeholderId, ActivityDate)`
row, increments only for a new observation ID, and maintains minimum/maximum UTC timestamps.

## Reporting date

The reporting calendar is UTC by design. Derive `ActivityDate` from the UTC observation timestamp without
server-local, configurable, or tenant-specific timezone conversion. Persisted dates are historical facts
and are never recalculated.
