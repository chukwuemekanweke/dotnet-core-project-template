# Administrative user analytics

CAF-BE-022 adds four read-only, versioned administrative endpoints:

- `GET /api/v1/admin/analytics/registrations` reports all-time, current-period, previous-period, and calendar-bucketed registration counts.
- `GET /api/v1/admin/analytics/active-users` reports DAU, rolling seven-day WAU, rolling thirty-day MAU, DAU/MAU, and the daily timeline.
- `GET /api/v1/admin/users/registrations` lists visible stakeholders registered in the selected range.
- `GET /api/v1/admin/users/active` lists visible stakeholders active on one UTC reporting day.

All reporting dates are UTC and inclusive at the HTTP boundary. Persistence queries convert registration ranges to half-open timestamp boundaries. Weeks begin on Monday. Aggregate queries run in PostgreSQL and missing chart periods are returned as zeroes.

Historical registration and active-user counts remain after stakeholder soft deletion; detailed lists continue to respect the global soft-delete filters. Consequently, aggregate counts can exceed visible-list totals. Last login is the latest `InitialLogin` activity only and never includes token refreshes.

The `users.analytics.read` permission protects all four endpoints and is initially assigned only to the Administrator role. Aggregate responses use tenant- and range-specific Redis cache keys; registration results cache for two minutes and active-user results for one minute. Cache failures fall back to PostgreSQL. Lists are not cached.

Current-day activity is eventually consistent because qualifying observations are delivered through the transactional outbox and aggregated asynchronously by the Consumer. Analytics endpoints are excluded from user-activity observation so opening a report does not inflate engagement.
