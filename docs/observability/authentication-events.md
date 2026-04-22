# Authentication And Profile Observability Specification

This document defines the observability contract for the authentication, onboarding, and profile flows currently implemented in this repository.

It is intentionally scoped to the flows that already exist in source code today:

- password sign-up
- Google sign-up
- email confirmation
- password sign-in
- Google sign-in
- sign-in failure processing
- password reset request
- password reset OTP delivery
- password reset completion
- sign-out
- session refresh
- session refresh post-processing
- profile update
- avatar upload

Payments are not covered here yet.

## Goals

The observability model should answer three classes of questions:

1. Outcome / correctness
2. Flow progression
3. Technical health

For the currently implemented scope, the most important business questions are:

- Are users successfully signing up?
- Are users receiving and completing email confirmation?
- Are users successfully signing in?
- Are password reset journeys completing?
- Are session refresh and sign-out behaving correctly?
- Are authenticated users able to update their profile and avatar?

## Design Rules

### Event names carry the meaning

Custom event names are the primary business signal.

Examples:

- `PasswordSignUpStarted`
- `PasswordSignUpCompleted`
- `GoogleSignInStarted`
- `GoogleSignInCompleted`

Do not encode redundant state like `outcome`, `source`, `auth_method`, `provider`, or `endpoint` as custom-event properties when the event name already expresses that meaning.

### Use a small, stable property set

Custom event payloads should stay minimal.

Current standard properties:

- `flow.id`
- `correlation_id`
- `tenant_id`
- `stakeholder_id`
- `failure_reason`

Only include `failure_reason` when a failure reason is actually needed.

### Distinguish journey IDs from transport correlation

- `flow.id` is the customer-journey identifier.
- `correlation_id` is the request/message correlation identifier.

They are related but not the same thing.

Use `flow.id` to follow a single user journey across:

- WebAPI
- outbox / broker
- consumer

Use `correlation_id` to correlate one request/message execution.

### Do not invent telemetry context after the fact

- If a consumer message does not inherit from `BaseCommand` or `BaseEvent`, it is invalid for the shared consumer pipeline.
- If a consumer message has no `FlowId`, keep `flow.id` empty.
- Do not synthesize a new customer-journey identifier inside consumer handling.

### Keep failure reasons low-cardinality

Failure reasons must come from shared constants and remain query-friendly.

Current shared source:

- `ObservabilityFailureReasons`

## Current Shared Constants

The current source-of-truth constants live in:

- [Observability.cs](/C:/Work/Chidelu/BackendProjectTemplate/src/BackendProjectTemplate.Domain/Common/Observability/Observability.cs)
- [ObservabilityFailureReasons.cs](/C:/Work/Chidelu/BackendProjectTemplate/src/BackendProjectTemplate.Domain/Common/Observability/ObservabilityFailureReasons.cs)

## Standard Property Contract

### Required on custom events

- `flow.id`
- `correlation_id`

### Include when known

- `tenant_id`
- `stakeholder_id`

### Include only when relevant

- `failure_reason`

### Avoid by default

- raw email address
- IP address
- user agent
- endpoint
- auth method
- provider
- source
- outcome

Those values may still exist in logs or technical traces, but they are not part of the standard business custom-event payload by default.

## Event Taxonomy

These are the current business custom events for the implemented scope.

### Authentication

- `PasswordSignUpStarted`
- `PasswordSignUpCompleted`
- `GoogleSignUpStarted`
- `GoogleSignUpCompleted`
- `EmailConfirmationOtpSent`
- `EmailConfirmationStarted`
- `EmailConfirmationCompleted`
- `PasswordSignInStarted`
- `PasswordSignInCompleted`
- `GoogleSignInStarted`
- `GoogleSignInCompleted`
- `SignInPostProcessingCompleted`
- `SignInFailureProcessed`
- `PasswordResetRequested`
- `PasswordResetOtpSent`
- `PasswordResetCompleted`
- `SignOutCompleted`
- `SessionRefreshCompleted`
- `SessionRefreshPostProcessingCompleted`
- `ProfileUpdateCompleted`
- `AvatarUploadCompleted`

### Notifications

- `EmailNotificationSent`

### Deferred / intentionally unused right now

These names exist or may exist conceptually, but are not part of the preferred current auth/profile custom-event story:

