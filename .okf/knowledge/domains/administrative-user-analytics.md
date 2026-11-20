---
type: domain
title: Administrative User Analytics
description: Registration, DAU, rolling WAU/MAU, visible user lists, authorization, caching and persistence semantics
resource: okf://knowledge/domains/administrative-user-analytics
tags:
    - constraint
    - domain
    - analytics
    - user-activity
    - authorization
    - caching
sources:
    - resource: ""
generated:
    at: "2026-11-20T00:00:00Z"
    by: agent
status: stable
code_refs:
    - src/BackendProjectTemplate.Domain/Analytics/ReadModels/**
    - src/BackendProjectTemplate.Application/Analytics/Features/**
    - src/BackendProjectTemplate.Infrastructure/Persistence/*AnalyticsReadModelRepository.cs
    - src/BackendProjectTemplate.WebAPI/Features/Analytics/**
    - tests/**/Analytics/**
governance: constraint
---
## Contract and calendar

Administrative user reporting is exposed through registration aggregates, active-user aggregates,
registration lists, and selected-day active-user lists. All dates use UTC. HTTP date ranges are inclusive;
registration persistence predicates use half-open timestamps. The default range is thirty calendar days
including the end date, the maximum range is 366 days, and future reporting dates are rejected. Calendar
grouping uses `Interval` and `AnalyticsCalendar`; weeks begin Monday and missing periods are zero-filled.

Registration time is `Stakeholder.CreatedAtUtc`. The previous comparable period is the immediately
preceding equal number of calendar days. A positive current count over a zero previous count has a null
percentage change; two zero counts produce zero.

## Active-user definitions

`UserDailyActivity` is the sole engagement source. DAU counts distinct stakeholders on the selected end
date. WAU and MAU count distinct stakeholders over the inclusive seven- and thirty-day rolling windows
ending on that date; never sum DAU values. DAU/MAU uses the shared zero-safe percentage helper. Timeline
queries cover the requested dates while summary queries include the full lookback before chart start.

Current-day data is eventually consistent because the Consumer asynchronously aggregates durable activity
observations. Reporting endpoints use `ExcludeFromUserActivity` so reading reports does not create product
activity.

## Visibility, security and caching

Historical counts bypass the stakeholder soft-delete filter where required and must not join stakeholder
profiles, so later deletion does not erase history. Detailed lists respect soft deletion. Last login is the
latest tenant-and-stakeholder-scoped `LoginActivityType.InitialLogin`; token refreshes never qualify.

All four endpoints require `users.analytics.read`, initially granted only to Administrator. The platform
administrative model is cross-tenant; an optional tenant identifier narrows results and is never an
authorization grant. Authorization runs before controller/application cache access.

Only aggregate endpoints are cached through `IJsonCache`. Keys include endpoint, effective dates, interval
when applicable, tenant or global scope, and UTC. Registration TTL is two minutes, active-user TTL one
minute, cache failure falls back to PostgreSQL, and lists are not cached.

## Persistence

Read paths use `AppReadDbContext`, `AsNoTracking`, projected fields, database aggregation, server-side
pagination, and correlated last-login subqueries. Reporting indexes are tenant/date/stakeholder for daily
activity, tenant/created/id for stakeholders, and tenant/stakeholder/activity-type/descending occurrence
for login activity.
