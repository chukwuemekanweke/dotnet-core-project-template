---
type: domain
title: Subscriptions Domain
description: Provider-agnostic subscription plans, BillingAccount ownership, purchase correlation, administrative allocation, the one-entitlement-per-BillingAccount invariant, lifecycle periods, status history, and cancellation semantics
resource: okf://knowledge/domains/subscriptions
tags:
    - context
    - domain
    - subscriptions
    - billing
    - payments
    - lifecycle
sources:
    - resource: ""
generated:
    at: "2026-11-15T00:00:00Z"
    by: agent
status: stable
code_refs:
    - src/BackendProjectTemplate.Domain/Subscriptions/**
    - src/BackendProjectTemplate.Application/Subscriptions/**
    - src/BackendProjectTemplate.Infrastructure/Persistence/Configurations/Subscription*.cs
    - src/BackendProjectTemplate.Infrastructure/Persistence/SubscriptionLifecyclePersistence.cs
    - src/BackendProjectTemplate.Infrastructure/Persistence/SubscriptionAdministrationReadModelRepository.cs
    - src/BackendProjectTemplate.Infrastructure/Persistence/SubscriptionAnalyticsReadModelRepository.cs
    - src/BackendProjectTemplate.Infrastructure/Persistence/Interceptors/SubscriptionConflictTranslationInterceptor.cs
    - src/BackendProjectTemplate.Infrastructure/Migrations/*_ImplementAdminSubscriptionManagement.cs
    - src/BackendProjectTemplate.WebAPI/Features/Subscriptions/**
    - src/BackendProjectTemplate.Consumer/Payments/ActivateSubscriptionHandler.cs
    - src/BackendProjectTemplate.Consumer/Subscriptions/**
    - src/BackendProjectTemplate.Contracts/Commands/Subscriptions/**
    - tests/unit/BackendProjectTemplate.Domain.UnitTests/Subscriptions/**
    - tests/unit/BackendProjectTemplate.Application.UnitTests/Subscriptions/**
    - tests/integration/BackendProjectTemplate.Consumer.IntegrationTests/Payments/ActivateSubscription/**
    - tests/integration/BackendProjectTemplate.Consumer.IntegrationTests/Subscriptions/**
    - tests/unit/BackendProjectTemplate.Consumer.UnitTests/Subscriptions/**
    - tests/unit/BackendProjectTemplate.WebAPI.UnitTests/Features/Subscriptions/**
    - tests/integration/BackendProjectTemplate.WebAPI.IntegrationTests/Subscriptions/**
governance: context
---
## Boundary

`BillingAccount` owns the commercial relationship, `Subscription` owns entitlement and lifecycle,
and `PaymentTransaction` owns money movement. A subscription always carries the same tenant as its
billing account and never has a direct stakeholder owner. Tenant-owned and stakeholder-owned billing
accounts can each own multiple subscriptions.

## Plans and money

`SubscriptionPlan` codes are normalized, stable and globally unique. Prices are positive whole minor
currency units for paid purchases, matching `PaymentTransaction.Amount`; floating-point money is not
used. Monthly and yearly periods use calendar arithmetic. The post-deploy plan seed is deterministic
and idempotent. Updating a plan changes future purchases only: `SubscriptionPurchase` and
`SubscriptionPeriod` retain immutable code, amount, currency and interval snapshots.

## Purchase and fulfillment

The generic `/payments/initiate` flow accepts wallet top-ups only. Subscription purchases go through
`POST /subscriptions/purchases`, which resolves an active plan and BillingAccount, derives price and
currency server-side, and saves the pending `PaymentTransaction`, pending `Subscription`, and
`SubscriptionPurchase` correlation before invoking the provider.

Verified `SuccessfulPaymentConfirmed` events retain the existing command/outbox/Consumer flow.
`ActivateSubscriptionHandler` distinguishes a `SubscriptionPurchase` from a durably correlated
`SubscriptionRenewalAttempt`. Initial activation requires matching tenant, amount, currency and
merchant reference, then atomically creates one period, records history, fulfills the purchase and
links the compatibility `SubscriptionActivation`. Renewal fulfillment validates the same ownership
and money invariants, creates exactly one period from the agreed period boundary (not settlement
time), marks the attempt/renewal fulfilled and schedules the next renewal.

## Lifecycle semantics

New purchases begin `Pending`; a paid or explicitly administrative activation transitions them to
`Active`. Administrative allocation creates no fake payment reference. Cancellation at period end
keeps the subscription active and entitlement intact through `CurrentPeriodEndUtc`, disables renewal,
clears `NextBillingAtUtc`, records an auditable lifecycle decision, and atomically schedules a
state-checked `ExpireSubscriptionCommand`. `NextBillingAtUtc` is the next charge-attempt date, not a
substitute for entitlement end.

Initial paid activation atomically schedules `RenewSubscriptionCommand` through the transactional
outbox. A durable `SubscriptionRenewal`, uniquely identified by `(SubscriptionId,
TargetPeriodStartUtc)`, snapshots the authoritative period boundaries, price, currency and interval;
`SubscriptionRenewalAttempt` is a separate financial-attempt identity. Consumers always re-read the
subscription and treat stale renewal, reminder and expiry commands as no-ops. Renewal reminders flow
through the generic notification command/outbox path and are sequence-deduplicated on the renewal.

The Bachs adapter exposes the provider-agnostic recurring-charge boundary for NGN and USD. A renewal
attempt and normal `PaymentTransaction` are saved before the provider call, including a stable key
`renewal-{RenewalId}-{AttemptNumber}`. Duplicate commands cannot create another attempt while one is
pending, submitted or unknown. Immediate success uses the standard successful-payment event;
confirmed failure schedules bounded backoff retries; processing or transport ambiguity preserves the
same attempt/key and schedules `ReconcileSubscriptionRenewalCommand`. Reconciliation retrieves a
known charge or repeats the exact idempotent request when submission timed out. Conditional reminders
and grace expiry remain scheduled state-checked commands. Missing, pending, revoked, expired,
wrong-currency or unsupported-provider authorization fails closed as `AwaitingAuthorization`; a paid
initial subscription remains entitled even if saved-card setup never completes.

Proration, upgrades, downgrades, refunds, ownership transfers and automatic complimentary renewal remain
outside this domain version.

## One active entitlement per BillingAccount

`Subscription.EntitlementStartUtc`/`EntitlementEndUtc` form a half-open window `[start, end)`; a NULL end
is open-ended and is used only by paid subscriptions that are still auto-renewing (cancel-at-period-end
closes it to `CurrentPeriodEndUtc`). Scheduled and Active subscriptions on the same `(TenantId,
BillingAccountId)` must never overlap, so an entitlement starting exactly at another's end is valid.
PostgreSQL enforces this with the `EX_Subscriptions_BillingAccountEntitlement` GiST exclusion constraint
(`btree_gist`, `tstzrange(..., '[)')`, filtered to Active/Scheduled) — the authoritative concurrent-write
guard. `OverlappingEntitlementSpecification` mirrors it for deterministic application checks.
`SubscriptionConflictTranslationInterceptor` translates the exclusion violation, the allocation
idempotency index, plan-code uniqueness and `Subscription` row-version conflicts into domain exceptions.
Lifecycle boundaries are normalized to microsecond precision (`SubscriptionTimestamps`) because scheduled
commands compare them by exact equality against `timestamptz` values.

Customer purchase initiation rejects (409) a BillingAccount that already holds a current or scheduled
entitlement. Entitlement-changing consumers (paid fulfillment, scheduled allocation activation) save their
own changes inside the handler: when PostgreSQL rejects the write because a concurrent, previously
uncommitted entitlement won, they call `IUnitOfWork.DiscardChanges()` and persist the compensating outcome
(blocked purchase, or allocation left Scheduled) in the same delivery, so correctness never depends on
broker redelivery. The base handler then records the inbox entry in a second save; redelivery remains safe
because every handler re-checks state. A verified payment that still cannot be fulfilled (conflict, or the subscription is no longer
Pending) is never activated and never redelivered forever: the payment stays Succeeded, the purchase is
marked `FulfillmentBlocked*`, and an administrator resolves the conflict then calls the
purchase-fulfillment retry endpoint, which re-enqueues `ActivateSubscriptionCommand` through the outbox
(refunds are out of scope). A renewal charge that succeeds after the subscription was cancelled marks the
renewal `FulfillmentBlocked` instead of extending entitlement. Non-renewing paid activations schedule
`ExpireSubscriptionCommand` at period end.

## Administrative allocation

`AllocateSubscription` grants a complimentary subscription to a tenant- or stakeholder-owned
BillingAccount resolved through `IBillingAccountResolver` (which validates that a stakeholder belongs to
the target tenant and may commit a new account before the allocation transaction; it runs before any
subscription state is tracked). Allocations always have `AutoRenew = false` (a request with
`AutoRenew: true` is rejected), carry no `PaymentTransaction`, `SubscriptionPurchase`, payment reference or
renewal, and record a period whose price is a nominal plan snapshot, not money collected. The allocation
window defaults to the plan's calendar billing interval from the start; an explicit `EndAtUtc` must be
later than the start; a start more than five minutes in the past is rejected.

`SubscriptionAllocation` is the immutable audit record (business `AllocationSource`: AdminAllocation,
Promotion, Migration, Trial — distinct from the technical `SubscriptionActivationSource`), holding the
reason, the authenticated actor, the window and a plan snapshot. Its `OperationId` is the caller's
idempotency key (unique) and equals the allocation period's `OperationId`; a duplicate request returns the
original allocation, including when a concurrent duplicate loses on either database constraint. Replay
is only accepted when the stored `RequestFingerprint` (SHA-256 of the full canonical request: tenant, owner
type, stakeholder, plan, requested window as sent, source and trimmed reason, timestamps normalized to
microseconds) matches; any material difference returns 409 `IdempotencyKeyReused`.
`SubscriptionStatusHistory.Details` stores administrator-supplied justification beside the stable
machine-readable `Reason` code.

An allocation starting now is Active immediately and schedules `ExpireSubscriptionCommand` at its end. A
future allocation is `Scheduled` (reserving its window under the exclusion constraint) and schedules
`ActivateAllocatedSubscriptionCommand` at its start. The consumer re-reads subscription and allocation,
requires matching tenant, BillingAccount and window and Scheduled status (otherwise a stale no-op),
re-schedules early deliveries, activates late deliveries from the agreed start (never extending the
grant) using the allocation's plan snapshot, lapses a command delivered after the whole window ended
straight from Scheduled to Expired (`scheduled_allocation_lapsed` history, no period, never Active) — plan deactivation after approval does not block activation —
and then schedules expiry. All of these commands set `TenantId` to the subscription's tenant, never the
administrator's.

## Cancellation

Period-end cancellation (customer or administrative) keeps the subscription Active through
`CurrentPeriodEndUtc`, disables renewal and schedules state-checked expiry; repeated requests are no-ops.
Immediate administrative cancellation is valid for Active and Scheduled subscriptions: status becomes
Cancelled, `EndedAtUtc`/`CancelledAtUtc` record the effective termination while `CurrentPeriodEndUtc`
keeps the originally granted boundary, the entitlement window closes, open (non-charging) renewals are
cancelled, and periods and payments are retained. Activation, renewal, reminder and expiry consumers all
re-check state, so stale commands cannot restore entitlement. Neither mode implies a refund.

## Access

Administrative subscription analytics is exposed as five independent read-only endpoints under
`/api/v1/admin/subscriptions/analytics`: `summary`, `timeline`, `renewals`, `plans` and `statuses`.
The inclusive UTC `DateOnly` range is converted to a half-open database range; summary state is sampled
at the final instant of the selected end date and timeline state at each bucket's capped final instant.
Filters support plan, BillingAccount owner type, tenant and currency, and timeline intervals reuse the
shared `Day`/`Week`/`Month`/`Year` calendar utilities.

Active subscription snapshots require an actually persisted activation period and use the entitlement's
half-open boundaries, so later cancellation does not erase earlier activity and renewal grace remains
entitled until the lifecycle ends. New subscriptions use the first persisted activation; churn occurs at
effective entitlement end only when no replacement entitlement for the BillingAccount is active at that
instant. Cancellation-pending counts scheduled cancellation decisions that remain entitled. Past-due means
an entitled subscription whose paid period boundary passed while its renewal is unresolved or exhausted
during grace; a pending or failed attempt alone is not a terminal renewal failure.

Successful and failed renewal KPIs count distinct terminal `Fulfilled` and `Exhausted` renewals by their
completion timestamps. `FulfillmentBlocked`, pending, charging, retrying and awaiting-reconciliation states
do not enter the terminal success-rate denominator; failed payment attempts are reported separately.

MRR uses the latest immutable paid `SubscriptionPeriod` snapshot effective for the subscription at the
reporting instant. `PaidPurchase` and `PaidRenewal` are eligible regardless of `AutoRenew`; nominal
`AdministrativeAllocation` prices are excluded. Monthly price maps directly to MRR, yearly price divides by
12, and ARR is MRR multiplied by 12. Values remain decimal minor-currency units without intermediate
rounding, and revenue is always grouped by currency with no conversion. The read repository issues bounded,
no-tracking projections and never one query per timeline bucket. Endpoint-specific cache keys include every
effective filter and use short TTLs, Redis-failure fallback and keyed single-flight coordination.
Each Application query handler owns its endpoint's repository orchestration, calculations and response
mapping; only cache execution plus consistent date/filter and revenue normalization mechanics are shared,
so handlers remain complete vertical slices rather than pass-through wrappers around an analytics facade.

User-facing plan listing, purchase, current-subscription lookup and scheduled cancellation require the
active-session policy and derive tenant/stakeholder identity from the authenticated actor. Administrative
endpoints live under `api/v1/admin/subscriptions` and `api/v1/admin/subscription-plans`, target explicit
tenant/subscription ids, read the acting administrator only from the authenticated actor, and are
permission-guarded (see [[authorization]]). Listing uses `(CreatedAtUtc, Id)` cursor pagination over
projections; detail issues a fixed set of bounded queries and keeps financial history (purchase, payments,
renewals/attempts) separate from entitlement history (periods, status history, allocations). Plans are
never deleted; code, currency and billing interval are immutable after creation, while name and price may
change without affecting purchase, period, renewal or allocation snapshots. Deactivating a plan blocks new
purchases and allocations only.
