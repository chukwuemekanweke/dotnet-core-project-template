---
type: infrastructure
title: Database Migration Execution (One-shot DatabaseMigrator)
description: One-shot Generic Host migrator, fixed initialization stage order, exit-code contract, idempotent-script and expand/contract rules, Compose and DEVOPS-010 integration
resource: okf://knowledge/infrastructure/database-migrations
tags:
    - constraint
    - infrastructure
    - persistence
    - migrations
    - docker-compose
    - deployment
governance: constraint
code_refs:
    - src/BackendProjectTemplate.DatabaseMigrator/**
    - src/BackendProjectTemplate.Infrastructure/Persistence/DatabaseInitializationExtensions.cs
    - docker-compose.yml
    - docker-compose.local.yml
    - deploy/compose.production.yml
    - tests/unit/BackendProjectTemplate.DatabaseMigrator.UnitTests/**
    - tests/integration/BackendProjectTemplate.DatabaseMigrator.IntegrationTests/**
sources:
    - kind: file
      path: src/BackendProjectTemplate.DatabaseMigrator/Program.cs
    - kind: file
      path: src/BackendProjectTemplate.Infrastructure/Persistence/DatabaseInitializationExtensions.cs
generated:
    at: "2026-11-22T00:00:00Z"
    by: human
status: stable
---
## Lifecycle contract

`DatabaseMigrator` is a one-shot console process on `Host.CreateApplicationBuilder`, not a web host or
`BackgroundService`. `Program.cs` builds the host, calls `StartAsync` (this runs the production
configuration precedence validator and installs the console lifetime), awaits
`DatabaseMigrationRunner.RunAsync(ApplicationStopping)`, stops the host, and **returns the runner's exit code**.
`0` is the only success signal. A failure or a cancellation (SIGTERM/Ctrl+C) of `ApplicationStopping` returns
`1`. Configuration and production-secret failures throw before the host is built and end the process with the
runtime's nonzero unhandled-exception code. `When_StartingHostInProductionWithoutSecrets_Should` relies on that
throw.

Never reintroduce a keep-alive loop, HTTP endpoints, health checks or a migration "state" for the migrator.
Orchestrators must not treat a running migrator as successful. Keep the runner free of process concerns.
It takes the initialization delegate, so its outcome is unit-testable. Its environment comes from
`DOTNET_ENVIRONMENT` (Generic Host ignores `ASPNETCORE_ENVIRONMENT`). The image still uses the `aspnet` runtime
because Infrastructure depends on the ASP.NET Core shared framework (Identity, Data Protection).

## Pipeline (order is part of the contract)

`InitializeDatabaseAsync`: EnsureDatabase → PreDeploy scripts → EF Core `MigrateAsync` → authorization seed →
PostDeploy scripts. Each stage is logged (`Database initialization stage {StageName} started/completed/failed`).
The failing script is logged by file name, and the exception propagates unchanged. Scripts are ordered by file
name and split on `GO` lines. Every batch autocommits, and no transaction spans a script, stage, or run. Earlier
stages stay committed when a later one fails. Nothing is ever reversed automatically. WebAPI can also call this
pipeline (`Database:InitializeOnStartup`), but its content root has no `Scripts/` folder.

## Constraints for schema and script changes

- PreDeploy/PostDeploy scripts are not journaled. They run on **every** execution, including retries, so they
  must be idempotent (`IF NOT EXISTS`, `CREATE OR REPLACE`, upsert/update-when-distinct seeds, guarded fixes).
- Schema changes follow expand → migrate → contract. Rollout failure restores previous application images, not
  the schema, so the previous release must work against the migrated schema. Never drop, rename or retype
  columns the previous release uses in the same release.
- Never log connection strings or secret values. EF sensitive-data logging stays off.

## Compose and deployment

- `docker-compose.yml` (+ `docker-compose.local.yml`): the migrator has no port or health check. WebAPI, Consumer
  and Jobs use `depends_on: database-migrator: condition: service_completed_successfully`.
- `deploy/compose.production.yml`: `database-migrator` is in the `migration` profile with `restart: "no"`,
  `no-new-privileges`, no port and no health check. It mounts only its own DEVOPS-007 generation. No service
  depends on it. The DEVOPS-010 `vps-deploy` workflow runs it with `docker compose run`, enforces the timeout,
  and requires exit `0` before rollout. Release images are injected by digest through the toolkit's generated
  override, so the file names no image tags.

## Tests

The unit tests cover runner outcomes. The integration tests launch the migrator's real build output
(`src/.../bin/<Configuration>/net10.0`) as a child process against a Testcontainers PostgreSQL. They assert
exit codes, that the process terminates (timeout = failure), applied migrations, seed idempotency, and stage
ordering with a temporary content root holding a failing PreDeploy or PostDeploy script.
