# Administrative dashboard data requirements

BE-DS-002 reviewed what the administrative dashboard needs from the backend. This document records
the decision, the two API gaps it closed, and the rules frontends must follow when composing the
dashboard from existing endpoints.

## Decision: frontend composition

The dashboard is composed in the frontend from independent, existing administrative endpoints. A
unified dashboard endpoint, a dedicated dashboard read model, dashboard tables, materialized views
and background aggregation jobs were all considered and rejected: the existing user, payment and
subscription analytics endpoints already answer every section's question, and a combined endpoint
would merge sections with different permissions, freshness and caching behavior.

Two gaps were found and closed with small read-only endpoints:

1. An administrative KYC aggregate summary (`GET /api/v1/admin/kyc/summary`).
2. An administrative payment provider listing (`GET /api/v1/payment-providers`).

| Dashboard section | Endpoint(s) | Permission |
| --- | --- | --- |
| Registrations | `GET /api/v1/admin/analytics/registrations` | `users.analytics.read` |
| Active users | `GET /api/v1/admin/analytics/active-users` | `users.analytics.read` |
| Payments | `GET /api/v1/admin/payments/analytics/*` | `payments.analytics.read` |
| Subscriptions | `GET /api/v1/admin/subscriptions/analytics/*` | `subscriptions.analytics.read` |
| KYC queue | `GET /api/v1/admin/kyc/summary` | `kyc.read` |
| Payment providers | `GET /api/v1/payment-providers` | `payments.read` |

## Dashboard sections are independently authorized

Each section calls its own endpoint under its own permission. A user may see some sections and not
others: for example, a Compliance Officer can see the KYC queue but not payment analytics. The
frontend must treat a `403` on one section as "hide or disable this section", not as a dashboard-wide
failure, and must not infer one section's data from another's.

## KYC summary

`GET /api/v1/admin/kyc/summary`

| Query parameter | Type | Rules |
| --- | --- | --- |
| `tenantId` | UUID, optional | Narrows the report. An empty UUID (`00000000-0000-0000-0000-000000000000`) or a malformed value returns `400`. It is a filter, not an authorization boundary. |
| `providerKey` | string, optional | Narrows the report to one KYC provider (for example `kycaid`). Trimmed; blank means no filter; longer than 50 characters returns `400`. |

Response `200 OK`:

```json
{
  "pendingCount": 14,
  "inProgressCount": 38,
  "rejectedLastSevenDaysCount": 9,
  "asOfUtc": "2026-11-25T19:00:00+00:00",
  "rejectedFromUtc": "2026-11-18T19:00:00+00:00"
}
```

Metric semantics:

- `pendingCount`: attempts whose current status is `Pending` (started, not yet submitted).
- `inProgressCount`: attempts whose current status is `InProgress` (submitted, awaiting a decision).
- `rejectedLastSevenDaysCount`: attempts whose status is `Rejected` and whose `CompletedAtUtc` (the
  moment of rejection) is in the half-open window `[asOfUtc - 7 days, asOfUtc)`. When the attempt
  *started* does not matter: one started a month ago and rejected yesterday is counted. Verified,
  Expired and Failed attempts are never counted as rejections.
- `asOfUtc` is captured once per request from the server clock; `rejectedFromUtc` is exactly seven days
  earlier. Use both to label the widget.
- All counts are **verification attempts, not distinct stakeholders**. A stakeholder rejected twice in
  the window contributes two.
- Visibility matches the administrative KYC list: soft-deleted attempts and attempts belonging to a
  soft-deleted stakeholder are excluded.
- An empty dataset returns `200` with all three counts set to `0`.

Authorization: `kyc.read` (Administrator, Compliance Officer). `401` without a valid session, `403`
without the permission. Read-only, so no step-up MFA.

Freshness: computed live from PostgreSQL on every request in one aggregate query, with no cache.
Results reflect committed KYC state immediately, including callbacks already processed by the Consumer.

## Payment provider listing

`GET /api/v1/payment-providers` (no query parameters)

Response `200 OK`:

```json
[
  { "id": "0193…", "providerName": "Bachs", "providerKey": "bachs", "isActive": true },
  { "id": "0193…", "providerName": "Credo", "providerKey": "credo", "isActive": true },
  { "id": "0193…", "providerName": "SafeHaven", "providerKey": "safehaven", "isActive": false }
]
```

- Returns every visible (not soft-deleted) payment provider, active or inactive, including providers
  with no payment history. Ordered by `providerName`, then `id`, so the order is deterministic.
- `isActive` is the **administrative activation switch** controlled by
  `PUT /api/v1/payment-providers/{id}/activation`. It is not a health, availability or success-rate
  measure, and is never derived from payment transactions. Use payment analytics for measured provider
  performance.
- Only the four fields above are returned. Credentials, API keys, secrets and per-currency provider
  configuration are never exposed.
- An empty dataset returns `200` with `[]`.

This endpoint differs from `GET /api/v1/subscription-plans/{planId}/payment-providers`, which is a
customer-facing, plan-specific list of eligible active providers only.

Authorization: `payments.read` (Administrator, Finance Officer). `401` without a valid session, `403`
without the permission. The activation endpoint on the same route keeps `payments.manage` plus the
`admin.payment-providers.set-activation` step-up MFA proof; the permissions are applied per action so
the read does not require management rights.

Freshness: read live from PostgreSQL with no cache, so an activation change is visible on the next
request.

## Frontend consumption rules

- **Snapshot and period metrics stay distinct.** KYC pending/in-progress counts, provider activation,
  DAU and MRR are point-in-time snapshots. Registrations, payment volume and KYC rejections are period
  totals. Do not add a snapshot to a period total, and label each with its own as-of time or range.
- **Registration totals** are historical stakeholder registrations up to the requested end date,
  including stakeholders later soft-deleted. They can exceed the visible user list.
- **Registration percentage change** measures registration activity between equal periods. It is not
  growth of the total user population.
- **DAU comparisons** compare equivalent historical daily snapshots (for example, today against the
  same weekday a week earlier), never one day's DAU against a multi-day aggregate.
- **Payment volume comparisons** use reporting periods of equal length with the same currency (and
  any other) filters on both sides.
- **Subscription MRR comparisons** use equivalent historical snapshots with the same currency filter.
- **Financial values are never combined across currencies.** Every monetary value is reported per
  currency; the dashboard shows one series or tile per currency and never sums or converts them.
- Use provider `id` values from the provider listing as the `paymentProviderId` filter for payment
  analytics; `providerKey` is a stable label, not a filter for those endpoints.
