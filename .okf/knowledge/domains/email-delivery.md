---
type: domain
title: Email Delivery Tracking
description: Mailtrap webhook contract, correlation, and terminal delivery-state rules for email notification logs
resource: okf://knowledge/domains/email-delivery
tags:
    - constraint
    - notifications
    - email
    - mailtrap
    - webhook
    - delivery
governance: constraint
code_refs:
    - src/BackendProjectTemplate.Domain/Notifications/Entities/EmailNotificationLog.cs
    - src/BackendProjectTemplate.Domain/Notifications/Entities/EmailDeliveryWebhookInbox.cs
    - src/BackendProjectTemplate.Application/Notifications/Features/ProcessMailtrapDeliveryWebhook/**
    - src/BackendProjectTemplate.WebAPI/Features/EmailNotifications/Webhooks/Mailtrap/**
    - src/BackendProjectTemplate.Consumer/Notifications/EmailDeliveryWebhookReceivedHandler.cs
    - src/BackendProjectTemplate.Infrastructure/Notifications/MailtrapWebhookSignatureValidator.cs
    - tests/integration/BackendProjectTemplate.WebAPI.IntegrationTests/EmailNotifications/Webhooks/Mailtrap/**
    - tests/integration/BackendProjectTemplate.Consumer.IntegrationTests/Notifications/EmailDeliveryWebhookReceived/**
sources:
    - kind: url
      path: https://docs.mailtrap.io/email-api-smtp/webhooks
    - kind: url
      path: https://docs.mailtrap.io/email-api-smtp/analytics/statuses-and-events
generated:
    at: "2026-11-14T00:00:00Z"
    by: codex
status: stable
---
## Mailtrap contract

- The JSON webhook format is an object containing an `events` array; batches are supported.
- Track the exact lowercase terminal event names `delivery`, `bounce`, and `reject`. `soft bounce` and
  `suspension` are temporary and must not be recorded as terminal failures.
- Correlate the webhook `message_id` with `EmailNotificationLog.ProviderMessageId` and use `event_id`
  as the provider-scoped idempotency key.
- Event timestamps are Unix epoch seconds. Bounce failure detail comes from `response`; reject detail
  comes from `reason`.
- Verify the hexadecimal `Mailtrap-Signature` as HMAC-SHA256 over the original request bytes with a
  constant-time comparison. Return HTTP 200 after durable asynchronous acceptance; Mailtrap retries
  non-200 responses.

## Delivery state rules

`EmailNotificationLog` distinguishes send failures from post-send delivery outcomes. `FailureReason`
describes failure to call the provider; `DeliveryFailureReason` and `FailedAtUtc` describe an accepted
message that later bounced or was rejected.

The lifecycle is `Pending -> Sent -> Delivered|Bounced|Rejected`. The three outcomes are terminal:
duplicate events are idempotent, a late conflicting event cannot replace a terminal outcome, and
`MarkSent` cannot regress a terminal record. State changes go through the entity's domain methods.

The WebAPI persists each supported event to `EmailDeliveryWebhookInbox` and publishes through the
transactional outbox. The Consumer correlates and applies the outcome, marks the inbox processed, and
emits the corresponding delivery observability event.
