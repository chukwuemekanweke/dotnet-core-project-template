# Production configuration and runtime secrets

## Ownership and data flow

DEVOPS-007 in `devops-toolkit` owns Doppler retrieval, validation, tmpfs generations, atomic activation,
and host-to-container delivery. This repository never authenticates to Doppler. It consumes one secret
per file from the service-specific directory mounted read-only at `/run/secrets/app`.

At startup, each Production host:

1. Requires `/run/secrets/app` to exist and be readable.
2. Adds the standard .NET KeyPerFile provider with `reloadOnChange: false` after all default providers
   (`__` in a file name becomes `:`; dot-prefixed files such as DEVOPS-007's `.metadata.json` are ignored).
3. Verifies, by querying only the KeyPerFile provider, that every secret declared by that host was
   delivered as an upper-case `__` file and is not blank (or weak/placeholder for the JWT key).
4. Confirms that each delivered value is the effective value in `IConfiguration`.
5. Fails before service registration, migrations, request handling, or background work when validation
   fails. Errors name the host and configuration key but never the value.
6. Re-checks during `IHostedLifecycleService.StartingAsync` — before the HTTP server, background services,
   or the migration worker start — that no provider was added later in `Program.cs`.

The final provider order is `appsettings.json`, `appsettings.{Environment}.json`, user secrets in
Development, environment variables, command-line arguments, then KeyPerFile. The last provider wins, so
files override every other source. A production environment variable cannot satisfy a missing required
file because required keys are looked up in the KeyPerFile provider, not the merged configuration.

Development does not add KeyPerFile or require `/run/secrets/app`. The checked-in development JWT key is
loaded only from `appsettings.Development.json` (and, for local Compose, from the overridable
`JWT_SIGNING_KEY` default in `docker-compose.yml`); it cannot become a Production fallback and is rejected as
a placeholder if it is ever delivered as a production secret file.

Local `docker-compose.yml` runs all hosts as `Container`, a non-Production environment, so it needs neither
Doppler nor `/run/secrets/app`. It deliberately avoids `Development`: that environment turns on DI
`ValidateOnBuild`, and Consumer, Jobs and DatabaseMigrator currently register Application/Identity services
whose dependencies only WebAPI provides. Independently of environment,
WebAPI validates `Jwt:SigningKey` with `ValidateOnStart` (at least 32 UTF-8 bytes), so a non-Development,
non-Production environment without a key also fails at startup instead of at the first token operation.

## Service isolation and required secrets

The application-owned DEVOPS-007 allowlists are under `deploy/runtime-secrets/`. They must stay aligned
with each host's `ProductionSecrets.cs` declaration.

| Service | Required | Conditional | Explicitly not required |
| --- | --- | --- | --- |
| WebAPI | PostgreSQL write/read, Redis, JWT signing key, R2 key pair, Mailtrap webhook secret, IpInfo, IpWhoIs, Bachs API/webhook, Credo secret key, SafeHaven client ID/assertion | Kycaid API key when `Kyc:Kycaid:Enabled=true` | RabbitMQ, Mailtrap API token |
| Consumer | PostgreSQL write/read, Redis, RabbitMQ user/password, R2 key pair, Mailtrap API token, Bachs API key | None currently | JWT, Mailtrap webhook secret, KYC/IP geolocation, Credo, SafeHaven |
| Jobs | PostgreSQL write/read, Redis (readiness), RabbitMQ user/password, IpInfo, IpWhoIs, Bachs, Credo, SafeHaven | Kycaid API key when enabled | JWT, R2, email |
| DatabaseMigrator | PostgreSQL write | None | Every WebAPI, Consumer, Jobs, cache, messaging, and integration credential |

The payment providers are required for their actual consumers because the post-deploy seed currently
activates Bachs, Credo, and SafeHaven. If provider enablement becomes configuration-driven, update the host
requirement declaration and tests together; DI registration alone is not proof of consumption.

The JWT signing key must contain at least 32 UTF-8 bytes for the configured HMAC algorithm and must not be
a known template placeholder. Token creation and validation continue to bind the same `JwtOptions` value.

## Production Compose contract

Use the standalone production definition; do not merge it with the development definition:

```bash
docker compose -f docker-compose.production.yml config
docker compose -f docker-compose.production.yml up --build
```

Set `RUNTIME_SECRETS_APPLICATION`, `RUNTIME_SECRETS_ENVIRONMENT`, and each service's numeric
`*_SECRETS_GID` to the values in the DEVOPS-007 instance configuration. Compose binds each service's
`current` generation to `/run/secrets/app`, uses `create_host_path: false`, adds only that service's group,
and sets `ASPNETCORE_ENVIRONMENT=Production`. Missing source directories therefore fail container creation
instead of being silently created. All services use `restart: "no"` so systemd boot ordering cannot be
bypassed.

GitHub credentials remain BuildKit secrets only. Runtime credentials are absent from the production
environment. Non-secret endpoints, bucket names, provider public identifiers, and feature flags remain
ordinary configuration.

Production telemetry targets a credential-free collector endpoint (default `http://otel-collector:4317`).
The collector/agent must own Grafana export credentials. Direct Pyroscope profiling is disabled in the
production Compose definition until a credential-holding agent is deployed; the development Compose flow
retains its existing direct cloud/local behavior. This is an explicit deployment prerequisite, not an
automatic KeyPerFile migration, because the OTel SDK and native profiler read those credentials from
process environment variables.

## Rotation and failure behavior

KeyPerFile is a startup snapshot. DEVOPS-007 renders and activates a new host generation, but Docker
resolves the `current` pointer when it creates the bind mount. Activation neither retargets a running
container nor reloads .NET configuration. Recreate the affected container, then health-check it. A render
failure preserves the running container and current generation; a boot with no valid generation fails
closed.

Typical startup failures are a missing/unreadable directory, missing/unreadable required file, blank value,
weak/placeholder JWT key, a value that did not win provider precedence, or a provider registered after
KeyPerFile. None falls back to development
configuration or environment credentials.

## Extending the contract

To add a production secret:

1. Add the configuration key to only the consuming host's `ProductionSecrets.cs` (conditionally when the
   integration has an existing enablement flag).
2. Add the upper-case `__` name to that service's key manifest under `deploy/runtime-secrets/`.
3. Provision it only in the matching DEVOPS-007 service config and recreate the container after activation.
4. Add or update isolation, missing-file, blank-value, and conditional-enablement tests.

To add another host, reference Infrastructure, call `AddProductionSecrets` immediately after
`WebApplication.CreateBuilder`, create a host-owned requirement declaration and key manifest, and give the
service its own DEVOPS-007 config/token/group/generation and read-only Compose bind. Do not share another
service's directory.

For an optional integration, keep its non-secret enabled flag outside Doppler and include its credential
requirements only when that flag is true. The runtime feature must use the same flag; merely registering a
provider in DI is not sufficient. Host requirement lists are evaluated before KeyPerFile is added, so an
enablement flag must come from JSON, environment variables, or the command line — never from a secret file.
