# Administrative payment analytics

CAF-BE-019 exposes five read-only, versioned endpoints below
`/api/v1/admin/payments/analytics`: `summary`, `timeline`, `statuses`, `providers`, and `intents`.
All require the `payments.analytics.read` permission, which is assigned to Administrator and Finance
Officer. The endpoints are cross-tenant administrative reports; `tenantId` narrows the report but is
not itself an authorization mechanism.

## Filters and dates

All endpoints accept `from`, `to`, `paymentProviderId`, `currencyId`, `paymentIntent`,
`paymentStatus`, and `tenantId`. Timeline additionally accepts `interval` (`Day`, `Week`, `Month`, or
`Year`). Dates are inclusive UTC calendar dates and become a half-open `CreatedAtUtc` predicate. The
default is the 30 calendar days ending today, and the configurable maximum is 366 days. Week buckets
start on Monday; all other buckets use actual UTC calendar boundaries.

## Metrics and currencies

Transaction count and attempted volume include every filtered payment; attempted volume always uses
the requested `Amount`, including for underpaid payments. Successful, failed, cancelled, expired, and
processing volumes use their matching statuses; processing means `PendingInitiation` or `Initiated`.
Unsuccessful count means `Failed`, `Cancelled`, `Expired`, or `Underpaid`. `Underpaid` is a terminal
outcome where the provider collected only part of the amount (see the payments domain); it is not a
success. `underpaidCount` is reported separately as a subset of `unsuccessfulCount`, so it must not be
added to it. `underpaidVolume` is the collected `AmountPaid`, not the requested amount, and is not
included in failed volume. Success rate is
`Succeeded / (Succeeded + Failed + Cancelled + Expired + Underpaid) * 100`; zero terminal outcomes
produce zero. These rules apply in summary, timeline, provider and intent results. Average successful
transaction value is successful volume divided by successful count.

Money is never combined across currencies. Every monetary aggregate carries currency ID and code.
Timeline rows are aggregated in PostgreSQL and missing periods are filled in Application only for the
currencies present in the result. Empty datasets return zero summary counts and empty collections.

## Querying and caching

`PaymentAnalyticsReadModelRepository` applies composable filters and conditional `Count`/`Sum`
aggregates to `AppReadDbContext`; it never loads individual transactions for grouping. The existing
filter indexes and `(CreatedAtUtc, Id)` reporting index are reused, so this change adds no migration.
Timeline queries project each transaction to one `DateTime` period start, then use one explicit
currency-separated aggregate query for every interval. Npgsql translates day/week buckets to
`date_trunc`/`date_part` expressions and month/year buckets to date-part grouping; only the already
aggregated rows are converted to `DateOnly` in Infrastructure.
Shared reporting code uses `Domain.Common.Analytics`: `Interval` defines the calendar grain,
`AnalyticsCalendar` owns UTC calendar bucket boundaries, and `AnalyticsMath` owns safe rounded
percentages and averages. Other reporting slices should reuse these utilities instead of recreating
their own calendar or denominator behavior.

Each endpoint has a separate SHA-256 cache key containing the effective UTC range, applicable
interval, all filters, tenant/global scope, and reporting timezone. Default TTLs are summary two
minutes, statuses three minutes, and timeline/providers/intents five minutes. Authorization runs before
cache access. Cache failures are logged and fall back to PostgreSQL; writes are best effort, and a
reference-counted per-process keyed lock limits concurrent identical cache misses. A key cannot be
retired while callers hold or await it; waiting cancellation releases only that caller's reference,
and the final releaser removes the idle entry. Historical records are not cached indefinitely because
reconciliation may update payment state.

Known limitations: V1 uses UTC only, has no reporting-currency conversion, and does not use materialized
views or precomputed aggregates. Empty timelines cannot manufacture a currency series when no matching
transaction identifies a currency. PostgreSQL-backed tests verify the generated command count,
parameterized half-open filters, SQL grouping/conditional aggregation and all four provider-translated
calendar expressions. The integration dataset is intentionally small and is not representative enough
for meaningful `EXPLAIN (ANALYZE, BUFFERS)` benchmarking, so no additional index is claimed or added.
