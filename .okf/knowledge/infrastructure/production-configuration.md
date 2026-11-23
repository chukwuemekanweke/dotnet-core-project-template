---
type: infrastructure
title: Production Configuration and Runtime Secret Consumption
description: Fail-closed KeyPerFile startup, per-host secret declarations, service isolation, and the DEVOPS-007 Compose boundary
resource: okf://knowledge/infrastructure/production-configuration
tags:
    - constraint
    - infrastructure
    - configuration
    - runtime-secrets
    - docker-compose
    - security
sources:
    - resource: ""
generated:
    at: "2026-11-21T00:00:00Z"
    by: human
status: stable
code_refs:
    - src/BackendProjectTemplate.Infrastructure/Configuration/**
    - src/BackendProjectTemplate.WebAPI/ProductionSecrets.cs
    - src/BackendProjectTemplate.Consumer/ProductionSecrets.cs
    - src/BackendProjectTemplate.Jobs/ProductionSecrets.cs
    - src/BackendProjectTemplate.DatabaseMigrator/ProductionSecrets.cs
    - src/BackendProjectTemplate.WebAPI/Program.cs
    - src/BackendProjectTemplate.WebAPI/Infrastructure/ForwardedHeaders/**
    - src/BackendProjectTemplate.WebAPI/Infrastructure/CrossOrigin/**
    - src/BackendProjectTemplate.Infrastructure/Observability/OtlpExporterConfigurator.cs
    - src/BackendProjectTemplate.Consumer/Program.cs
    - src/BackendProjectTemplate.Jobs/Program.cs
    - src/BackendProjectTemplate.DatabaseMigrator/Program.cs
    - deploy/runtime-secrets/**
    - deploy/compose.production.yml
    - deploy/vps-deploy-stack.production.json
    - src/BackendProjectTemplate.Infrastructure/Authentication/JwtOptions.cs
    - src/BackendProjectTemplate.Infrastructure/Authentication/ServiceCollectionExtensions.cs
    - tests/unit/BackendProjectTemplate.Infrastructure.UnitTests/Configuration/**
    - tests/unit/BackendProjectTemplate.Infrastructure.UnitTests/When_ConfiguringJwtAuthentication_Should.cs
governance: constraint
---
## Host startup contract

Every deployable host calls `AddProductionSecrets` (generic over `IHostApplicationBuilder`) immediately after
creating its builder: `WebApplication.CreateBuilder`, or `Host.CreateApplicationBuilder` for the one-shot
DatabaseMigrator (see [[database-migrations]]). In
Production, `/run/secrets/app` must exist and be readable; KeyPerFile is added with `reloadOnChange: false`
and each host-owned `ProductionSecrets.cs` key is then looked up in the KeyPerFile provider only (never the
merged configuration), so an environment-only value never satisfies file delivery. KeyPerFile is the final
provider and must win over JSON, environment, and command-line sources; a hosted lifecycle validator
re-checks this in `StartingAsync`, so never add configuration providers after the bootstrap call. Missing,
unreadable, blank, or invalid required files fail startup before migrations, requests, or workloads, and
failure messages must never contain values. Requirement lists are computed before KeyPerFile loads, so
enablement flags must not live in secret files.

Every non-Production environment skips this bootstrap; Development keeps JSON, Development JSON and user
secrets, and local Compose (`Container`) keeps working without secret files. The
template JWT key exists only in Development JSON and is itself a rejected production placeholder. Never
reintroduce a `JwtOptions.SigningKey` default or a key in base `appsettings.json`/Compose. WebAPI validates
`Jwt:SigningKey` (≥32 UTF-8 bytes, HMAC-SHA256) with `ValidateOnStart` in every environment; production keys
are additionally file-delivered and cannot equal a known placeholder.

`When_StartingHostInProductionWithoutSecrets_Should` invokes each real host entry point in Production; it
requires that `/run/secrets/app` does not exist on the test machine.

## Ownership and isolation

Infrastructure owns only the generic delivery validation/provider composition. Each application host owns
its secret list. Keep `deploy/runtime-secrets/<service>.keys.json` aligned with the corresponding list.
Conditional secrets follow an existing runtime enablement flag; DI registration alone does not establish a
requirement. Jobs requires Redis for readiness but never R2. DatabaseMigrator requires only PostgresWrite.

`deploy/compose.production.yml` is standalone, names no images or build sections (vps-deploy injects
release digests), runs every service with a read-only root filesystem (tmpfs `/tmp` and `/home/app`),
`no-new-privileges`, the image's non-root user and `restart: "no"`, publishes only WebAPI on
`127.0.0.1:8080`, sets Production explicitly, mounts only the matching
DEVOPS-007 `current` generation read-only at `/run/secrets/app`, uses the service numeric group, disables
implicit host-path creation, and keeps runtime credentials out of environment variables. GitHub credentials
remain build secrets only. The production migrator sets `DOTNET_ENVIRONMENT: Production` (it is a Generic Host),
sits in the `migration` profile, and no service depends on it. The ordinary `docker-compose.yml` runs every
host as `Container` (the migrator through `DOTNET_ENVIRONMENT`) and must keep
working without runtime-secret delivery: never `Production` (it would require `/run/secrets/app`) and never
`Development` (its DI `ValidateOnBuild` currently fails for Consumer, Jobs and DatabaseMigrator, which register
unused Application/Identity services). WebAPI gets the dev-only JWT key there via
`${JWT_SIGNING_KEY:-development-only-signing-key-not-for-production}`.

## Lifecycle

DEVOPS-007 owns Doppler, render, activation, tmpfs, and rollback. Applications never call Doppler or poll
files. Activation does not retarget a live bind mount; recreate and health-check the affected container to
consume a generation. Deployed hosts export OTLP directly to Grafana Cloud (same model as pickly): endpoint and
protocol are non-secret `stack.env` values (`OTEL_EXPORTER_OTLP_ENDPOINT`, `OTEL_EXPORTER_OTLP_PROTOCOL`), and the
auth header is the optional runtime secret `OTEL_EXPORTER_OTLP_HEADERS` (key manifests of webapi, consumer and jobs;
the migrator exports nothing). `OtlpExporterConfigurator` applies it from configuration to every exporter, so a
secret file works where the SDK would otherwise read only the environment variable. Never pass it as an
environment variable on a deployed host. Adding a Doppler key requires the host manifest copy under
`/etc/runtime-secrets/manifests/` to list it first, or render rejects the payload. Direct production Pyroscope
profiling remains disabled until an agent owns its environment-only credentials.

## Reverse proxy trust

WebAPI applies `X-Forwarded-For`/`X-Forwarded-Proto` only from proxies listed in `ForwardedHeaders:KnownProxies`
(IPs) or `ForwardedHeaders:KnownNetworks` (CIDR), plus the framework's loopback default
(`Infrastructure/ForwardedHeaders`); invalid entries fail startup. A host proxy in front of a
loopback-published container port connects from the Docker network gateway, so a deployment must pin its
network subnet and trust that gateway (development does; `compose.production.yml` does not yet). Never trust
broad ranges: rate limiting partitions on `RemoteIpAddress`.

## CORS

`Infrastructure/CrossOrigin` builds the default policy. In Production only `Cors:AllowedOrigins`
(`scheme://host[:port]`, no path; normalized and de-duplicated; invalid entries fail startup) may call the API.
With none configured it falls back to `localhost:9000` for backward compatibility. Every other environment
allows any origin. Origins are non-secret: set them in the deployment's Compose environment
(`Cors__AllowedOrigins__N`), never in base `appsettings.json`. Development allows the `template-user`,
`template-admin` and `template-marketing` `chidelu.com` frontends.

Passkeys (`Authentication:Passkeys`) are per deployment too: base `appsettings.json` holds localhost values, so
each deployed Compose file must set `RelyingPartyId` and override *every* `AllowedOrigins` index the base file
defines (configuration arrays merge by index; leftover base entries stay allowed). Origins are de-duplicated, so
repeating one to fill a slot is safe. Development uses RP ID `chidelu.com` with the `template-user` and
`template-admin` origins; production needs its own RP ID, and passkeys registered in development will not
work there.

## Known production gaps (verified in the DEVOPS-012 rehearsal with release `4fdbdf5`)

- Data Protection has no `PersistKeysTo`: in containers it logs "Using an ephemeral key repository", so every
  container recreation (each deploy or restoration) invalidates Data-Protection-protected payloads (Identity
  tokens, auth cookies/flows). Persisting the key ring (e.g. PostgreSQL or Redis) is an open application task.
- Health semantics: WebAPI `/health` has no registered checks (process liveness only); Consumer
  `/health/readiness` returns 503 when RabbitMQ is down; Jobs `/health/readiness` checks Redis but not
  RabbitMQ, so a deploy can pass health while Jobs cannot publish outbox messages.
