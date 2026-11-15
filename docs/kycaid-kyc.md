# KYCAID KYC integration

The backend owns KYC orchestration and state; KYCAID performs identity, document, facial and screening
checks. The Domain and Application layers contain no KYCAID DTOs or HTTP details.

## API

- `POST /api/v1/stakeholders/me/kyc/start` starts or resumes the authenticated stakeholder's active attempt.
- `GET /api/v1/stakeholders/me/kyc` returns local current status and a resumable URL when one is valid.
- `POST /api/v1/webhooks/kycaid` authenticates and durably accepts KYCAID callbacks.

Start and status require the active-session authorization policy. The webhook is anonymous at the HTTP
authentication layer and authenticates KYCAID with `x-data-integrity`.

## Configuration

Set these values through secure deployment configuration (never commit the API key):

```text
Kyc__Kycaid__Enabled=true
Kyc__Kycaid__BaseUrl=https://api.kycaid.com
Kyc__Kycaid__ApiKey=<secret>
Kyc__Kycaid__FormId=<dashboard form id>
Kyc__Kycaid__RedirectUrl=https://app.example.com/kyc/complete
Kyc__Kycaid__RequestTimeoutSeconds=15
Kyc__Kycaid__TrustedFormHosts__0=forms.kycaid.com
```

Add documented custom form domains to `TrustedFormHosts`; untrusted or non-HTTPS returned URLs are rejected.
Options are validated at startup when the integration is enabled.

## KYCAID Dashboard

Create one reusable form, configure the public callback URL
`https://<api-host>/api/v1/webhooks/kycaid`, and enable verification-created/status and verification-result
callbacks. Keep the Form ID in configuration. The backend requests one-time URLs using that form and sends
the stakeholder's stable internal ID as `external_applicant_id`. Once a callback supplies `applicant_id`,
later attempts reuse it to preserve KYCAID applicant history.

## Lifecycle and security

An attempt starts as Pending. KYCAID `pending` moves it to InProgress. `completed` plus `verified: true`
marks the attempt and stakeholder verified; `verified: false` rejects it. Completion without a decision is
not approval. `FORM_URL_EXPIRED` expires only the form URL: an unsubmitted attempt can request a new form,
and a submitted attempt keeps waiting for its decision. Rejected, failed and expired attempts may restart;
verified stakeholders may not.

A form request is recorded before KYCAID is called. If KYCAID rejects it (4xx other than 408, or the
integration is disabled) the request can be retried at once. If the outcome is unknown (timeout, network
error, 5xx), no new form is requested for 10 minutes so a slow response cannot produce a duplicate form.

The Jobs host reconciles active attempts whose callbacks were missed by polling KYCAID's verification
endpoint (`Jobs:Kyc:Reconciliation` — `BatchSize`, `PollInterval`, `StaleThreshold`, `RecheckDelay`). The
Jobs host therefore needs the same `Kyc__Kycaid__*` configuration as the WebAPI.

After signature verification, WebAPI stores the provider-independent callback fields and a
`KycWebhookReceived` outbox event atomically, then responds. The Consumer applies verification state and
stakeholder changes and records the shared message-inbox entry in one transaction. Therefore a `200`
response means the callback was durably queued, not that downstream processing has already completed.

The database preserves attempt history, enforces one active attempt per tenant/stakeholder, uniquely indexes
provider verification references, and deduplicates callback request IDs. Signatures use lowercase-hex
HMAC-SHA512(API key, Base64(raw request bytes)) with constant-time comparison. Callback payloads and form
URLs are not logged.

## Local testing

Keep `Enabled=false` unless a non-production KYCAID account is configured. Automated tests use fakes and do
not call KYCAID. Run the repository verification sequence from `AGENTS.md` after changes.

## Recovery and reconciliation

KYCAID retries unacknowledged callbacks. Duplicate and out-of-order deliveries are safe. A reserved attempt
left without a session URL after a provider or persistence interruption is retried by the next start request.
The provider result can be reconciled through `GET /verifications/{verification_id}` once the verification
reference has been durably stored. No background polling is enabled by default; add bounded polling only if
operational evidence shows callbacks are insufficient.

References: [form integration](https://docs.kycaid.com/guides/form-integration),
[retrieve form URL](https://docs.kycaid.com/objects/forms/retrieve-form-url),
[get verification](https://docs.kycaid.com/objects/verifications/verification-get), and
[callback integrity](https://docs.kycaid.com/callbacks/callback-integrity-verification).
