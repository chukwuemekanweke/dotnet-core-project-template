---
type: architecture
title: Shared Analytics Utilities
description: Reusable calendar-bucket and aggregate-math semantics for reporting features
resource: okf://knowledge/architecture/analytics-utilities
tags:
    - constraint
    - architecture
    - analytics
    - reporting
governance: constraint
code_refs:
    - src/BackendProjectTemplate.Domain/Common/Analytics/**
    - tests/unit/BackendProjectTemplate.Domain.UnitTests/Common/Analytics/**
sources:
    - kind: file
      path: src/BackendProjectTemplate.Domain/Common/Analytics/AnalyticsCalendar.cs
generated:
    at: "2026-11-18T00:00:00Z"
    by: agent
status: stable
---
## Rule

Reporting features use the common `Interval` enum (`Day`, `Week`, `Month`, `Year`) and
`AnalyticsCalendar` for calendar-aligned period starts, next-period calculation and range-capped period
ends. Weeks start on Monday. Do not recreate feature-specific calendar bucket arithmetic.

Use `AnalyticsMath` for zero-safe, decimal percentage and average calculations. Both operations round to
two decimal places by default and accept an explicit precision when a report contract requires one.

These types are pure Domain utilities with no persistence, HTTP or caching dependency, so Application
and Infrastructure reporting code can share identical semantics.
