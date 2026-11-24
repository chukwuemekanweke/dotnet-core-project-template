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
    - src/BackendProjectTemplate.Infrastructure/Persistence/PaymentAdministrationReadModelRepository.cs
    - src/BackendProjectTemplate.Infrastructure/Persistence/PaymentAnalyticsReadModelRepository.cs
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
- Bachs supports hosted-checkout wallet top-ups and trusted subscription purchases. The generic
  payment endpoint remains wallet-top-up only; only the server-authoritative subscription purchase
  slice sets `IsTrustedSubscriptionPurchase`. Auto-renew purchases create a Bachs customer, send
  `save_payment_method: true`, and persist a pending authorization tied to the BillingAccount and
  source PaymentTransaction. Internal amounts are whole minor units; the adapter converts them to
  exact major-unit decimal strings and verifies merchant reference, currency and amount before success.
  Bachs webhook signatures cover the timestamp plus original raw request bytes, prefer the V2
  multi-signature header, and enforce a five-minute default tolerance.
- Bachs webhooks are only triggers: every supported event (`collection.succeeded|failed|underpaid`,
  `checkout.completed|expired`, plus `payment_method.saved`) re-verifies the checkout through
  `GET /v1/checkout-sessions/{id}`; others are recorded and ignored. Webhook bodies use different status
  vocabularies (e.g. `paid`, uppercase charge statuses) from the API object, so never derive state from them.
- Underpayment (Bachs documents it for crypto) stays payable while the checkout is open, so it is settled only
  when the checkout is `expired`/`cancelled` with an `underpaid` charge whose `amount_paid` is a true partial
  amount. The transaction becomes terminal `PaymentStatus.Underpaid` with `AmountPaid`.
  `UnderpaidPaymentSettlement` (shared by the webhook handler and reconciliation) credits wallet top-ups with
  the collected amount and never fulfils subscriptions: those stay `Underpaid` for administrators (admin
  payments filter/detail) to refund or settle with the provider by hand.
- Wallet: `InitiatePayment`, wallet transactions, wallet top-up transaction detail, `CreditWallet` (triggered async, see below).
- Billing ownership: `BillingAccount` is the tenant-scoped commercial ownership aggregate. Each tenant
  owner and each tenant/stakeholder owner pair has one retained account, enforced by PostgreSQL partial
  unique indexes. Stakeholder ownership also uses a composite `(TenantId, StakeholderId)` foreign key
  so cross-tenant associations fail at the database boundary. Ownership fields are immutable; display
  names are labels, not identifiers.
- `IBillingAccountResolver.ResolveOrCreateAsync` validates non-deleted owners, derives display names
  from verified tenant/stakeholder records, returns an existing account idempotently, and recovers only
  from the two known owner-uniqueness conflicts after detaching the losing insert. Billing-account
  uniqueness is intentionally not filtered by soft deletion, so historical commercial identity cannot
  be silently replaced.
- Webhook inbox: incoming provider webhooks (`ProcessBachsWebhook`, `ProcessCredoWebhook`,
  `ProcessSafeHavenWebhook`, generic `ProcessPaymentWebhook`) are recorded in a `PaymentWebhookInbox` for idempotency before being
  acted on — do not process a webhook payload without first checking/recording it there.
- `ReconcilePayments` (Jobs) periodically reconciles payment state against provider truth. Bachs
  renewal transactions carry a durable `renewal_attempt_id` marker so verification uses
  `/v1/payments/{payment_id}` instead of the hosted-checkout lookup.
- Administrative payment reporting is read-only and cross-tenant under `payments.read`: `GET
  /api/v1/admin/payments` performs SQL-side filtering, safe reference search and deterministic
  `(CreatedAtUtc, Id)` keyset pagination; `GET /api/v1/admin/payments/{id}` projects provider/currency/
  stakeholder details, provider-isolated bounded webhook activity, the latest reconciliation check,
  and persisted wallet-credit, subscription-purchase or subscription-renewal fulfillment evidence.
  Reporting uses `AppReadDbContext` through `IPaymentAdministrationReadModelRepository`; it never
  returns raw webhook payloads, provider metadata, processing exceptions or provider failure text.
  Payment ordering uses a filtered `(CreatedAtUtc, Id)` index; direct payment filters and the
  provider-plus-reference webhook correlation paths have dedicated indexes.
