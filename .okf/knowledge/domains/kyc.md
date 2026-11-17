---
type: domain
title: KYC Verification Domain
description: Provider-independent KYC attempt lifecycle, KYCAID hosted-form integration, authenticated callbacks and tenant isolation
resource: okf://knowledge/domains/kyc
tags:
    - context
    - domain
    - kyc
    - kycaid
    - webhooks
governance: constraint
code_refs:
    - src/BackendProjectTemplate.Domain/Kyc/**
    - src/BackendProjectTemplate.Application/Kyc/**
    - src/BackendProjectTemplate.Infrastructure/Kyc/**
    - src/BackendProjectTemplate.WebAPI/Features/Kyc/**
    - src/BackendProjectTemplate.Contracts/Events/KycWebhookReceived.cs
    - src/BackendProjectTemplate.Contracts/Events/StakeholderKycVerified.cs
    - src/BackendProjectTemplate.Consumer/Kyc/**
    - src/BackendProjectTemplate.Jobs/Kyc/**
    - tests/unit/BackendProjectTemplate.Domain.UnitTests/Kyc/**
    - tests/unit/BackendProjectTemplate.Application.UnitTests/Kyc/**
    - tests/unit/BackendProjectTemplate.Consumer.UnitTests/Kyc/**
    - tests/unit/BackendProjectTemplate.Infrastructure.UnitTests/Kyc/**
    - tests/unit/BackendProjectTemplate.Jobs.UnitTests/Kyc/**
sources:
    - kind: file
      path: src/BackendProjectTemplate.Domain/Kyc/Entities/KycVerification.cs
generated:
    at: "2026-11-16T00:00:00Z"
    by: agent
status: stable
---
## Boundaries

`KycVerification` is the tenant-scoped history of verification attempts. Domain and Application depend
only on `IKycProviderService`; KYCAID HTTP paths, DTOs, API-key authentication and form-host validation
remain in Infrastructure. A partial unique PostgreSQL index permits only one Pending/InProgress attempt
per tenant and stakeholder.

## Approval rule

Form completion or a pending callback never verifies a stakeholder. Infrastructure normalizes KYCAID status
and decision fields to `KycVerificationTransition`; the aggregate only understands provider-independent
`NoChange`, `InProgress`, `Verified` and `Rejected` transitions. Only KYCAID `completed` with `verified: true`
normalizes to `Verified`; `completed` with no decision and unknown statuses normalize to `NoChange`. Provider
results are applied only to active (Pending/InProgress) attempts, so late callbacks never change Verified,
Rejected, Failed or Expired attempts.

## Callbacks

The public KYCAID endpoint verifies `x-data-integrity` over the original request bytes before parsing.
It persists the callback in `KycWebhookInboxes` and appends `KycWebhookReceived` to the transactional outbox
in the same transaction; `200 OK` means durably accepted. The Consumer handler performs domain processing
and uses the shared message inbox for redelivery safety. Provider callback request IDs are unique
(`UX_KycWebhookInboxes_ProviderRequest`); a concurrent duplicate surfaces as `KycWebhookDuplicateException` and is
acknowledged as a duplicate. Callbacks resolve an attempt only by provider verification or session reference —
never by external applicant ID, which is shared by every attempt of a stakeholder — and applicant references
must correlate. A verification reference first seen in a callback is recorded on the attempt. Every receipt
starts with `KycWebhookProcessingOutcome.Pending`; Consumer processing records StateTransitioned,
NoStateTransition, UnknownVerification, ReferenceMismatch or TerminalVerificationIgnored with a TimeProvider
timestamp. Permanent correlation failures are acknowledged and audited rather than endlessly retried, while
infrastructure exceptions still escape for retry.

The first transition to Verified marks the stakeholder and appends `StakeholderKycVerified` to the transactional
outbox in the same unit of work. `VerifiedEventPublishedAtUtc` is the durable logical-publication guard. The
verification PostgreSQL row version makes concurrent webhook and reconciliation attempts conflict, so only one
transaction can commit the transition, guard and event.

## Reconciliation

`Jobs/Kyc/KycReconciliationProcessor` periodically runs `KycReconciliationService`, which fetches the provider
result (`IKycProviderService.GetVerificationAsync`) for stale active attempts that have a verification reference.
The provider client returns the same normalized transition used by callbacks, and reconciliation uses the same
one-time verified-event guard. Every checked attempt records `LastReconciledAtUtc`, including provider failures,
so the recheck delay backs off unreachable attempts.

## Start and retry policy

Start reuses an active attempt and its URL, and returns an InProgress (submitted) attempt without requesting a
new form. Rejected, Failed or Expired attempts may create a new attempt, reusing a known provider applicant
reference. Already-verified stakeholders cannot restart.

Provider session creation is guarded by `SessionRequestedAtUtc`, committed before the provider call. If the call
has an unknown outcome (timeout, transport error, 5xx, untrusted URL) the request stays pending for
`KycVerification.SessionProvisioningLease`, during which no new form is requested; after it, a new start may
request one. `KycProviderRequestRejectedException` (integration disabled, or a non-408 4xx) means no form was
created, so the request is released for immediate retry. Provider failures never fail the attempt.

KYCAID does not return form expiry from its URL endpoint; `FORM_URL_EXPIRED` is authoritative for URL expiry
and expires only the URL, never the attempt. For an unsubmitted attempt it clears the URL and session so a new
form can be requested; for a submitted (InProgress) attempt it keeps the provider references so the pending
decision still correlates.

See [[stakeholders]], [[providers]], [[persistence]], [[vertical-slices]] and [[integration-testing]].
