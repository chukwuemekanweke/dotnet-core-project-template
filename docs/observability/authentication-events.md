# Authentication Observability Specification

This document defines the business observability contract for the authentication flow in this repository.

It covers:

- business events
- attribute naming
- Grafana dashboard expectations
- future SLO considerations

## Goals

The authentication flow should be observable at two levels:

- discrete business events that show what happened for a specific flow

The key business questions are:

- how many sign-up requests were received
- how many users were successfully created
- how many `UserCreated` messages were successfully processed by the subscriber
- how many OTP confirmations succeeded
- how to correlate a single authentication journey across services

## Signals

Use the following split:

- OpenTelemetry logs/events for business event records
- optional span events when the event occurs inside an active trace and trace correlation is useful

Prometheus should not be treated as a custom event store.

Authentication-specific counters are intentionally deferred for now.

## Event Names

Use domain-style names for all authentication events.

- `authentication.signup_requested`
- `authentication.user_created`
- `authentication.user_created_processed`
- `authentication.otp_confirmed`
- `authentication.signup_failed`

## Event Attributes

Every authentication business event should use a consistent attribute contract.

### Common attributes

- `event.name`
- `event.domain`
- `event.version`
- `flow.id`
- `outcome`
- `service.name`
- `occurred_at`

### Optional attributes

- `user.id`
- `user.email_hash`
- `failure.reason`
- `channel`

### Attribute rules

- `event.domain` should be `authentication`
- `event.version` should start at `1`
- `flow.id` should identify a single authentication journey across async boundaries
- `user.id` should be included only when known
- `user.email_hash` may be included if email-level troubleshooting is needed without sending raw email addresses
- `failure.reason` must remain low-cardinality
- `occurred_at` should use UTC timestamps

## Event Definitions

### `authentication.signup_requested`

Emitted when the WebAPI accepts a new sign-up request.

Recommended attributes:

- `event.name = authentication.signup_requested`
- `event.domain = authentication`
- `flow.id`
- `user.email_hash`
- `channel = api`
- `outcome = success`

### `authentication.user_created`

Emitted when the user is persisted and the `UserCreated` message is written to the transactional outbox.

Recommended attributes:

- `event.name = authentication.user_created`
- `event.domain = authentication`
- `flow.id`
- `user.id`
- `outcome = success`

### `authentication.user_created_processed`

Emitted by the Consumer when the `UserCreated` message is handled successfully and the sign-up OTP is generated and delivered.

Recommended attributes:

- `event.name = authentication.user_created_processed`
- `event.domain = authentication`
- `flow.id`
- `user.id`
- `outcome = success`

### `authentication.otp_confirmed`

Emitted when OTP verification succeeds and the account is confirmed.

Recommended attributes:

- `event.name = authentication.otp_confirmed`
- `event.domain = authentication`
- `flow.id`
- `user.id`
- `outcome = success`

### `authentication.signup_failed`

Emitted when sign-up fails in a business-relevant way.

Recommended attributes:

- `event.name = authentication.signup_failed`
- `event.domain = authentication`
- `flow.id` when available
- `failure.reason`
- `outcome = failure`

## High Cardinality Rules

High-cardinality attributes such as `user.id` are allowed in logs/events.

If a future logs backend such as Loki is added:

- keep `user.id` and `flow.id` in the event payload
- do not promote them to log labels unless there is a strong operational reason and a bounded-cardinality strategy

## Emission Points

### WebAPI Registration Flow

When a sign-up request is accepted:

- emit `authentication.signup_requested`

After the user is created and the outbox event is persisted:

- emit `authentication.user_created`

If sign-up fails:

- emit `authentication.signup_failed`

### Consumer `UserCreatedHandler`

After the `UserCreated` event is successfully processed and the sign-up OTP has been generated and delivered:

- emit `authentication.user_created_processed`

### OTP Confirmation Flow

After OTP confirmation succeeds:

- emit `authentication.otp_confirmed`

## Flow Correlation

The authentication flow spans multiple processes:

- WebAPI
- Jobs
- Consumer

The event contract should support end-to-end correlation.

Use `flow.id` as the business correlation key for the full sign-up journey.

Recommended behavior:

- generate `flow.id` at the start of sign-up
- store or propagate it through the transactional outbox event payload
- include it in the Consumer event processing logs
- include it in OTP confirmation logs

If `flow.id` does not yet exist in the current implementation, it should be introduced before building dashboards that require end-to-end correctness analysis.

## Grafana Expectations

### Prometheus

Prometheus should be used for:

- platform and runtime metrics already exposed by the services

Authentication-specific business counters are deferred for now.

### Logs backend

If a logs backend is added later, it should be used for:

- inspecting individual business events
- searching by `flow.id`
- troubleshooting specific authentication journeys

### Tempo

If trace correlation is useful, span events may be added to traces for authentication request operations. This is optional and does not replace the business event logs.

## Future SLO and Correctness Queries

Authentication funnel and correctness metrics are intentionally deferred.

When counters are introduced later, this document should be extended with:

- sign-up to `UserCreated` processing ratio
- sign-up to OTP confirmation ratio
- sign-up failure ratio

## Naming Conventions

Use the following naming conventions consistently:

- event names: dot-separated domain names
- attributes: dot-separated semantic keys

Examples:

- `authentication.signup_requested`
- `authentication.user_created_processed`
- `user.id`
- `flow.id`

## Implementation Guidance

When this specification is implemented in code:

- centralize event names and property names in shared constants
- do not place raw telemetry logic directly in controllers
- keep telemetry emission aligned with DDD boundaries and vertical slices
- prefer narrow abstractions so application handlers express intent clearly

## Current Gap

At the time of writing, this document is a specification only.

The repository currently implements:

- structured authentication business event emission through activity events
- request and message enrichment through a custom telemetry context

The repository does not yet fully implement:

- end-to-end `flow.id` propagation
- authentication-specific counters for funnel/SLO dashboards

Those can be added in a follow-up implementation.
