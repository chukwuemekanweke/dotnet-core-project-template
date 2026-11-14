---
type: domain
title: Subscriptions Domain
description: Provider-agnostic subscription plans, BillingAccount ownership, purchase correlation, lifecycle periods, status history, and cancellation semantics
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
    at: "2026-11-14T00:00:00Z"
    by: agent
status: stable
code_refs:
    - src/BackendProjectTemplate.Domain/Subscriptions/**
    - src/BackendProjectTemplate.Application/Subscriptions/**
    - src/BackendProjectTemplate.Infrastructure/Persistence/Configurations/Subscription*.cs
    - src/BackendProjectTemplate.Infrastructure/Persistence/SubscriptionLifecyclePersistence.cs
    - src/BackendProjectTemplate.WebAPI/Features/Subscriptions/**
    - src/BackendProjectTemplate.Consumer/Payments/ActivateSubscriptionHandler.cs
    - tests/unit/BackendProjectTemplate.Domain.UnitTests/Subscriptions/**
    - tests/unit/BackendProjectTemplate.Application.UnitTests/Subscriptions/**
    - tests/integration/BackendProjectTemplate.Consumer.IntegrationTests/Payments/ActivateSubscription/**
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
`ActivateSubscriptionHandler` requires a succeeded subscription payment and matching purchase tenant,
amount, currency and merchant reference. Initial activation atomically creates one period, records
history, fulfills the purchase, links the compatibility `SubscriptionActivation`, and relies on
payment/operation uniqueness plus PostgreSQL concurrency for redelivery safety.

## Lifecycle semantics

New purchases begin `Pending`; a paid or explicitly administrative activation transitions them to
`Active`. Administrative allocation creates no fake payment reference. Cancellation at period end
keeps the subscription active and entitlement intact through `CurrentPeriodEndUtc`, disables renewal,
clears `NextBillingAtUtc`, and records an auditable lifecycle decision. `NextBillingAtUtc` is the next
charge-attempt date, not a substitute for entitlement end. Automatic charging, retries, dunning,
proration, upgrades and downgrades remain outside this domain version.

User-facing plan listing, purchase, current-subscription lookup and scheduled cancellation require the
active-session policy and derive tenant/stakeholder identity from the authenticated actor. Static admin
permissions already include `subscriptions.read`, `subscriptions.manage`, and `subscriptions.allocate`;
no administrative allocation endpoint is introduced here.