- Administrative payment analytics exposes independent summary, timeline, status, provider and intent
  endpoints under `/api/v1/admin/payments/analytics` using `payments.analytics.read`. The bounded,
  inclusive UTC `DateOnly` range is translated to an index-friendly half-open `CreatedAtUtc` predicate.
  `IPaymentAnalyticsReadModelRepository` performs conditional counts/sums and grouping in PostgreSQL;
  Application fills bounded missing timeline buckets. Monetary results always remain grouped by
  currency. Success rate uses only terminal outcomes: succeeded is successful, while failed, cancelled,
  expired and underpaid are unsuccessful; pending/initiated payments are processing. Underpaid counts are
  exposed explicitly as a subset of unsuccessful counts, and underpaid volume uses the collected
  `AmountPaid` while attempted volume continues to use the requested `Amount`. Endpoint-specific
  `IJsonCache` entries include every
  effective filter and tenant/global scope, use short configurable TTLs, fall back to PostgreSQL on
  Redis failure, and use a reference-counted per-process keyed lock for identical misses. Lock entries
  remain registered while held or awaited and are removed by the final releaser, preventing parallel
  gates for one cache key without retaining idle entries.
  Calendar bucket boundaries and aggregate percentage/average calculations reuse the shared
  `Domain.Common.Analytics` utilities and common `Interval` enum.
- Payment status-transition history and reconciliation-attempt history are not currently persisted.
  Administrative detail therefore returns the current status plus its latest safe reason and
  `LastStatusCheckAtUtc`; it must not infer or manufacture historical transitions or reconciliation outcomes.
- `SuccessfulPaymentConfirmed`, `UnsuccessfulPaymentConcluded`, `ActivateSubscriptionCommand`, `CreditWalletCommand` cross into `Consumer`/`Jobs` via `Contracts` and the outbox — see [[async-processing]].
  Every path that moves a subscription-intent payment to a terminal non-success state (webhook and reconciliation
  Failed/Expired/Underpaid, and failed provider initiation) must call `UnsuccessfulPaymentNotification.PublishAsync`,
  or the Pending subscription is never released (see [[subscriptions]]).
- When `PaymentInitiationService` was given a correlation callback, the payment is committed *before* the provider
  call. If that call throws (HTTP error, timeout, cancellation), it moves the payment `PendingInitiation → Failed`
  (`MarkInitiationFailed`, reason `provider_initiation_failed`), publishes the unsuccessful-payment event and saves
  with `CancellationToken.None`, then rethrows. The customer never got a checkout, so the payment can never succeed.
  Without a correlation callback (wallet top-up) nothing is committed before the provider call, so there is nothing to undo.
  Reconciliation only scans `Initiated` payments, so nothing else ever concludes a `PendingInitiation` payment.
- The generic payment-initiation endpoint is wallet-top-up only. Subscription checkout goes through
  the dedicated [[subscriptions]] purchase slice so plan price/currency are server-authoritative and a
  durable purchase correlation exists before provider initiation.
- Authenticated subscription provider discovery reads the existing active `PaymentProvider` and enabled
  `PaymentProviderConfiguration` records for `PaymentIntent.Subscription`, the selected plan currency and the
  actor's active country/currency mapping. It exposes provider id/name, initial payment method and whether a
  registered `IRecurringPaymentProvider` exists; it never returns provider credentials or configuration.
  A provider is only offered for subscriptions if the post-deploy seed has a `PaymentIntent.Subscription`
  configuration row for it; Bachs (the only recurring provider) is seeded for NGN subscriptions as `PaymentLink`.
- Customer purchase responses preserve provider-neutral structured instructions. Hosted Bachs/Credo checkout
  uses `paymentMethodType: PaymentLink` with `instructionFields.paymentLink`; SafeHaven bank transfer uses
  `accountNumber`, `bankName`, `accountName` and `providerReference`. Frontends never construct provider URLs
  and always query authoritative purchase status after returning from a provider.
- `Subscription` is the entitlement/lifecycle source of truth. `SubscriptionActivation` remains an
  immutable compatibility fulfillment record and links new fulfillments to the subscription and
  period; its new linkage columns are nullable for historical rows and no historical plan data is
  manufactured.
- `RecurringPaymentAuthorization` is provider-agnostic and BillingAccount-, provider-, currency-,
  consent-, customer- and source-payment-scoped. It begins pending and becomes active only after a
  signature-verified Bachs `payment_method.saved` webhook. The charge-capable `pm_` reference is
  protected at rest with ASP.NET Data Protection; PAN and CVV are never stored or returned. Selection
  requires matching tenant, BillingAccount, currency, active status and expiry. Bachs is the sole
  registered `IRecurringPaymentProvider`; it supports NGN and USD `POST /v1/charges` requests with an
  explicit `cust_` customer, `pm_` payment method and stable idempotency key. Credo and SafeHaven fail
  closed for recurring charges.

Follows the standard [[vertical-slices]] feature-folder shape and the [[unit-testing]]/[[integration-testing]] conventions. Distinct from the generic [[providers]] domain (email/file-storage provider registry).
