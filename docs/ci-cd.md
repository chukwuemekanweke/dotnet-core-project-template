# CI/CD

CI/CD for this template is a set of thin callers of reusable workflows owned by
[`devops-toolkit`](https://github.com/chukwuemekanweke/devops-toolkit) (DEVOPS-012). This repository holds
configuration; the toolkit holds the automation.

## Who owns what

The **application repository** owns its source, solution and test layout, Dockerfiles, image names,
production Compose topology, runtime secret key lists, workflow triggers, environment selection, health
checks, and notification destinations.

The **toolkit** owns restore, format, build, test discovery and execution, TRX and coverage handling,
image build, scan and publish, release manifests, VPS deployment, migrations, health gating, deployment
records, and restoration.

Change application configuration here. Change shared workflow behaviour in the toolkit, with its own
tests and release. Don't copy toolkit logic into this repository.

## `ci.yml`

`.github/workflows/ci.yml` (**CI/CD (.NET 10)**) runs on pull requests, pushes to `main`, and manual
dispatch. It never deploys.

| Job | When | What it does |
|---|---|---|
| `shared-ci` | Every event | Calls the toolkit's `dotnet-ci.yml`: restore, `IDE0005` format check, Release build, unit and integration tests with TRX and coverage, combined Cobertura/HTML report |
| `image-config` | Every event | Validates `.github/docker-images.json` |
| `publish-images` | Push to `main` | Calls the toolkit's `docker-publish.yml` (see [Release images](#release-images)) |
| `verify-release` | After `publish-images` | Checks the release manifest and resolves every digest |

Test discovery uses the globs `tests/unit/**/*.UnitTests.csproj` and
`tests/integration/**/*.IntegrationTests.csproj`. Every matched project runs even after an earlier
failure, and an empty category fails. The toolkit uploads `dotnet-test-results-<run>-<attempt>` (TRX)
and `dotnet-coverage-<run>-<attempt>` (combined `Cobertura.xml` and HTML summary).

### Docker-unavailable protection

Integration tests use Testcontainers fixtures owned by this repository (PostgreSQL, Redis, RabbitMQ,
Tempo, OpenTelemetry Collector). The shared workflow does not start any Compose stack. Two independent
guards stop a missing Docker daemon from producing a green run:

1. The fixtures rethrow Testcontainers' `DockerUnavailableException` as a failure ("Docker is
   unavailable … never skipped"). They don't skip. Keep that message: the toolkit matches it.
2. The toolkit runs `docker info` before integration tests and scans the TRX for Docker-unavailable
   messages.

### Migration history

The shared path replaced a self-contained `ci.yml` in three steps. Parity was shown on PR #68, where both
workflows ran 7 unit projects (609 tests) and 5 integration projects (104 tests) with nothing skipped.
The first toolkit release, `4fdbdf5`, was published and verified with all four digests, SBOM, provenance
and clean Trivy scans before the legacy jobs and the temporary parity check were removed.

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
for `push` events on `main`, and only after `shared-ci` and `image-config` pass.
Pull requests and manual dispatches never publish.

- **Deployment id** is `github.sha`. The toolkit tags every image `release-<sha>` and refuses to push a
  tag that already exists, so a release identity can't be overwritten. No moving tag such as `latest`
  is published; anything that runs these images uses the manifest digests.
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
| Private NuGet (Mailtrap on GitHub Packages) | `GH_PKG_USERNAME`, `GH_PKG_PAT` | `shared-ci`, mapped to `nuget-username` / `nuget-token`. The toolkit exposes them as `GITHUB_USERNAME` / `GITHUB_PAT` (referenced by `nuget.config`) only to restore steps |
| Docker build package restore | `GH_PKG_USERNAME`, `GH_PKG_PAT` | `publish-images`, mapped to `github-packages-username` / `github-packages-token` and mounted as BuildKit secrets |
| Docker Hub push | `DOCKERHUB_USERNAME`, `DOCKERHUB_TOKEN` | `publish-images`, mapped to `registry-username` / `registry-token`, `main` pushes only |

Callers map each secret by name and never use `secrets: inherit`. Workflow-level permissions are `{}`,
and each job is granted only `contents: read`. GitHub withholds secrets from fork pull requests, so
the Mailtrap restore fails for forks. This is a known limitation, and it is not worked around with
`pull_request_target`. Runtime application secrets never enter CI.

## Toolkit reference pinning

The toolkit has no published release tag yet, so callers pin a reviewed full commit SHA (currently
`1891bbfc7694eb20233cda1bdd1f511319b54959`, toolkit `main` after the masked-output fixes, #13 and #14).
The toolkit checks out its helper scripts from the exact called commit (`job.workflow_sha`), so the
workflow and its scripts always match. To upgrade:

1. Review the toolkit diff between the pinned SHA and the candidate.
2. Update the `uses:` SHA in every caller together, so all callers stay on one coherent baseline.
3. Confirm CI passes on the pull request, and that the next push to `main` publishes and verifies.
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

## Secrets that overlap public names

`GH_PKG_USERNAME` and `DOCKERHUB_USERNAME` hold the same value as the Docker Hub namespace and the
repository owner. GitHub drops any job output that contains a secret the job references ("Skip output …
since it may contain secret"). At toolkit `4ab2e18` this emptied the publish matrix, so no images were
built. Toolkit `a7a4f80` (#13) and `1891bbf` (#14) keep every output-producing job free of secrets. When
you change toolkit pins, check the logs for that warning.

## Re-running a failed CI run

Re-run **all jobs**, never only the failed ones. The toolkit names its artifacts by run attempt and
combines them by that name, so a failed-jobs-only re-run leaves the earlier attempt's results out of the
combined TRX and coverage artifacts. Several WebAPI integration tests are timing-sensitive and fail
intermittently in any workflow (see the OKF `ci-cd` concept); a full re-run is the expected recovery
until they are fixed.
