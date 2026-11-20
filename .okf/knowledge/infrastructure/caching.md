---
type: infrastructure
title: Caching Conventions (Redis-backed JSON cache)
description: IJsonCache abstraction and its Redis-backed DistributedJsonCache implementation
resource: okf://knowledge/infrastructure/caching
tags:
    - constraint
    - infrastructure
    - caching
    - redis
governance: constraint
code_refs:
    - src/BackendProjectTemplate.Infrastructure/Caching/**
    - src/BackendProjectTemplate.Domain/Common/Caching/**
    - src/BackendProjectTemplate.Infrastructure/Analytics/RedisUserActivityThrottle.cs
    - src/BackendProjectTemplate.Domain/Analytics/IUserActivityThrottle.cs
sources:
    - kind: file
      path: src/BackendProjectTemplate.Infrastructure/Caching/DistributedJsonCache.cs
generated:
    at: "2026-09-23T00:00:00Z"
    by: human
status: stable
---
## Shape

Application code depends on `IJsonCache` (defined in `Domain.Common.Caching`), never on `IDistributedCache`/Redis types directly. `Infrastructure.Caching.DistributedJsonCache` is the only
implementation and serializes values as JSON over the configured distributed cache (Redis in every environment — see [[dev-environment]] for local vs cloud Redis).

## Constraint

New JSON caching needs go through `IJsonCache`. Capabilities requiring Redis-native atomic semantics that
`IDistributedCache` cannot express use a narrow Domain interface with its implementation in Infrastructure;
`IUserActivityThrottle` is the existing example and uses StackExchange.Redis ownership-aware lock methods.
Do not
inject `IDistributedCache` or a Redis client into Application/WebAPI code — that keeps caching swappable and
keeps cache-key/serialization conventions in Infrastructure.
