---
type: domain
title: Payments Domain
description: Payment providers (Bachs, Credo, SafeHaven), wallet and wallet transactions, webhook inbox idempotency, reconciliation, and subscription activation
resource: okf://knowledge/domains/payments
tags:
    - context
    - domain
    - payments
    - wallet
    - webhooks
governance: context
code_refs:
    - src/BackendProjectTemplate.Application/Payments/**
    - src/BackendProjectTemplate.Domain/Payments/**
    - src/BackendProjectTemplate.Infrastructure/Payments/**
    - src/BackendProjectTemplate.Infrastructure/Persistence/BillingAccountResolver.cs
    - src/BackendProjectTemplate.Infrastructure/Persistence/Configurations/BillingAccountConfiguration.cs
    - src/BackendProjectTemplate.Infrastructure/Persistence/Configurations/SubscriptionActivationConfiguration.cs
    - src/BackendProjectTemplate.Application/Subscriptions/Features/PurchaseSubscription/**
    - src/BackendProjectTemplate.WebAPI/Features/Payments/**
    - src/BackendProjectTemplate.Jobs/Payments/**
    - src/BackendProjectTemplate.Contracts/Payments/**
    - src/BackendProjectTemplate.Contracts/Commands/Payments/**
    - tests/unit/BackendProjectTemplate.Application.UnitTests/Payments/**
    - tests/integration/BackendProjectTemplate.WebAPI.IntegrationTests/Payments/**
sources:
    - kind: file
      path: src/BackendProjectTemplate.Domain/Payments/Entities/Wallet.cs
generated:
    at: "2026-09-23T00:00:00Z"
    by: human
status: stable
---
## Scope

- Payment providers: Bachs, Credo and SafeHaven, each with provider-specific payload types under
  `Infrastructure/Payments/{Bachs,Credo,SafeHaven}`. Provider activation/deactivation is a distinct feature
  (`ActivatePaymentProvider`) from initiating a payment.
- Bachs supports one-time hosted-checkout wallet top-ups in this phase. The seeded configuration enables
  NGN `WalletTopUp` only. Internal amounts are whole minor units; the adapter converts them to exact
  major-unit decimal strings and verifies merchant reference, currency and amount before success. Bachs
  webhook signatures cover the timestamp plus original raw request bytes, prefer the V2 multi-signature
  header, and enforce a five-minute default tolerance.
- Wallet: `InitiatePayment`, wallet transactions, wallet top-up transaction detail, `CreditWallet` (triggered async, see below).
- Billing ownership: `BillingAccount` is the tenant-scoped commercial ownership aggregate. Each tenant owner and each tenant/stakeholder owner pair has one retained account, enforced by PostgreSQL partial unique indexes. Stakeholder ownership also uses a composite `(TenantId, StakeholderId)` foreign key so cross-tenant associations fail at the database boundary. Ownership fields are immutable; display names are labels, not identifiers.
- `IBillingAccountResolver.ResolveOrCreateAsync` validates non-deleted owners, derives display names from verified tenant/stakeholder records, returns an existing account idempotently, and recovers only from the two known owner-uniqueness conflicts after detaching the losing insert. Billing-account uniqueness is intentionally not filtered by soft deletion, so historical commercial identity cannot be silently replaced.
- Webhook inbox: incoming provider webhooks (`ProcessBachsWebhook`, `ProcessCredoWebhook`,
  `ProcessSafeHavenWebhook`, generic `ProcessPaymentWebhook`) are recorded in a `PaymentWebhookInbox` for idempotency before being
  acted on — do not process a webhook payload without first checking/recording it there.
- `ReconcilePayments` (Jobs) periodically reconciles payment state against provider truth.
- `SuccessfulPaymentConfirmed`, `ActivateSubscriptionCommand`, `CreditWalletCommand` cross into `Consumer`/`Jobs` via `Contracts` and the outbox — see [[async-processing]].
- The generic payment-initiation endpoint is wallet-top-up only. Subscription checkout goes through the dedicated [[subscriptions]] purchase slice so plan price/currency are server-authoritative and a durable purchase correlation exists before provider initiation.
- `Subscription` is the entitlement/lifecycle source of truth. `SubscriptionActivation` remains an immutable compatibility fulfillment record and links new fulfillments to the subscription and period; its new linkage columns are nullable for historical rows and no historical plan data is manufactured.

Follows the standard [[vertical-slices]] feature-folder shape and the [[unit-testing]]/[[integration-testing]] conventions. Distinct from the generic [[providers]] domain (email/file-storage provider registry).
