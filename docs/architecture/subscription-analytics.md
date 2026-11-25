# Administrative subscription analytics

CAF-BE-023 exposes five read-only, versioned endpoints below `/api/v1/admin/subscriptions/analytics`:
`summary`, `timeline`, `renewals`, `plans` and `statuses`. All require the `subscriptions.analytics.read`
permission (Administrator and Finance Officer). Anonymous requests return `401`, authenticated users
without the permission `403`. The reports are cross-tenant; `tenantId` narrows a report but is not an
authorization mechanism.

## Filters

Every endpoint accepts the same query parameters. Invalid values return `400` with a
`ValidationProblemDetails` body.

| Parameter | Type | Default | Rules |
|---|---|---|---|
| `from` | `yyyy-MM-dd` | `to` minus 29 days | Inclusive UTC calendar date |
| `to` | `yyyy-MM-dd` | today (UTC) | Inclusive; `from <= to`; at most 366 days in total |
| `planId` | UUID | all plans | Must not be the empty UUID |
| `ownerType` | `Tenant` \| `Stakeholder` | both | Owner type of the subscription's BillingAccount |
| `tenantId` | UUID | all tenants | Must not be the empty UUID |
| `currencyId` | UUID | all currencies | Subscriptions denominated in that currency |
| `interval` | `Day` \| `Week` \| `Month` \| `Year` | `Day` | `timeline` only |

The range becomes the half-open UTC interval `[from 00:00, to + 1 day 00:00)`. **Snapshot** metrics are
read at the last instant of `to` (`snapshotAtUtc`, e.g. `2026-11-30T23:59:59.9999999Z`). **Period**
metrics count events whose timestamp falls inside the range.

A subscription is denominated in its plan's currency, which never changes, so `currencyId` selects the
same subscriptions for every endpoint and never leaks revenue from other currencies. Unknown but
well-formed identifiers are valid filters that simply match nothing (zero-valued response).

## Metric definitions

Every metric is reconstructed from durable history, so a later cancellation, expiry, renewal or plan
repricing never changes what a past date reports.

- **Active subscriptions** (snapshot). Subscriptions that held entitlement at the snapshot instant: inside
  their entitlement window `[start, end)` and already backed by a persisted subscription period. Paid
  subscriptions in renewal grace remain active until their lifecycle ends. Pending purchases, purchases
  whose payment never succeeded, and scheduled allocations that never started are never active.
  Complimentary allocations are active (they are entitled), but carry no revenue.
- **New subscriptions** (period). Subscriptions whose first entitled instant falls inside the range. A
  replacement subscription is also new.