- onboarding started/completed events for sign-up and profile update
- generic `...Failed` custom events for API auth flows where a `Started` event plus missing `Completed` event and failure context is sufficient

## Failure Reason Catalog

Current shared failure reasons:

- `already_confirmed`
- `duplicate_email`
- `duplicate_google_account`
- `invalid_file`
- `invalid_google_token`
- `invalid_otp`
- `not_authenticated`
- `stakeholder_not_found`
- `user_not_found`
- `validation_failed`

Additional sign-in failure processing uses the existing domain failure reasons from `UserSignInFailureReasons`, such as:

- invalid credentials
- locked out
- email not verified
- user not found

Those remain valid because they are already part of the domain contract used by `UserSignInFailed`.

## Flow Mapping

This section maps each implemented flow to the events that should exist.

### Password sign-up

Entry point:

- WebAPI registration flow

Expected business events:

- `PasswordSignUpStarted`
- `PasswordSignUpCompleted`
- `EmailConfirmationOtpSent`

Failure context currently expected:

- `duplicate_email`
- `validation_failed`

### Google sign-up

Entry point:

- WebAPI Google registration flow

Expected business events:

- `GoogleSignUpStarted`
- `GoogleSignUpCompleted`

Failure context currently expected:

- `invalid_google_token`
- `duplicate_email`
- `duplicate_google_account`
- `validation_failed`

### Email confirmation

Entry point:

- WebAPI email confirmation flow

Expected business events:

- `EmailConfirmationStarted`
- `EmailConfirmationCompleted`

Failure context currently expected:

- `invalid_otp`
- `already_confirmed`

### Password sign-in

Entry point:

- WebAPI sign-in flow

Expected business events:

- `PasswordSignInStarted`
- `PasswordSignInCompleted`
- `SignInPostProcessingCompleted`

Failure context currently expected on the request/message path:

- domain sign-in failure reasons via `UserSignInFailed`

### Google sign-in

Entry point:

- WebAPI Google sign-in flow

Expected business events:

- `GoogleSignInStarted`
- `GoogleSignInCompleted`
- `SignInPostProcessingCompleted`

Failure context currently expected:

- `invalid_google_token`
- domain sign-in failure reasons via `UserSignInFailed`

### Sign-in failure processing

Entry point:

- Consumer `UserSignInFailedHandler`

Expected business event:

- `SignInFailureProcessed`

This event exists to show that the async failure-processing branch completed, including account-lock handling where applicable.

### Password reset

Entry points:

- WebAPI password reset request
- Consumer password reset OTP delivery
- WebAPI password reset completion

Expected business events:

- `PasswordResetRequested`
- `PasswordResetOtpSent`
- `PasswordResetCompleted`

Failure context currently expected:

- `user_not_found`
- `invalid_otp`
- `validation_failed`

### Sign-out

Entry point:

- WebAPI logout flow

Expected business event:

- `SignOutCompleted`

### Session refresh

Entry points:

- WebAPI refresh flow
- Consumer refresh post-processing

Expected business events:

- `SessionRefreshCompleted`
- `SessionRefreshPostProcessingCompleted`

### Profile update

Entry point:

- WebAPI profile update flow

Expected business event:

- `ProfileUpdateCompleted`

Failure context currently expected:

- `not_authenticated`
- `validation_failed`
- `stakeholder_not_found`

### Avatar upload

Entry point:

- WebAPI avatar upload flow

Expected business event:

- `AvatarUploadCompleted`

Failure context currently expected:

- `not_authenticated`
- `invalid_file`
- `stakeholder_not_found`

## Implemented Source Mapping

The main current implementation points are:

