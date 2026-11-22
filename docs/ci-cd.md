# CI/CD

This template is moving from one self-contained GitHub Actions workflow to thin callers of reusable
workflows owned by [`devops-toolkit`](https://github.com/chukwuemekanweke/devops-toolkit) (DEVOPS-012).
The migration ships in three PRs so each stage can be checked against the old behaviour before the old
code is removed.

| Stage | Status | What it adds |
|---|---|---|
| PR 1: shared .NET CI | **Comparison only** | `ci-shared-validation.yml` calls the toolkit's `dotnet-ci.yml` next to the existing `ci.yml` |
| PR 2: image publishing | Not started | `.github/docker-images.json` plus a caller for the toolkit's `docker-publish.yml` |
| PR 3: VPS deployment | Not started | Production Compose, stack registration, and a caller for the toolkit's `vps-deploy.yml` (deployment off until readiness gates pass) |

## Who owns what

The **application repository** owns its source, solution and test layout, Dockerfiles, image names,
production Compose topology, runtime secret key lists, workflow triggers, environment selection, health
checks, and notification destinations.

The **toolkit** owns restore, format, build, test discovery and execution, TRX and coverage handling,
image build, scan and publish, release manifests, VPS deployment, migrations, health gating, deployment
records, and restoration.

Change application configuration here. Change shared workflow behaviour in the toolkit, with its own
tests and release. Don't copy toolkit logic into this repository.

## Current workflows

### `ci.yml` (still authoritative)

`ci.yml` is unchanged. It restores, checks `IDE0005`, builds Release, runs unit and integration tests
with TRX and XPlat coverage, combines coverage with ReportGenerator, and, only on `push` to `main`,
pushes the four images (`webapi`, `consumer`, `jobs`, `migrator`) to Docker Hub. It stays in place
until the shared path has demonstrated parity. It is retired in a later change, not in PR 1.

### `ci-shared-validation.yml` (temporary comparison)

This workflow runs on pull requests, pushes to `main`, and manual dispatch. It never publishes images
and never deploys.

- **`shared-ci`** calls `devops-toolkit/.github/workflows/dotnet-ci.yml` pinned to a full commit SHA.
  It passes the solution, .NET 10, Release, `IDE0005`, both test-project globs,
  `fail-on-empty-test-set`, coverage, and TRX upload.
- **`parity-check`** downloads the toolkit's combined TRX and coverage artifacts. It runs
  `.github/scripts/verify-shared-ci-artifacts.py`, which fails unless all of these hold:
  - the test projects that ran are exactly the ones `ci.yml`'s `find` would discover;
  - every TRX parses and reports a non-zero test count;
  - every test executed (no skipped, failed, errored, or not-executed tests);
  - the combined `Cobertura.xml` parses.

Test discovery uses the globs `tests/unit/**/*.UnitTests.csproj` and
`tests/integration/**/*.IntegrationTests.csproj`. Every matched project runs even after an earlier
failure. An empty category fails.

### Docker-unavailable protection

Integration tests use Testcontainers fixtures owned by this repository (PostgreSQL, Redis, RabbitMQ,
Tempo, OpenTelemetry Collector). The shared workflow does not start any Compose stack. Three
independent guards stop a missing Docker daemon from producing a green run:

1. The fixtures rethrow Testcontainers' `DockerUnavailableException` as a failure ("Docker is
   unavailable … never skipped"). They don't skip.
2. The toolkit runs `docker info` before integration tests and scans the TRX for Docker-unavailable
   messages.
3. `parity-check` rejects any TRX with skipped or not-executed tests.

## Credentials

| Purpose | Repository secrets | Where they go |
|---|---|---|
| Private NuGet (Mailtrap on GitHub Packages) | `GH_PKG_USERNAME`, `GH_PKG_PAT` | Mapped explicitly to the toolkit's `nuget-username` / `nuget-token`. The toolkit exposes them as `GITHUB_USERNAME` / `GITHUB_PAT` (referenced by `nuget.config`) only to restore steps |
| Docker Hub push (legacy `ci.yml` only) | `DOCKERHUB_USERNAME`, `DOCKERHUB_TOKEN` | Not used by the comparison workflow |

Callers map each secret by name and never use `secrets: inherit`. Workflow-level permissions are `{}`,
and each job is granted only `contents: read`. GitHub withholds secrets from fork pull requests, so
the Mailtrap restore fails for forks in both workflows. This is a known limitation, and it is not
worked around with `pull_request_target`. Runtime application secrets never enter CI.

## Toolkit reference pinning

The toolkit has no published release tag yet, so callers pin a reviewed full commit SHA (currently
`4ab2e18d180fb670dc9d592c917f69760dbe0540`, toolkit `main` after DEVOPS-010). The toolkit checks out
its helper scripts from the exact called commit (`job.workflow_sha`), so the workflow and its scripts
always match. To upgrade:

1. Review the toolkit diff between the pinned SHA and the candidate.
2. Update the `uses:` SHA in every caller together, so all callers stay on one coherent baseline.
3. Confirm the comparison workflow and parity check pass.
4. Switch to an immutable release tag once one is published. Adopt a moving `v1` alias only after it
   is formally published and validated.

Never reference `@main` or an unpublished tag.

## Repository access

`devops-toolkit` must be **public**. This template is public, and GitHub refuses a public caller's
reference to a private reusable workflow, even with the toolkit's Actions access set to "accessible
from repositories owned by the user". The run fails before any job starts with `error parsing called
workflow … workflow was not found`. The toolkit also checks out its own helper scripts using the
caller's `GITHUB_TOKEN`, which works only while the toolkit is public. Do not work around either
restriction with a personal access token.

## Re-running a failed shared CI run

Re-run **all jobs**, never only the failed ones. The toolkit names its artifacts by run attempt and
combines them by that name. A failed-jobs-only re-run leaves the earlier attempt's unit results out of
the combined TRX artifact, and `parity-check` fails.

## Validating CI parity

For a given commit, compare the `ci.yml` run with the `ci-shared-validation.yml` run:

- the same unit and integration projects ran (`parity-check` asserts this against `ci.yml`'s discovery);
- format, Release build, unit and integration tests all succeeded;
- the TRX and combined coverage artifacts exist and parse;
- no package credential appears in either log (GitHub masks mapped secrets).

Negative scenarios to exercise on throwaway branches with manual dispatch, reverting afterwards:

- a failing unit test;
- a failing integration test;
- a test glob that matches nothing;
- a broken package source (restore failure).

Each should fail `shared-ci`.
