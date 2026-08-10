# Backend Project Template

A .NET 10 backend starter organized around DDD boundaries, vertical slices in the application layer, modular schemas, Redis caching, a transactional outbox with RabbitMQ dispatching, OpenTelemetry, and containerized development dependencies.

## Included

- `src/BackendProjectTemplate.Domain`: entities, repository/specification abstractions, and infrastructure-facing interfaces
- `src/BackendProjectTemplate.Application`: vertical slices for use cases, DTOs, handlers, and specifications
- `src/BackendProjectTemplate.Infrastructure`: EF Core, Redis cache, JWT, OTP delivery, telemetry, and other implementations
- `src/BackendProjectTemplate.DatabaseMigrator`: dedicated deployment-time migrator that runs pre-deploy SQL, EF Core migrations, seed data, and post-deploy SQL
- `src/BackendProjectTemplate.WebAPI`: controller-based HTTP host and presentation layer
- `src/BackendProjectTemplate.Consumer`: worker placeholder for async message consumption with readiness and liveness endpoints
- `src/BackendProjectTemplate.Jobs`: worker placeholder for scheduled work and transactional outbox dispatching with readiness and liveness endpoints
- `tests/unit/BackendProjectTemplate.Application.UnitTests`: unit tests for application handlers and use cases
- `tests/unit/BackendProjectTemplate.Domain.UnitTests`: unit tests for domain entities and behavior
- `tests/unit/BackendProjectTemplate.Infrastructure.UnitTests`: unit tests for infrastructure components
- `tests/unit/BackendProjectTemplate.WebAPI.UnitTests`: unit tests for WebAPI-specific helpers
- `tests/unit/BackendProjectTemplate.Consumer.UnitTests`: unit tests for consumer worker behavior
- `tests/unit/BackendProjectTemplate.Jobs.UnitTests`: unit tests for scheduled jobs worker behavior
- `tests/integration/BackendProjectTemplate.WebAPI.IntegrationTests`: WebAPI integration tests using SQL Server and Redis testcontainers
- `tests/integration/BackendProjectTemplate.Consumer.IntegrationTests`: consumer host integration tests using SQL Server and Redis testcontainers
- `tests/integration/BackendProjectTemplate.Jobs.IntegrationTests`: jobs host integration tests using SQL Server and Redis testcontainers

## Architecture Notes

- Domain owns entities plus contracts such as repositories, cache interfaces, token generation, and OTP delivery
- Application keeps vertical slices by feature and depends only on the domain
- Infrastructure contains EF Core persistence, Redis caching, JWT generation, observability, and other implementation details
- Asynchronous integration messages are persisted through a transactional outbox and dispatched from the Jobs service through RabbitMQ
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

This creates a fresh project tree without the template repository's `.git` history.

The generated root name becomes `{OrganizationAbbreviation}.{ClientName}.{ClientProjectName}` and is applied to the solution, projects, folders, and namespaces. The organization abbreviation is intended for short forms such as `CN` and should be at most 3 characters.

If you want an interactive prompt instead of typing the parameters yourself, run:

```powershell
.\scripts\New-BackendProject.ps1
```

The script prompts for organization abbreviation, client name, and client project name, then installs the local template and creates the solution for you. If you leave organization blank, it defaults to `CN`.
When `git` is available on `PATH`, the script also initializes a new repository in the generated project directory.

There is also a bash version:

```bash
./scripts/New-BackendProject.sh
```

It supports the same inputs and generated naming convention.
When `git` is available on `PATH`, it also initializes a new repository in the generated project directory.

## Local Development

Restore, build, and test:

```powershell
$env:DOTNET_CLI_HOME = "$PWD\.dotnet"
dotnet restore
dotnet build
dotnet test
```

## Git workflow automation

The Windows PowerShell workflow command automates branch creation, validation,
AI-assisted commit and pull-request drafting, publishing, and returning to an
updated `main` after merge. It requires Git, GitHub CLI (`gh`), the .NET 10 SDK,
and an installed and authenticated Codex CLI. On Windows, the workflow selects a
Codex executable that has its matching sandbox helper, including the version
bundled with the Codex VS Code extension.

This repository publishes through the `chukwuemekanweke` GitHub account without
switching the globally active `gh` login. Git pushes use the
`github-chukwuemekanweke` SSH host alias. Pull-request commands read the
account-specific token from an encrypted local credential and verify the account
before doing any remote work.

Configure the SSH alias in `%USERPROFILE%\.ssh\config`:

```sshconfig
Host github-chukwuemekanweke
    HostName github.com
    User git
    IdentityFile ~/.ssh/id_ed25519_chukwuemekanweke
    IdentitiesOnly yes
```

Configure this clone to use that alias if its `origin` still uses HTTPS:

```powershell
git remote set-url origin git@github-chukwuemekanweke:chukwuemekanweke/dotnet-core-project-template.git
```

Save the PAT once after cloning the repository:

```powershell
.\scripts\git-workflow.ps1 setup-auth
```