- [SignUpHandler.cs](/C:/Work/Chidelu/BackendProjectTemplate/src/BackendProjectTemplate.Application/Authentication/Features/SignUp/SignUpHandler.cs)
- [GoogleSignUpHandler.cs](/C:/Work/Chidelu/BackendProjectTemplate/src/BackendProjectTemplate.Application/Authentication/Features/GoogleSignUp/GoogleSignUpHandler.cs)
- [SignUpOtpHandler.cs](/C:/Work/Chidelu/BackendProjectTemplate/src/BackendProjectTemplate.Application/Authentication/Features/SignUpOtp/SignUpOtpHandler.cs)
- [SignInHandler.cs](/C:/Work/Chidelu/BackendProjectTemplate/src/BackendProjectTemplate.Application/Authentication/Features/SignIn/SignInHandler.cs)
- [GoogleSignInHandler.cs](/C:/Work/Chidelu/BackendProjectTemplate/src/BackendProjectTemplate.Application/Authentication/Features/GoogleSignIn/GoogleSignInHandler.cs)
- [RequestPasswordResetHandler.cs](/C:/Work/Chidelu/BackendProjectTemplate/src/BackendProjectTemplate.Application/Authentication/Features/RequestPasswordReset/RequestPasswordResetHandler.cs)
- [CompletePasswordResetHandler.cs](/C:/Work/Chidelu/BackendProjectTemplate/src/BackendProjectTemplate.Application/Authentication/Features/CompletePasswordReset/CompletePasswordResetHandler.cs)
- [RefreshSessionHandler.cs](/C:/Work/Chidelu/BackendProjectTemplate/src/BackendProjectTemplate.Application/Authentication/Features/RefreshSession/RefreshSessionHandler.cs)
- [LogoutSessionHandler.cs](/C:/Work/Chidelu/BackendProjectTemplate/src/BackendProjectTemplate.Application/Authentication/Features/LogoutSession/LogoutSessionHandler.cs)
- [UpdateProfileHandler.cs](/C:/Work/Chidelu/BackendProjectTemplate/src/BackendProjectTemplate.Application/Stakeholders/Features/UpdateProfile/UpdateProfileHandler.cs)
- [UploadAvatarHandler.cs](/C:/Work/Chidelu/BackendProjectTemplate/src/BackendProjectTemplate.Application/Stakeholders/Features/UploadAvatar/UploadAvatarHandler.cs)
- [UserCreatedHandler.cs](/C:/Work/Chidelu/BackendProjectTemplate/src/BackendProjectTemplate.Consumer/Authentication/UserCreatedHandler.cs)
- [ResetPasswordHandler.cs](/C:/Work/Chidelu/BackendProjectTemplate/src/BackendProjectTemplate.Consumer/Authentication/ResetPasswordHandler.cs)
- [UserSignInSuccessfulHandler.cs](/C:/Work/Chidelu/BackendProjectTemplate/src/BackendProjectTemplate.Consumer/Authentication/UserSignInSuccessfulHandler.cs)
- [UserSignInFailedHandler.cs](/C:/Work/Chidelu/BackendProjectTemplate/src/BackendProjectTemplate.Consumer/Authentication/UserSignInFailedHandler.cs)
- [UserAccessTokenRefreshedHandler.cs](/C:/Work/Chidelu/BackendProjectTemplate/src/BackendProjectTemplate.Consumer/Authentication/UserAccessTokenRefreshedHandler.cs)
- [CurrentActorMiddleware.cs](/C:/Work/Chidelu/BackendProjectTemplate/src/BackendProjectTemplate.WebAPI/Infrastructure/CurrentActorMiddleware.cs)
- [BaseMessageHandler.cs](/C:/Work/Chidelu/BackendProjectTemplate/src/BackendProjectTemplate.Consumer/BaseMessageHandler.cs)

## Grafana Dashboard Plan

For the implemented scope, use three dashboards.

### 1. Authentication Overview

Purpose:

- business success visibility for sign-up, sign-in, confirmation, password reset, refresh, and sign-out

Recommended top-row stat panels:

- password sign-up completed count
- Google sign-up completed count
- email confirmation completed count
- password sign-in completed count
- Google sign-in completed count
- password reset completed count
- session refresh completed count

Recommended time-series panels:

- `PasswordSignUpStarted` vs `PasswordSignUpCompleted`
- `GoogleSignUpStarted` vs `GoogleSignUpCompleted`
- `EmailConfirmationStarted` vs `EmailConfirmationCompleted`
- `PasswordSignInStarted` vs `PasswordSignInCompleted`
- `GoogleSignInStarted` vs `GoogleSignInCompleted`
- `PasswordResetRequested` vs `PasswordResetOtpSent` vs `PasswordResetCompleted`
- `SessionRefreshCompleted` vs `SessionRefreshPostProcessingCompleted`

### 2. Authentication Failures And Security

Purpose:

- understand where auth journeys stop and why

Recommended panels:

- count of requests/messages carrying `failure_reason`
- sign-up failures by `failure_reason`
- email confirmation failures by `failure_reason`
- password reset failures by `failure_reason`
- sign-in failure processed count by domain failure reason
- account-lock notifications triggered over time

