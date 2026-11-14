# Bachs Payments

The Bachs integration supports one-time hosted checkout sessions for wallet top-ups. It uses the existing payment initiation endpoint, payment transaction lifecycle, webhook inbox, reconciliation job, transactional outbox, and wallet-credit consumer.

## Configuration

Use the sandbox while developing:

```text
Payments__Bachs__BaseUrl=https://sandbox-api.bachs.io
Payments__Bachs__ApiKey=sk_sandbox_...
Payments__Bachs__WebhookSecret=whsec_...
Payments__Bachs__SuccessUrl=https://your-public-client.example/payments/success
Payments__Bachs__CancelUrl=https://your-public-client.example/payments/cancel
Payments__Bachs__CheckoutExpirationMinutes=60
Payments__Bachs__WebhookTimestampToleranceSeconds=300
```

Never commit the API key or webhook secret. Production uses `https://api.bachs.io` with an `sk_live_...` key. The API key needs checkout creation and payment-read permissions because reconciliation retrieves checkout sessions.

`SuccessUrl` and `CancelUrl` are optional Bachs checkout fields. When supplied, they must be public HTTP(S) URLs; Bachs rejects localhost and private-network redirect URLs, including in sandbox.

## Provider activation

The post-deploy template seeder creates the `bachs` payment provider and enables `WalletTopUp` with `PaymentLink` for NGN. The code also performs exact two-decimal conversion for USD, but USD is not enabled for Bachs by the seed data. Activate additional provider/currency/intent combinations only after confirming the Bachs account has the corresponding payment corridor.

Internal payment amounts are whole minor units. Bachs receives decimal strings in major units, so an internal NGN amount of `2500` is sent as `"25.00"`. Verification converts the provider amount back to minor units and requires an exact merchant-reference, currency, and amount match.

## Webhook registration

Register this public HTTPS endpoint in the Bachs Developer Portal:

```text
POST /api/v1/payments/webhooks/bachs
```

Subscribe to:

- `collection.succeeded`
- `collection.failed`
- `checkout.expired`

The endpoint validates `X-Bachs-Signature-V2` against the original request bytes using HMAC-SHA256, accepts any matching `v1` signature during secret rotation, and rejects timestamps outside the configured tolerance. The legacy `X-Bachs-Signature` plus `X-Bachs-Timestamp` pair is also accepted.

Successful collection events are verified by retrieving the checkout from Bachs before the payment is confirmed. A failed collection leaves an open checkout in processing state because Bachs permits another attempt; expiry or cancellation is terminal. Confirmed payments publish `SuccessfulPaymentConfirmed` through the transactional outbox. The webhook never credits a wallet directly.

## Local verification

Automated tests use mocked Bachs HTTP responses and signed local webhook payloads; they do not call live Bachs services.

```text
dotnet test BackendProjectTemplate.slnx --no-build --filter Bachs
```

For manual sandbox testing, expose the webhook endpoint over public HTTPS (or use Bachs' documented local webhook forwarding), create a wallet top-up through the existing payment initiation endpoint with the seeded Bachs provider ID, complete the hosted checkout, and confirm the inbox, payment transaction, outbox, and wallet transaction are each recorded once.

## Deliberately unsupported

This phase does not support subscriptions, Connect accounts, payouts, refunds, direct charges, saved payment methods, free checkouts, or crypto checkout flows.
