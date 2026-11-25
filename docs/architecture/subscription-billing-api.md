# Subscription and Billing Frontend API

This API keeps plan pricing, payment eligibility, payment outcome, and subscription entitlement authoritative on the server. Amounts are whole minor currency units. A provider redirect is never proof of payment; clients must query purchase status.

All routes use `X-Tenant-Id`. Routes marked authenticated also require an active-session bearer token. `BillingAccountOwnerType` is `Tenant` or `Stakeholder`; ownership is derived from the authenticated actor and is never accepted as an arbitrary account id.

Administrative subscription KPIs, timelines and breakdowns are documented in [subscription-analytics.md](subscription-analytics.md).

## Contracts

### Public plans

`GET /api/v1/subscription-plans` requires no bearer token. It returns active plans only:

```json
[
  {
    "id": "019...",
    "code": "PRO_MONTHLY",
    "name": "Pro Monthly",
    "price": 750000,
    "currencyId": "019...",
    "currencyCode": "NGN",
    "billingInterval": "Monthly"
  }
]
```

Inactive plans and administrative metadata are not returned.

### Eligible payment providers

`GET /api/v1/subscription-plans/{planId}/payment-providers?BillingAccountOwnerType=Stakeholder` is authenticated. It returns active providers whose enabled subscription configuration matches the selected active plan's currency and the actor's country. `supportsRecurringPayments` describes recurring-charge capability separately from initial-checkout eligibility.

```json
[
  {
    "id": "019...",
    "name": "Bachs",
    "paymentMethodType": "PaymentLink",
    "supportsRecurringPayments": true
  }
]
```

An unknown or inactive plan returns `404`; invalid owner input returns `400`; missing authentication returns `401`.

### Purchase

`POST /api/v1/subscriptions/purchases` is authenticated and requires a stable UUID in the `Idempotency-Key` header.

```json
{
  "planId": "019...",
  "paymentProviderId": "019...",
  "billingAccountOwnerType": "Stakeholder",
  "autoRenew": true
}
```

The server resolves the plan's price and currency. Repeating the same request and key returns the original purchase; reusing the key for a different payload returns `409`. A conflicting current/reserved entitlement also returns `409`. Invalid input, inactive plans, and ineligible providers return `400` through the standard validation/error contract.

```json
{
  "subscriptionId": "019...",
  "purchaseId": "019...",
  "merchantReference": "pay_019...",
  "paymentStatus": "Initiated",
  "paymentProviderId": "019...",
  "paymentProviderName": "Bachs",
  "expiresAtUtc": "2026-11-23T13:00:00Z",
  "paymentMethodType": "PaymentLink",
  "instructionFields": {
    "paymentLink": "https://provider.example/checkout/...",
    "providerReference": "checkout_...",
    "merchantReference": "pay_019..."
  }
}
```

For `PaymentLink`, navigate only to `instructionFields.paymentLink`. For `BankTransfer`, render `accountNumber`, `bankName`, `accountName`, and `providerReference`. Do not construct provider URLs. `Card` identifies a provider-managed card flow when returned by a configured adapter.

### Purchase status

`GET /api/v1/subscriptions/purchases/{purchaseId}?BillingAccountOwnerType=Stakeholder` is authenticated and owner-scoped. A purchase outside the selected billing context returns `404`.

The response includes raw `paymentStatus` and `subscriptionStatus`, plus a presentation-safe normalized `status`, `fulfillmentStatus`, `isTerminal`, and `shouldContinuePolling`. Normalized statuses are:

- transient: `PurchaseInitiated`, `PaymentPending`, `PaymentSucceededFulfillmentPending`;
- successful entitlement: `SubscriptionActive` or `SubscriptionScheduled`;
- payment terminal: `PaymentFailed`, `PaymentCancelled`, `PaymentExpired`, `PaymentUnderpaid`;
- subscription terminal: `SubscriptionCancelled`, `SubscriptionExpired`;
- intervention: `FulfillmentBlocked`.