- **Churned subscriptions** (period). See [Churn](#churn).
- **Past-due subscriptions** (snapshot). Active subscriptions whose latest paid period, as persisted at
  the snapshot, had already ended. This is the renewal grace window, whether retries are still running or
  have been exhausted.
- **Cancel-at-period-end subscriptions** (snapshot). Active subscriptions with a period-end cancellation
  requested at or before the snapshot.
- **Successful renewals** (period). Distinct renewals that reached `Fulfilled` inside the range.
- **Failed renewals** (period). Distinct renewals that reached `Exhausted` (every retry failed) inside the
  range. A declined attempt that a later retry recovers is **not** a failed renewal.
- **Renewal success rate** (period). `successful / (successful + failed) * 100`, rounded to two
  decimals; `0` when there are no terminal renewals. Pending, charging, retrying, awaiting-authorization,
  awaiting-reconciliation, cancelled and `FulfillmentBlocked` renewals are excluded from the denominator.
- **Failed payment attempts** (period, `renewals` only). Individual declined renewal charges, counted per
  attempt.
- **Pending / awaiting-reconciliation renewals** (`renewals` only). The **current** operational backlog
  for the filtered subscriptions; these two counts do not depend on `from`/`to`.
- **MRR and ARR** (snapshot, financial). See [MRR and ARR](#mrr-and-arr).

### Churn

A subscription churns when its lifecycle ends (expired, cancelled immediately, or ended after a
period-end cancellation) and **no other subscription of the same BillingAccount holds entitlement at
that instant**. The churn instant is the effective entitlement end. Rules:

- A replacement that starts exactly when the previous subscription ends (plan switch, follow-on
  allocation) prevents churn. The replacement still counts as new.
- A gap between subscriptions counts as churn when the first one ends, and as a new subscription when the
  customer returns.
- Subscriptions that were never entitled (abandoned or unpaid purchases, lapsed allocations, allocations
  cancelled before they started) never churn.
- The replacement check uses the replacement's entitlement window at the churn instant, not its current
  status. It deliberately **ignores** `planId`, `ownerType` and `currencyId`. Filters choose which
  subscriptions are reported; a plan or currency switch on the same account is not customer churn in any
  slice. For example, a BillingAccount moving from plan A to plan B reports zero churn for
  `planId=A`.

Summary and timeline use the same definition: the sum of timeline `churnedSubscriptions` equals the
summary value for the same filters.

### MRR and ARR

- MRR sums, per subscription active at the snapshot, the price of its **single** effective paid period:
  the most recently persisted period from a paid purchase or paid renewal. Each period snapshot keeps the
  price, currency and billing interval agreed when it was bought or renewed, so repricing a plan never
  changes existing MRR. Duplicate payment or renewal deliveries never create extra periods. Two periods of
  the same subscription are never counted at the same time.
- Complimentary administrative allocations are excluded. Their period price is a nominal plan snapshot,
  not money collected. The domain has no billable-allocation concept, so no allocation contributes
  revenue.
- Normalization (`SubscriptionRevenueMath`): `Monthly` price ÷ 1, `Yearly` price ÷ 12. These are the
  only billing intervals the domain defines; there is no quarterly interval. ARR is always MRR × 12.
- Amounts are decimal minor currency units (the same units as plan prices and payments) and are not
  rounded, so yearly prices can produce fractional MRR (`1000 / 12 = 83.3333…`). Round only for display.

### Currency isolation

Money is never combined or converted across currencies. Every monetary value appears in a `revenues`
array with one entry per currency (`currencyId`, `currencyCode`, `mrr`, `arr`), ordered by
`currencyCode`. The summary lists currencies with active paid revenue at the snapshot. Every timeline
bucket lists every currency that appears anywhere in that timeline, with `0` where it had no revenue, so
chart series stay aligned.

## Endpoints and responses

### `GET summary`

```json
{
  "from": "2026-11-01",
  "to": "2026-11-30",
  "snapshotAtUtc": "2026-11-30T23:59:59.9999999+00:00",
  "activeSubscriptions": 182,
  "newSubscriptions": 24,
  "churnedSubscriptions": 5,
  "pastDueSubscriptions": 3,
  "cancelAtPeriodEndSubscriptions": 7,
  "successfulRenewals": 61,
  "failedRenewals": 2,
  "renewalSuccessRate": 96.83,
  "revenues": [
    { "currencyId": "…", "currencyCode": "NGN", "mrr": 1250000.0, "arr": 15000000.0 },
    { "currencyId": "…", "currencyCode": "USD", "mrr": 4991.666666666666666666666667, "arr": 59900.0 }
  ]
}
```

### `GET timeline?interval=Week`

```json
{
  "interval": "Week",
  "buckets": [
    {
      "periodStart": "2026-10-26",
      "periodEnd": "2026-11-01",
      "activeSubscriptions": 170,
      "newSubscriptions": 1,
      "churnedSubscriptions": 0,
      "successfulRenewals": 2,
      "failedRenewals": 0,
      "revenues": [ { "currencyId": "…", "currencyCode": "NGN", "mrr": 1150000.0, "arr": 13800000.0 } ]
    }
  ]
}
```

Buckets are contiguous, ordered and always present, including empty ones. `periodStart` is the
calendar-aligned start (`Week` starts on Monday, `Month` on the 1st, `Year` on 1 January), so the first
bucket may start before `from`. Events are still only counted from `from`. `periodEnd` is the bucket's
last calendar day, capped at `to` for the final bucket. Snapshot values in a bucket (`activeSubscriptions`,
`revenues`) are read at the last instant of `periodEnd`; event counts cover `[max(periodStart, from),
periodEnd]`. The final bucket therefore reconciles with `summary`. Its active count and revenues equal
the summary's, and bucket sums of new, churned, successful and failed renewals equal the summary totals.

### `GET renewals`

```json
{
  "successfulRenewals": 61,
  "failedRenewals": 2,
  "failedPaymentAttempts": 9,
  "pendingRenewals": 4,
  "awaitingReconciliationRenewals": 1,
  "successRate": 96.83
}
```

### `GET plans`

Subscriptions active at the snapshot, grouped by plan, ordered by `subscriptionCount` descending then plan
code. Counts sum to `activeSubscriptions`; `percentage` is the share of that total.

```json
{ "plans": [ { "planId": "…", "planCode": "STANDARD", "planName": "Standard", "subscriptionCount": 120, "percentage": 65.93 } ] }
```

### `GET statuses`

Every filtered subscription grouped by its recorded lifecycle status at the snapshot (from status
history, never the current status). Ordered `Pending`, `Active`, `Cancelled`, `Expired`, `Scheduled`;
statuses without subscriptions are omitted.

```json
{ "statuses": [ { "status": "Active", "subscriptionCount": 182, "percentage": 71.09 } ] }
```

`Active` here is the recorded status. It can differ slightly from `activeSubscriptions` for subscriptions
whose entitlement just ended but whose expiry has not yet been processed.

### Zero-data responses

An empty slice is a `200` with zero values, never a `404`:

```json
{
  "from": "2026-11-01", "to": "2026-11-30", "snapshotAtUtc": "2026-11-30T23:59:59.9999999+00:00",
  "activeSubscriptions": 0, "newSubscriptions": 0, "churnedSubscriptions": 0, "pastDueSubscriptions": 0,
  "cancelAtPeriodEndSubscriptions": 0, "successfulRenewals": 0, "failedRenewals": 0,
  "renewalSuccessRate": 0, "revenues": []
}
```

The timeline still returns every bucket with zero counts and `"revenues": []`; `plans` returns
`{ "plans": [] }` and `statuses` returns `{ "statuses": [] }`.

## Frontend consumption

```ts
type Revenue = { currencyId: string; currencyCode: string; mrr: number; arr: number };

const params = new URLSearchParams({ from: "2026-11-01", to: "2026-11-30", ownerType: "Tenant" });
const [summary, timeline] = await Promise.all([
  adminFetch(`/api/v1/admin/subscriptions/analytics/summary?${params}`),
  adminFetch(`/api/v1/admin/subscriptions/analytics/timeline?${params}&interval=Week`),
]);

// KPI cards: one MRR/ARR card per currency; never add amounts of different currencies.
const mrrCards = summary.revenues.map((r: Revenue) => ({
  label: `MRR (${r.currencyCode})`,
  value: formatMinorUnits(r.mrr, r.currencyCode), // convert minor units and round only here
}));

// Charts: one series per currency; every bucket carries every currency of the timeline.
const labels = timeline.buckets.map((b) => b.periodStart);
const activeSeries = timeline.buckets.map((b) => b.activeSubscriptions);
const newVsChurned = timeline.buckets.map((b) => ({ x: b.periodStart, new: b.newSubscriptions, churned: b.churnedSubscriptions }));
const mrrSeries = (code: string) =>
  timeline.buckets.map((b) => b.revenues.find((r: Revenue) => r.currencyCode === code)?.mrr ?? 0);
```

Use `snapshotAtUtc` to label snapshot cards ("as of 30 Nov 2026"). Present new, churned and renewal
counts as "during the range".

## Querying and caching

Summary, timeline and plans share one activity projection in
`SubscriptionAnalyticsReadModelRepository`, so they cannot drift apart. Every series is aggregated in
PostgreSQL per UTC day (at most 366 rows per query), with a fixed number of no-tracking queries per
request; nothing materializes the full subscription history. Responses are cached per endpoint for 2
(summary), 5 (timeline) and 3 (breakdowns) minutes. The cache key includes every effective filter, the
interval and a metric-definition version. Cache failures fall back to PostgreSQL. Ranges ending today
therefore lag by at most one TTL.

## Limitations

- History starts at the records that exist. Activation and price-change instants are when the
  subscription period was persisted, and a few seconds may separate a paid boundary from its renewal
  period, which counts as past due for that interval.
- Churn is recognized once the lifecycle has actually ended. If expiry processing is delayed, the
  subscription stops being active at its entitlement end but appears as churn only after the expiry is
  processed.
- A `to` date in the future reports only already-known boundaries (for example scheduled cancellations).
  It does not forecast renewals.
- Billable allocations, quarterly billing, proration, refunds and exchange-rate conversion do not exist
  in the domain and are not reported.
