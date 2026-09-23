---
type: domain
title: Payments Domain
description: Payment providers (Credo, SafeHaven), wallet and wallet transactions, webhook inbox idempotency, reconciliation, and subscription activation
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

- Payment providers: Credo and SafeHaven, each with their own webhook payload types under `Infrastructure/Payments/{Credo,SafeHaven}`. Provider activation/deactivation is a distinct feature
  (`ActivatePaymentProvider`) from initiating a payment.
- Wallet: `InitiatePayment`, wallet transactions, wallet top-up transaction detail, `CreditWallet` (triggered async, see below).
- Webhook inbox: incoming provider webhooks (`ProcessCredoWebhook`, `ProcessSafeHavenWebhook`, generic `ProcessPaymentWebhook`) are recorded in a `PaymentWebhookInbox` for idempotency before being
  acted on — do not process a webhook payload without first checking/recording it there.
- `ReconcilePayments` (Jobs) periodically reconciles payment state against provider truth.
- `SuccessfulPaymentConfirmed`, `ActivateSubscriptionCommand`, `CreditWalletCommand` cross into `Consumer`/`Jobs` via `Contracts` and the outbox — see [[async-processing]].

Follows the standard [[vertical-slices]] feature-folder shape and the [[unit-testing]]/[[integration-testing]] conventions. Distinct from the generic [[providers]] domain (email/file-storage provider registry).
