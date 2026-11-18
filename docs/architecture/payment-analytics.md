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

Transaction count and attempted volume include every filtered payment. Successful, failed, cancelled,
expired, and processing volumes use their matching statuses; processing means `PendingInitiation` or
`Initiated`. Unsuccessful count means `Failed`, `Cancelled`, or `Expired`. Success rate is
`Succeeded / (Succeeded + Failed + Cancelled + Expired) * 100`; zero terminal outcomes produce zero.
Average successful transaction value is successful volume divided by successful count.

Money is never combined across currencies. Every monetary aggregate carries currency ID and code.
Timeline rows are aggregated in PostgreSQL and missing periods are filled in Application only for the
currencies present in the result. Empty datasets return zero summary counts and empty collections.

## Querying and caching

`PaymentAnalyticsReadModelRepository` applies composable filters and conditional `Count`/`Sum`
aggregates to `AppReadDbContext`; it never loads individual transactions for grouping. The existing
filter indexes and `(CreatedAtUtc, Id)` reporting index are reused, so this change adds no migration.
Shared reporting code uses `Domain.Common.Analytics`: `Interval` defines the calendar grain,
`AnalyticsCalendar` owns UTC calendar bucket boundaries, and `AnalyticsMath` owns safe rounded
percentages and averages. Other reporting slices should reuse these utilities instead of recreating
their own calendar or denominator behavior.

Each endpoint has a separate SHA-256 cache key containing the effective UTC range, applicable
interval, all filters, tenant/global scope, and reporting timezone. Default TTLs are summary two
minutes, statuses three minutes, and timeline/providers/intents five minutes. Authorization runs before
cache access. Cache failures are logged and fall back to PostgreSQL; writes are best effort, and a
per-process single-flight gate limits concurrent identical cache misses. Historical records are not
cached indefinitely because reconciliation may update payment state.

Known limitations: V1 uses UTC only, has no reporting-currency conversion, and does not use materialized
views or precomputed aggregates. Empty timelines cannot manufacture a currency series when no matching
transaction identifies a currency.