```json
{
  "purchaseId": "019...",
  "subscriptionId": "019...",
  "merchantReference": "pay_019...",
  "status": "PaymentSucceededFulfillmentPending",
  "paymentStatus": "Succeeded",
  "subscriptionStatus": "Pending",
  "fulfillmentStatus": "Pending",
  "isTerminal": false,
  "shouldContinuePolling": true,
  "amount": 750000,
  "currencyCode": "NGN",
  "paymentProviderName": "Bachs",
  "paymentMethodType": "PaymentLink",
  "paymentExpiresAtUtc": null,
  "paymentCompletedAtUtc": "2026-11-23T12:05:00Z",
  "fulfilledAtUtc": null,
  "fulfillmentBlockedAtUtc": null,
  "fulfillmentBlockedReason": null,
  "currentPeriodEndUtc": null
}
```

Poll only while `shouldContinuePolling` is true and impose a client-side time/count bound. The backend may activate asynchronously after a successful payment.

### Current subscription and cancellation

`GET /api/v1/subscriptions/current?BillingAccountOwnerType=Stakeholder` is authenticated and returns `404` when no current pending/active subscription exists. It exposes the raw domain `status` plus a presentation-safe `lifecycleStatus`, plan, currency code, period boundaries, next charge date, auto-renewal, and cancellation fields. `lifecycleStatus` is `PastDue` when an active entitlement has crossed its paid period boundary with a non-terminal renewal; this is a read-model mapping, not a new domain state. `currentPeriodEndUtc` is the entitlement boundary; `nextBillingAtUtc` is only the next charge-attempt date. When cancellation is scheduled, `cancellationEffectiveAtUtc` equals the current period end.

`POST /api/v1/subscriptions/current/cancellation-at-period-end` is authenticated:

```json
{ "billingAccountOwnerType": "Stakeholder" }
```

It returns `found`, `changed`, and `entitledUntilUtc`. Repeated requests are safe. The subscription remains active until that timestamp, auto-renewal is disabled, and no refund or immediate cancellation is implied.

### Billing history

`GET /api/v1/subscriptions/billing-history?BillingAccountOwnerType=Stakeholder&Page=1&PageSize=20` is authenticated and owner-scoped. Page size is `1..100`. Initial purchases and renewal attempts are returned newest first with authoritative amount, currency, payment state, provider, merchant reference, and a billing period when known.

```json
{
  "items": [
    {
      "paymentTransactionId": "019...",
      "subscriptionId": "019...",
      "planCode": "PRO_MONTHLY",
      "planName": "Pro Monthly",
      "billingPeriodStartUtc": "2026-11-23T12:05:00Z",
      "billingPeriodEndUtc": "2026-12-23T12:05:00Z",
      "amount": 750000,
      "currencyCode": "NGN",
      "paymentStatus": "Succeeded",
      "paymentProviderName": "Bachs",
      "paidAtUtc": "2026-11-23T12:05:00Z",
      "merchantReference": "pay_019...",
      "createdAtUtc": "2026-11-23T12:00:00Z"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 1
}
```

## Frontend flow

1. Load public plans and retain the selected plan id through authentication.
2. After authentication, query eligible providers for that plan and billing owner type.
3. Generate and persist one UUID idempotency key for the user's checkout action.
4. Submit the purchase, then render or follow the returned instruction fields.
5. On redirect/return, ignore provider query parameters as evidence and poll the purchase-status route.
6. Stop on `isTerminal`, or when the frontend's polling bound is reached; continue showing a pending state when the bound expires.
7. Load the current subscription and paginated billing history for the dashboard.
8. Schedule period-end cancellation and use `entitledUntilUtc`/`cancellationEffectiveAtUtc` as the effective date.

Upgrades, downgrades, proration, refunds, invoices, receipts, and immediate customer cancellation are not supported by these contracts.
