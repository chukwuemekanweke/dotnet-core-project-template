# Backend Project Template

A .NET 10 backend starter organized around DDD boundaries, vertical slices in the application layer, modular schemas, Redis caching, OpenTelemetry, and containerized development dependencies.

## Included

- `src/BackendProjectTemplate.Domain`: entities, repository/specification abstractions, and infrastructure-facing interfaces
- `src/BackendProjectTemplate.Application`: vertical slices for use cases, DTOs, handlers, and specifications
- `src/BackendProjectTemplate.Infrastructure`: EF Core, Redis cache, JWT, OTP delivery, telemetry, and other implementations
- `src/BackendProjectTemplate.DatabaseMigrator`: dedicated deployment-time migrator that runs pre-deploy SQL, EF Core migrations, seed data, and post-deploy SQL
- `src/BackendProjectTemplate.WebAPI`: controller-based HTTP host and presentation layer
- `src/BackendProjectTemplate.Consumer`: worker placeholder for async message consumption
- `src/BackendProjectTemplate.Jobs`: worker placeholder for scheduled work
- `tests/BackendProjectTemplate.UnitTests`: unit tests for the authentication flow
- `tests/BackendProjectTemplate.IntegrationTests`: end-to-end endpoint tests using SQL Server and Redis testcontainers

## Architecture Notes

- Domain owns entities plus contracts such as repositories, cache interfaces, token generation, and OTP delivery
- Application keeps vertical slices by feature and depends only on the domain
- Infrastructure contains EF Core persistence, Redis caching, JWT generation, observability, and other implementation details
- Database changes are applied by a separate migrator service before the other services are deployed
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
dotnet new backend-template -n Acme.Ordering -o .\Acme.Ordering
```

The `-n` value becomes the solution, project, and namespace prefix.

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

Start the web application and local infrastructure:

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

Default SQL Server credentials in the template:

- user: `sa`
- password: `Your_strong_Password123!`
