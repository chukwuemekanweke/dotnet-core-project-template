# Backend Project Template

A .NET 10 backend starter organized around DDD boundaries, vertical slices in the application layer, modular schemas, Redis caching, OpenTelemetry, and containerized development dependencies.

## Included

- `src/BackendProjectTemplate.Domain`: entities, repository/specification abstractions, and infrastructure-facing interfaces
- `src/BackendProjectTemplate.Application`: vertical slices for use cases, DTOs, handlers, and specifications
- `src/BackendProjectTemplate.Infrastructure`: EF Core, Redis cache, JWT, OTP delivery, telemetry, and other implementations
- `src/BackendProjectTemplate.DatabaseMigrator`: dedicated deployment-time migrator that runs pre-deploy SQL, EF Core migrations, seed data, and post-deploy SQL
- `src/BackendProjectTemplate.WebAPI`: controller-based HTTP host and presentation layer
- `src/BackendProjectTemplate.Consumer`: worker placeholder for async message consumption with readiness and liveness endpoints
- `src/BackendProjectTemplate.Jobs`: worker placeholder for scheduled work with readiness and liveness endpoints
- `tests/BackendProjectTemplate.UnitTests`: unit tests for the authentication flow
- `tests/BackendProjectTemplate.IntegrationTests`: end-to-end endpoint tests using SQL Server and Redis testcontainers

## Architecture Notes

- Domain owns entities plus contracts such as repositories, cache interfaces, token generation, and OTP delivery
- Application keeps vertical slices by feature and depends only on the domain
- Infrastructure contains EF Core persistence, Redis caching, JWT generation, observability, and other implementation details
- Database changes are applied by a separate migrator service before the other services are deployed
- The migrator exposes readiness and liveness endpoints so deployment orchestration can distinguish between startup and completed database work
- WebAPI is only the presentation host and endpoint mapping layer
- Schemas are separated by domain using `authentication` and `reference_data`
- `TimeProvider` is the standard time abstraction used across handlers and infrastructure

## Template Usage

Install the template from the repository root:

```powershell
dotnet new install .
```

Create a new solution:

```powershell
dotnet new backend-template --organizationAbbreviation CN --clientName Acme --clientProjectName Ordering -o .\CN.Acme.Ordering
```

The generated root name becomes `{OrganizationAbbreviation}.{ClientName}.{ClientProjectName}` and is applied to the solution, projects, folders, and namespaces. The organization abbreviation is intended for short forms such as `CN` and should be at most 3 characters.

If you want an interactive prompt instead of typing the parameters yourself, run:

```powershell
.\scripts\New-BackendProject.ps1
```

The script prompts for organization abbreviation, client name, and client project name, then installs the local template and creates the solution for you. If you leave organization blank, it defaults to `CN`.

## Local Development

Restore, build, and test:

```powershell
$env:DOTNET_CLI_HOME = "$PWD\.dotnet"
dotnet restore
dotnet build
dotnet test
```

Run the database migrator on its own:

```powershell
dotnet run --project src/BackendProjectTemplate.DatabaseMigrator
```

The migrator executes scripts in:

- `src/BackendProjectTemplate.DatabaseMigrator/Scripts/PreDeploy`
- `src/BackendProjectTemplate.DatabaseMigrator/Scripts/PostDeploy`

In `docker compose`, the migrator stays running and only becomes healthy after the database work completes. The other services depend on that liveness state and will not start while the migrator is still unhealthy or has failed.

The migrator health endpoints are:

- Readiness: `http://localhost:8080/health/readiness`
- Liveness: `http://localhost:8080/health/liveness`

`/health/readiness` returns healthy while the migrator is available to execute the deployment work. `/health/liveness` only returns healthy after pre-deploy SQL, EF migrations, seed data, and post-deploy SQL have all completed successfully.

Start the local stack:

```powershell
docker compose up --build
```

Useful endpoints:

- API: `http://localhost:8080`
- OpenAPI: `http://localhost:8080/openapi/v1.json`
- Metrics: `http://localhost:8080/metrics`
- Health: `http://localhost:8080/health`
- Grafana: `http://localhost:3000`
- Prometheus: `http://localhost:9090`
- Tempo: `http://localhost:3200`

The `consumer` and `jobs` containers expose internal `/health/readiness` and `/health/liveness` endpoints for orchestration. In `docker compose`, both services wait for the database migrator to complete successfully before starting.

Default SQL Server credentials in the template:

- user: `sa`
- password: `Your_strong_Password123!`
