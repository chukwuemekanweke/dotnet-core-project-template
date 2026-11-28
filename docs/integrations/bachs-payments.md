# Bachs Payments

The Bachs integration supports hosted checkout sessions for wallet top-ups and trusted subscription purchases,
plus saved payment methods for subscription renewal. It uses the existing payment transaction lifecycle,
webhook inbox, reconciliation job and transactional outbox.

## Configuration

Use the sandbox while developing:

```text
Payments__Bachs__BaseUrl=https://sandbox-api.bachs.io
Payments__Bachs__ApiKey=sk_sandbox_...
Payments__Bachs__WebhookSecret=whsec_...
Payments__Bachs__SuccessUrl=https://your-public-client.example/payments/success
Payments__Bachs__CancelUrl=https://your-public-client.example/payments/cancel
Payments__Bachs__ReturnUrls__WalletTopUp__SuccessUrl=https://your-public-client.example/wallet/success
Payments__Bachs__ReturnUrls__WalletTopUp__CancelUrl=https://your-public-client.example/wallet/cancelled
Payments__Bachs__ReturnUrls__SubscriptionPurchase__SuccessUrl=https://your-public-client.example/billing/checkout/success
Payments__Bachs__ReturnUrls__SubscriptionPurchase__CancelUrl=https://your-public-client.example/billing/checkout/cancelled
Payments__Bachs__CheckoutExpirationMinutes=60
Payments__Bachs__WebhookTimestampToleranceSeconds=300
```

Never commit the API key or webhook secret. Production uses `https://api.bachs.io` with an `sk_live_...` key. The API key needs checkout creation and payment-read permissions because reconciliation retrieves checkout sessions.

`ReturnUrls` separates wallet and subscription browser journeys. The legacy top-level `SuccessUrl` and
`CancelUrl` remain fallbacks for compatibility. When supplied, URLs must be absolute public HTTP(S) URLs;
Bachs rejects localhost and private-network redirect URLs, including in sandbox.

For subscription checkout, the backend appends the persisted `purchaseId` to both configured URLs. Existing
query parameters are preserved. The frontend may use this value only to call the owner-scoped purchase-status
endpoint; it is never payment evidence. Do not preconfigure a `purchaseId` parameter. Bachs may append its own
checkout identifier independently.

## Provider activation

The post-deploy template seeder creates the `bachs` payment provider and enables `WalletTopUp` and trusted
`Subscription` checkout with `PaymentLink` for the configured currencies. Activate additional
provider/currency/intent combinations only after confirming the Bachs account has the corresponding corridor.

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

For manual sandbox testing, expose the webhook endpoint over public HTTPS (or use Bachs' documented local
webhook forwarding), initiate either a wallet top-up or subscription purchase, complete the hosted checkout,
and confirm the inbox, payment transaction, outbox, and resulting wallet credit or subscription activation are
each recorded once.

## Deliberately unsupported

This phase does not support Connect accounts, payouts, refunds, free checkouts, or customer-initiated direct
charges. Subscription renewal uses the provider-neutral recurring-charge boundary and a consented saved method.
