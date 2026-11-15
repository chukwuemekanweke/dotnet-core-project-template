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
    - src/BackendProjectTemplate.Consumer/Kyc/**
    - tests/unit/BackendProjectTemplate.Domain.UnitTests/Kyc/**
    - tests/unit/BackendProjectTemplate.Application.UnitTests/Kyc/**
    - tests/unit/BackendProjectTemplate.Consumer.UnitTests/Kyc/**
    - tests/unit/BackendProjectTemplate.Infrastructure.UnitTests/Kyc/**
sources:
    - kind: file
      path: src/BackendProjectTemplate.Domain/Kyc/Entities/KycVerification.cs
generated:
    at: "2026-11-15T00:00:00Z"
    by: agent
status: stable
---
## Boundaries

`KycVerification` is the tenant-scoped history of verification attempts. Domain and Application depend
only on `IKycProviderService`; KYCAID HTTP paths, DTOs, API-key authentication and form-host validation
remain in Infrastructure. A partial unique PostgreSQL index permits only one Pending/InProgress attempt
per tenant and stakeholder.

## Approval rule

Form completion or a pending callback never verifies a stakeholder. Only an authenticated provider result
with provider status `completed` and `verified: true` transitions the attempt to `Verified` and calls
`Stakeholder.MarkVerified()`. Terminal Verified/Rejected attempts are not downgraded by late callbacks.

## Callbacks

The public KYCAID endpoint verifies `x-data-integrity` over the original request bytes before parsing.
It persists the callback in `KycWebhookInboxes` and appends `KycWebhookReceived` to the transactional outbox
in the same transaction; `200 OK` means durably accepted. The Consumer handler performs domain processing
and uses the shared message inbox for redelivery safety. Provider callback request IDs remain unique, and
verification, applicant and external-applicant references must correlate with the persisted attempt.

## Start and retry policy

Start reuses an active attempt and its URL. Rejected, Failed or Expired attempts may create a new attempt,
reusing a known provider applicant reference. Already-verified stakeholders cannot restart. KYCAID does not
return form expiry from its URL endpoint; `FORM_URL_EXPIRED` is authoritative for URL expiry.

See [[stakeholders]], [[providers]], [[persistence]], [[vertical-slices]] and [[integration-testing]].