Use a PAT that can manage this repository. The command validates the account
before saving the credential under
`%LOCALAPPDATA%\BackendProjectTemplate`. Windows encrypts the token for the
current user on the current computer, so it is loaded automatically on later
workflow runs. During publishing, the workflow also exposes that credential as
`GITHUB_USERNAME` and `GITHUB_PAT` for authenticated package access.
`CHUKWUEMEKANWEKE_GITHUB_TOKEN` remains available as a temporary override for the
current shell. Do not add the private key, token, or encrypted credential file to
this repository.

Start a feature branch from the latest `origin/main`:

```powershell
.\scripts\git-workflow.ps1 start -Epic 12 -Feature 34 -Label "user-profile"
```

This creates a branch named `epic-12/be-34-user-profile`. For work without an
epic or user story, provide only a branch name:

```powershell
.\scripts\git-workflow.ps1 start my-branch
# Equivalent named option:
.\scripts\git-workflow.ps1 start -BranchName "my-branch"
```

Standalone names are normalized under `feature/cn/`: `my-branch` and
`feature/my-branch` become `feature/cn/my-branch`, while
`feature/cn/my-branch` remains unchanged.

After making changes, stage only the intended files and publish them:

```powershell
git add src tests
git diff --cached
.\scripts\git-workflow.ps1 publish
```

`publish` builds the solution and then runs the tests using the existing restore.
Run `dotnet restore` separately after dependency changes. Codex inspects a
disposable Git worktree containing the branch history and staged diff, and
drafts a Conventional Commit message, PR title, and completed
`.github/pull_request_template.md`. The temporary worktree is removed before the
command pauses for approval of the commit message and again for editing or
approval of the PR. PRs are drafts by default; pass `-Ready` to create a
ready-for-review PR, or `-SkipChecks` only when the checks have already been run
separately. If a push or PR creation is interrupted, run `publish` again; with
no staged changes, it resumes PR creation from the commits already on the
feature branch.

Once GitHub reports the current branch's PR as merged, update `main` with:

```powershell
.\scripts\git-workflow.ps1 finish
```

The local feature branch is deliberately preserved. Use `status` for Git and PR
status, or `check` to run the repository validation suite without publishing.

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

If you want Docker Compose to start with the required environment variables, create your own local helper script first.

For Bash:

1. Create `docker-compose.local.sh`.
2. Copy the contents of `docker-compose.env.example.sh` into `docker-compose.local.sh`.
3. Replace the placeholder values with your own local values.
4. Add your Docker Compose command at the end of the file.
5. Run your local script.

Example:

```bash
cp docker-compose.env.example.sh docker-compose.local.sh
chmod +x docker-compose.local.sh
```

Your `docker-compose.local.sh` should look like this:

```bash
#!/usr/bin/env bash

export GITHUB_USERNAME=""
export GITHUB_PAT=""
export MAILTRAP_TOKEN=""
export MAILTRAP_WEBHOOK_SIGNING_SECRET=""
export MAILTRAP_FROM_ADDRESS=""
export MAILTRAP_FROM_NAME="BackendProjectTemplate"

docker compose up -d --build --force-recreate
```

For PowerShell, follow the same pattern with `docker-compose.local.ps1`:

1. Create `docker-compose.local.ps1`.
2. Copy the equivalent environment variable assignments into it.
3. Set your local values.
4. Add the Docker Compose command at the end of the script.

Run the local script instead of calling `docker compose` directly when you need those exported variables.

Useful endpoints:

- API: `http://localhost:8080`
- OpenAPI: `http://localhost:8080/openapi/v1.json`
- Metrics: `http://localhost:8080/metrics`
- Health: `http://localhost:8080/health`
- RabbitMQ: `amqp://localhost:5672`
- RabbitMQ Management: `http://localhost:15672`
- Grafana: `http://localhost:3000`
- Prometheus: `http://localhost:9090`
- Tempo: `http://localhost:3200`
- Pyroscope: `http://localhost:4040`

The `consumer` and `jobs` containers expose internal `/health/readiness` and `/health/liveness` endpoints for orchestration. In `docker compose`, both services wait for the database migrator to complete successfully before starting.

## Profiling

The local observability stack includes Grafana Pyroscope for continuous profiling.

- Grafana provisions a `Pyroscope` data source automatically.
- The `webapi`, `consumer`, and `jobs` containers are built with the native .NET Pyroscope profiler and push profiles directly to `http://pyroscope:4040`.
- The current profiling setup is container-only. Grafana's .NET profiler currently supports Linux on `amd64`, so `dotnet run` on Windows/macOS is not profiled by this configuration.

After `docker compose up --build`, open Grafana at `http://localhost:3000` and use Profiles Drilldown or Explore with the `Pyroscope` data source to inspect:

- `backendprojecttemplate.webapi`
- `backendprojecttemplate.consumer`
- `backendprojecttemplate.jobs`

Default SQL Server credentials in the template:

- user: `sa`
- password: `Your_strong_Password123!`
