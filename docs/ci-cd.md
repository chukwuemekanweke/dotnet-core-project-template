# CI/CD

This template is moving from one self-contained GitHub Actions workflow to thin callers of reusable
workflows owned by [`devops-toolkit`](https://github.com/chukwuemekanweke/devops-toolkit) (DEVOPS-012).
The migration ships in three PRs so each stage can be checked against the old behaviour before the old
code is removed.

| Stage | Status | What it adds |
|---|---|---|
| PR 1: shared .NET CI | **Merged; parity shown** | `ci-shared-validation.yml` calls the toolkit's `dotnet-ci.yml` next to the existing `ci.yml` |
| PR 2: image publishing | **In review** | `.github/docker-images.json`, plus `publish-images` and `verify-release` jobs calling the toolkit's `docker-publish.yml` on `main` |
| Retire legacy jobs | After the first verified release from PR 2 | Remove `ci.yml`'s duplicated verification and its `docker-build-push` matrix |
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
until the shared path has published and verified a release on `main`. It is then retired in a separate
change.

### `ci-shared-validation.yml` (shared callers)

This workflow runs on pull requests, pushes to `main`, and manual dispatch. It publishes images only
on `push` to `main` (see [Release images](#release-images)). It never deploys.

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

## Release images

### Declaring images

[`.github/docker-images.json`](../.github/docker-images.json) is the application-owned build intent. It
follows the toolkit's schema version 1 and lists exactly four components:

| Component | Repository | Dockerfile |
|---|---|---|
| `webapi` | `docker.io/chukwuemekanweke/backendprojecttemplate-webapi` | `src/BackendProjectTemplate.WebAPI/Dockerfile` |
| `consumer` | `docker.io/chukwuemekanweke/backendprojecttemplate-consumer` | `src/BackendProjectTemplate.Consumer/Dockerfile` |
| `jobs` | `docker.io/chukwuemekanweke/backendprojecttemplate-jobs` | `src/BackendProjectTemplate.Jobs/Dockerfile` |
| `migrator` | `docker.io/chukwuemekanweke/backendprojecttemplate-migrator` | `src/BackendProjectTemplate.DatabaseMigrator/Dockerfile` |

All four build from the repository root. The file never holds credentials, build args, or
deployment values. Infrastructure containers (PostgreSQL, Redis, RabbitMQ) are not release components.
A project created from this template must change the namespace to its own.

On every pull request, the `image-config` job runs `.github/scripts/verify-release-images.py config`.
It fails if the component set changes, a Dockerfile goes missing, a repository is not a real
`docker.io/<namespace>/<repository>`, extra keys such as `buildArgs` appear, or a Dockerfile stops
mounting the `GITHUB_USERNAME`/`GITHUB_PAT` BuildKit secrets.

### Publishing

`publish-images` calls `docker-publish.yml` at the same pinned toolkit SHA as `shared-ci`. It runs only
for `push` events on `main`, and only after `shared-ci`, `parity-check`, and `image-config` all pass.
Pull requests and manual dispatches never publish.

- **Deployment id** is `github.sha`. The toolkit tags every image `release-<sha>` and refuses to push a
  tag that already exists, so a release identity can't be overwritten. These tags don't collide with
  the legacy `ci.yml` tags (`latest` and `<sha>`).
- **Package credentials:** all four components are listed in `package-auth-components`. The toolkit
  mounts `GH_PKG_USERNAME`/`GH_PKG_PAT` as BuildKit secrets `GITHUB_USERNAME`/`GITHUB_PAT` for those
  builds only. They are never build args. They are read only by the restore `RUN` and never reach the
  final image, which copies only the publish output and the Pyroscope native libraries.
- **Registry credentials:** `DOCKERHUB_USERNAME`/`DOCKERHUB_TOKEN` are mapped to `registry-username` and
  `registry-token`. Use a Docker Hub access token scoped to read and write on these four repositories.
- **Evidence:** the toolkit publishes SBOM and provenance attestations and runs Trivy against each pushed
  digest. A CRITICAL finding fails that component; HIGH findings are reported. It assembles the release
  manifest only if every component succeeded with its scan, SBOM, and provenance evidence. A partial
  publish never produces a manifest.

### Release manifest

The toolkit's outputs are `deployment-id`, `manifest-json`, `manifest-artifact`
(`release-manifest-<sha>`), and `published-images`. The manifest lists each component's repository and
`sha256` digest. Deployments consume this manifest, never tags.

`verify-release` then:

1. checks that the `manifest-json` output matches the canonical artifact;
2. checks the manifest against `.github/docker-images.json`: the same four components and repositories,
   with `deployment_id` and `source_commit` both equal to the commit;
3. resolves every `image@digest` in Docker Hub;
4. writes the immutable references to the run summary.

### Failed or partial publishes

Images are pushed before they are scanned. If a component fails after another has pushed, its
`release-<sha>` tag is already taken, and re-running cannot reuse that identity. Investigate the cause,
then land a new commit on `main` to get a new deployment id. Never delete or move a `release-*` tag to
retry.

## Credentials

| Purpose | Repository secrets | Where they go |
|---|---|---|
| Private NuGet (Mailtrap on GitHub Packages) | `GH_PKG_USERNAME`, `GH_PKG_PAT` | Mapped explicitly to the toolkit's `nuget-username` / `nuget-token`. The toolkit exposes them as `GITHUB_USERNAME` / `GITHUB_PAT` (referenced by `nuget.config`) only to restore steps |
| Docker Hub push | `DOCKERHUB_USERNAME`, `DOCKERHUB_TOKEN` | `publish-images` (mapped to `registry-username` / `registry-token`, `main` pushes only) and the legacy `ci.yml` matrix until it is retired |
| Docker build package restore | `GH_PKG_USERNAME`, `GH_PKG_PAT` | `publish-images` (mapped to `github-packages-username` / `github-packages-token`, mounted as BuildKit secrets) |

Callers map each secret by name and never use `secrets: inherit`. Workflow-level permissions are `{}`,
and each job is granted only `contents: read`. GitHub withholds secrets from fork pull requests, so
the Mailtrap restore fails for forks in both workflows. This is a known limitation, and it is not
worked around with `pull_request_target`. Runtime application secrets never enter CI.

## Toolkit reference pinning

The toolkit has no published release tag yet, so callers pin a reviewed full commit SHA (currently
`1891bbfc7694eb20233cda1bdd1f511319b54959`, toolkit `main` after the masked-output fixes, #13 and #14). The toolkit checks out
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