Recommended table panels:

- recent auth journeys grouped by `flow.id`
- recent failed auth journeys grouped by `correlation_id`

### 3. Profile And Account Management

Purpose:

- track authenticated profile operations

Recommended panels:

- `ProfileUpdateCompleted` over time
- `AvatarUploadCompleted` over time
- profile-related failures by `failure_reason`
- avatar-related failures by `failure_reason`

## Funnel Definitions

These are the derived funnels Grafana should compute from events.

### Registration funnel

Stages:

1. `PasswordSignUpStarted` or `GoogleSignUpStarted`
2. `PasswordSignUpCompleted` or `GoogleSignUpCompleted`
3. `EmailConfirmationOtpSent` for password sign-up only
4. `EmailConfirmationCompleted` where applicable
5. `PasswordSignInCompleted` or `GoogleSignInCompleted`

### Password reset funnel

Stages:

1. `PasswordResetRequested`
2. `PasswordResetOtpSent`
3. `PasswordResetCompleted`

### Sign-in funnel

Stages:

1. `PasswordSignInStarted` or `GoogleSignInStarted`
2. `PasswordSignInCompleted` or `GoogleSignInCompleted`
3. `SignInPostProcessingCompleted`

## Derived Metrics

These are the first derived metrics to compute from business events.

- password sign-up completion rate
- Google sign-up completion rate
- email confirmation completion rate
- password sign-in completion rate
- Google sign-in completion rate
- password reset completion rate
- session refresh post-processing completion rate

Suggested formulas:

- password sign-up completion rate = `PasswordSignUpCompleted / PasswordSignUpStarted`
- Google sign-up completion rate = `GoogleSignUpCompleted / GoogleSignUpStarted`
- email confirmation completion rate = `EmailConfirmationCompleted / EmailConfirmationStarted`
- password sign-in completion rate = `PasswordSignInCompleted / PasswordSignInStarted`
- Google sign-in completion rate = `GoogleSignInCompleted / GoogleSignInStarted`
- password reset completion rate = `PasswordResetCompleted / PasswordResetRequested`
- session refresh post-processing completion rate = `SessionRefreshPostProcessingCompleted / SessionRefreshCompleted`

## Query Expectations

The current local observability stack uses:

- OTLP gRPC from services to an OpenTelemetry Collector
- OTLP gRPC from the collector to Tempo for traces
- OTLP HTTP from the collector to Loki for structured logs
- Grafana with provisioned `Prometheus`, `Tempo`, and `Loki` datasources

`AddCustomEvent(...)` writes both:

- span events for trace correlation
- structured JSON log records for LogQL business-event queries

The current query shape supports:

- counts by event name
- counts by event name filtered by `failure_reason`
- grouping by `tenant_id`
- filtering by `flow.id`
- filtering by `correlation_id`
- grouping by time buckets

The datasource contract must support:

- searching a single journey with `flow.id`
- drilling from event count to concrete event records
- correlating request-side and consumer-side events with the same `flow.id`

## Technical Health Layer

The business custom events above do not replace technical telemetry.

Technical health still needs its own layer for:

- HTTP latency and status codes
- exceptions by type
- dependency latency
- OTP provider reliability
- notification delivery reliability
- queue / outbox / consumer lag

That work is still pending.

## Current Status

### Implemented now

- shared event names
- shared failure-reason constants
- `flow.id` header propagation for WebAPI
- `flow.id` propagation through commands and events
- consumer enforcement that shared messages must inherit from `BaseCommand` or `BaseEvent`
- milestone custom events for the implemented auth/profile scope
- structured business-event logs for `AddCustomEvent(...)`
- Loki datasource provisioning
- Grafana dashboard JSON for authentication overview, authentication failures/security, and profile/account management
- LogQL panel queries for the currently emitted auth/profile events

### Still pending

- alert thresholds and SLO targets
- explicit security panels such as failed sign-ins by IP/email
- payment observability design and implementation

## Next Follow-Up

The next observability deliverable after this spec should be one of:

1. deeper auth failure-event coverage beyond `SignInFailureProcessed`
2. alert thresholds and SLO definitions for the current dashboards
3. technical-health instrumentation for dependencies such as notifications and cache/OTP persistence

Do not expand custom-event payloads ad hoc while doing that follow-up work. Keep changes aligned with this document unless the observability contract is intentionally revised first.
