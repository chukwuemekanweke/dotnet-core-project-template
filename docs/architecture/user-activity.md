# Authenticated user activity

Qualifying authenticated product interactions are aggregated into one row per tenant, stakeholder,
and reporting date for future DAU, WAU, and MAU reporting. This feature records activity only; it does
not expose analytics endpoints.

`UserActivityMiddleware` runs after authorization and rate limiting. It observes only authenticated,
successful 2xx responses, resolves the stakeholder from the authenticated identity, and reconciles the
request tenant against the stakeholder's persisted tenant. Endpoints can opt out with
`ExcludeFromUserActivity`; health, metrics, API documentation, webhooks, telemetry, and refresh-token
traffic are excluded. Successful password, Google, passkey, and completed two-factor sign-ins reuse
`UserSignInSuccessful`, so intermediate or failed authentication never counts.

## Throttling and delivery

Authenticated requests use fixed five-minute windows aligned to Unix epoch boundaries. Redis keys have
the form `user-activity:v1:{tenant}:{stakeholder}:{reporting-date}:{window-start-unix-seconds}`. A claim
uses StackExchange.Redis ownership-aware lock operations and has a ten-minute TTL. The window encoded in the key defines deduplication;
the longer TTL keeps the full current window, plus one window for late completion, protected.

Redis failure is fail-open: the request submits an event through the transactional outbox so activity is
not silently lost. If the outbox database save definitely fails, the middleware service conditionally
releases only its own Redis claim. RabbitMQ publication happens later from the durable outbox and follows
the existing retry policy, so a broker outage does not suppress the observation.

The Consumer records the event message ID in `analytics.UserActivityObservationReceipts` and updates
`analytics.UserDailyActivities` with tracked EF Core entities inside a serializable transaction. A bounded
retry handles PostgreSQL serialization, deadlock, and unique-constraint races. The receipt makes duplicate
delivery safe; the aggregate keeps the earliest and latest UTC timestamps and increments once for each new
observation.

## Reporting date

The reporting calendar is UTC by design. `ActivityDate` is derived from the UTC observation timestamp
when the observation is created (or when an existing sign-in event is consumed). There is no server-local,
configurable, or tenant-specific timezone conversion, so deployment configuration cannot silently move
activity between reporting dates. Previously persisted dates are never recalculated.
