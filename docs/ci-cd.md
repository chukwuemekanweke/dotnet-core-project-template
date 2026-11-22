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
dispatch. It deploys only from `main`, and only once production deployment is enabled.

| Job | When | What it does |
|---|---|---|
| `shared-ci` | Every event | Calls the toolkit's `dotnet-ci.yml`: restore, `IDE0005` format check, Release build, unit and integration tests with TRX and coverage, combined Cobertura/HTML report |
| `release-config` | Every event | Calls the toolkit's `release-config-check.yml` (see [Release configuration check](#release-configuration-check)) |
| `publish-images` | Push to `main` | Calls the toolkit's `docker-publish.yml` (see [Release images](#release-images)) |
| `deploy-production` | Push to `main`, **only when `PRODUCTION_DEPLOY_ENABLED` is `true`** | Calls the toolkit's `vps-deploy.yml` (see [Production deployment](#production-deployment)) |
| `notify-deployment-failure` | When `deploy-production` fails or is cancelled | Posts a sanitized alert to `DEPLOY_ALERT_WEBHOOK_URL` |

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

The `release-config` job validates this file on every pull request (see
[Release configuration check](#release-configuration-check)). Keeping exactly these four components, and
keeping every Dockerfile mounting the `GITHUB_USERNAME`/`GITHUB_PAT` BuildKit secrets, are rules of this
repository that no automated check enforces; keep them by review.

### Publishing

`publish-images` calls `docker-publish.yml` at the same pinned toolkit SHA as `shared-ci`. It runs only
for `push` events on `main`, and only after `shared-ci` and `release-config` pass.
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

The toolkit checks the manifest against `.github/docker-images.json` before writing it, records each digest
from the push itself, and scans each image by digest from Docker Hub. It writes the immutable references to
the publish job's run summary. This repository does not re-verify the manifest.

### Failed or partial publishes

Images are pushed before they are scanned. If a component fails after another has pushed, its
`release-<sha>` tag is already taken, and re-running cannot reuse that identity. Investigate the cause,
then land a new commit on `main` to get a new deployment id. Never delete or move a `release-*` tag to
retry.

## Production deployment

> **Status: off.** `deploy-production` is skipped until the repository variable
> `PRODUCTION_DEPLOY_ENABLED` is set to `true`. Leave it unset until every item in
> [Activation checklist](#activation-checklist) is done.

### What happens on a deploy

`deploy-production` needs `publish-images`, so it only ever deploys a release whose four digests were
published and verified in the same run. It calls the toolkit's `vps-deploy.yml` at the pinned SHA, which runs
in the protected `production` GitHub Environment and then, on the VPS:

1. validates the request, the release manifest and the host's stack registration;
2. stages `deploy/compose.production.yml` plus a generated override that sets each release image by digest;
3. renders a new runtime-secret generation for every service (DEVOPS-007);
4. pulls the four images by digest (the VPS never builds);
5. runs `database-migrator` with `docker compose run` and requires exit `0` within 900 seconds. Any other
   result stops the deploy before any running service changes;
6. activates the new secret generations and recreates `webapi`, `consumer` and `jobs`;
7. waits up to 300 seconds for all three Compose health checks plus the host check
   `http://127.0.0.1:8080/health` to pass in the same round;
8. records the release as active only after health passes, and keeps the previous release's manifest and
   secret generations for restoration.

The deploy job passes `environment: production` and `stack-name: backendprojecttemplate-production`
inline. Everything else comes from [`.github/vps-deploy.production.json`](../.github/vps-deploy.production.json)
through the toolkit's `deploy-config-path` input:

| Setting | Value |
|---|---|
| `composeFiles` | `deploy/compose.production.yml` |
| `remoteDeploymentPath` | `/srv/backendprojecttemplate/production` |
| `componentServiceMap` | `migrator` → `database-migrator` (the other components keep their names) |
| `migrationService`, `migrationTimeoutSeconds` | `database-migrator`, `900` |
| `healthServices` | `webapi`, `consumer`, `jobs` |
| `healthChecks`, `healthTimeoutSeconds` | `http://127.0.0.1:8080/health`, `300` |
| `rollbackOnHealthFailure` | `true` |

Change these settings in the file, not in `ci.yml`: the toolkit rejects a setting declared in both places.

### Files in this repository

- `deploy/compose.production.yml`: the four release services. No images, tags or build sections. Every service
  runs with a read-only root filesystem (tmpfs for `/tmp` and `$HOME`), `no-new-privileges`, the image's
  non-root user and `restart: "no"`, and mounts only its own secret generation read-only at `/run/secrets/app`.
  Only WebAPI publishes a port, on `127.0.0.1:8080`, for the host reverse proxy. PostgreSQL, Redis and RabbitMQ
  are external managed services reached through the secret connection strings.
- `deploy/vps-deploy-stack.production.json`: the host stack registration. An operator installs it as
  `/etc/vps-deploy/stacks/backendprojecttemplate-production.json`. It allows only this repository, the four
  image repositories and the four runtime-secret instances (`bpt-prd-webapi`, `bpt-prd-consumer`,
  `bpt-prd-jobs`, and `bpt-prd-migrator`, which does not start at boot). Committing it does not register
  anything; the copy on the host is what counts.
- `deploy/runtime-secrets/<service>.keys.json`: the secret keys each service may receive.
- `deploy/stack.env.example`: non-secret placeholder values for the variables the Compose file requires,
  standing in for the host's `stack.env` when CI renders Compose. Add a variable here (and on the host) when
  the Compose file starts requiring it.

### Release configuration check

The `release-config` job calls the toolkit's `release-config-check.yml` on every event. It uses no secrets,
so it also runs for fork pull requests, and it publishes and deploys nothing. Using the same validation code
that publishing and deployment run later, it checks:

1. `.github/docker-images.json`: schema, repositories, Dockerfiles and package-auth component names;
2. the deployment settings in `.github/vps-deploy.production.json` against a release manifest with
   placeholder digests;
3. `deploy/vps-deploy-stack.production.json` against the deployment: stack, environment, path, allowed
   repository and images, health-check hosts;
4. `deploy/compose.production.yml`, rendered with `deploy/stack.env.example` the way the host renders it,
   against the host's Compose policy (digest-pinned images, ports, bind mounts, restart rules, the one-shot
   migrator, and each service mounting its own runtime-secret generation).

A pass does not guarantee a deploy: the host re-checks everything against its own registry, `stack.env` and
runtime-secret instances.

### Health checks

| Service | Check | What it proves |
|---|---|---|
| `webapi` | Compose `curl /health`, plus the host check `http://127.0.0.1:8080/health` | The process serves HTTP. `/health` has no dependency checks |
| `consumer` | Compose `curl /health/readiness` | Its readiness checks pass. Returns 503 when RabbitMQ is down |
| `jobs` | Compose `curl /health/readiness` | Redis is reachable. RabbitMQ is **not** checked |
| `database-migrator` | Exit code only | `0` means every migration stage succeeded |

A running container is not counted as healthy until its health check passes. Strengthening WebAPI and Jobs
readiness is an open application task.

### Rollback and the database

If rollout or health fails and a previous release exists, the toolkit restores the previous image digests
and secret generations, recreates the services, checks their health again, and still fails the job.
It does **not** reverse migrations, restore backups or touch the database. A restored release therefore runs
against the already-migrated schema, which is safe only if the release followed expand → migrate → contract:

- ship additive, backward-compatible schema changes first;
- move code onto the new structures in a later release;
- remove old structures only once no release within the rollback window needs them.

A first deployment that fails has nothing to restore and is left for diagnosis. Take regular PostgreSQL
backups outside this pipeline. See the toolkit's [database compatibility guidance](https://github.com/chukwuemekanweke/devops-toolkit/blob/190253ec766e10185629ef3e6e123bf3d603aadf/docs/vps-deployment.md#database-compatibility-expand-migrate-contract)
and the repository's [migration rules](architecture/database-migrations.md).

Data Protection keys are not persisted yet. Each deploy or restoration recreates the containers, which
invalidates Data-Protection-protected payloads such as Identity tokens and auth cookies. That is an open
application task.

### When a deploy fails

1. Read the alert, or the job outputs: `deployment-outcome`, `failed-phase`, `migration-status`,
   `restoration-outcome`.
2. Download the sanitized `vps-deployment-record-…` artifact from the run.
3. On the host, inspect `docker compose --project-name backendprojecttemplate-production ps`, the container
   logs, and the stack's remote state and history.
4. Fix forward with a new commit. To return to a known good release, redeploy its deployment ID through the
   workflow. Never move `release-*` tags or edit host state by hand.

The toolkit's [recovery table](https://github.com/chukwuemekanweke/devops-toolkit/blob/190253ec766e10185629ef3e6e123bf3d603aadf/docs/vps-deployment.md#recovery) covers each outcome.

### GitHub configuration

| Item | Where | Notes |
|---|---|---|
| `production` environment | Repository settings → Environments | Required reviewers; deployment branches limited to `main` |
| `VPS_HOST`, `VPS_DEPLOY_USER`, `VPS_DEPLOY_SSH_KEY`, `VPS_KNOWN_HOSTS` | `production` environment secrets | Dedicated deploy account bound by a forced command to this stack. Known hosts recorded out of band, never with `ssh-keyscan` over the network |
| `DOCKERHUB_READ_USERNAME`, `DOCKERHUB_READ_TOKEN` | `production` environment secrets | Pull-only Docker Hub token for the four repositories |
| `DEPLOY_ALERT_WEBHOOK_URL` | Repository secret | Used only by `notify-deployment-failure`. Never sent to the VPS |
| `PRODUCTION_DEPLOY_ENABLED` | Repository variable | Set to `true` only after the activation checklist passes |

GitHub resolves the environment secrets when the toolkit's environment-scoped job starts. The caller maps
them by name and never uses `secrets: inherit`. Confirm that resolution on the first rehearsal deploy.

### Host setup

Follow the toolkit's [VPS deployment operations guide](https://github.com/chukwuemekanweke/devops-toolkit/blob/190253ec766e10185629ef3e6e123bf3d603aadf/docs/vps-deployment.md#initial-vps-preparation) and
[runtime-secret bootstrap](https://github.com/chukwuemekanweke/devops-toolkit/blob/190253ec766e10185629ef3e6e123bf3d603aadf/docs/doppler/vps-bootstrap.md). For this stack:

- install the runtime-secrets and vps-deploy helpers from toolkit commit `190253e` (the pinned revision);
- create the four DEVOPS-007 instances `bpt-prd-webapi`, `bpt-prd-consumer`, `bpt-prd-jobs` and
  `bpt-prd-migrator`, each with application `backendprojecttemplate`, environment `prd`, its own Doppler config
  and service group, `outputDirectory` `/run/runtime-secrets/backendprojecttemplate/prd/<service>`, and the
  matching `deploy/runtime-secrets/<service>.keys.json` as its key manifest;
- install `deploy/vps-deploy-stack.production.json` as root-owned
  `/etc/vps-deploy/stacks/backendprojecttemplate-production.json`;
- write a root-owned `/srv/backendprojecttemplate/production/stack.env` with `RUNTIME_SECRETS_APPLICATION`,
  `RUNTIME_SECRETS_ENVIRONMENT`, the four `*_SECRETS_GID` values, and the non-secret settings the services read
  (for example `RABBITMQ_HOSTNAME`, `OTEL_EXPORTER_OTLP_ENDPOINT`, R2 bucket names and feature flags). Never put
  secret values in it;
- put a TLS reverse proxy in front of `127.0.0.1:8080`.

### Activation checklist

Deployment stays off until all of these are true:

- [ ] Host helpers installed, stack registered and `stack.env` written on a **non-production** VPS first.
- [ ] Runtime-secret instances render for all four services, and sibling and cross-environment access is denied.
- [ ] `production` environment configured with reviewers, branch rules and the six secrets.
- [ ] Rehearsal on the non-production VPS, recording the run for each scenario:
  - a successful deploy;
  - a migration failure (services unchanged);
  - a health failure that restores the previous release;
  - a restoration failure, reported explicitly;
  - a failed first deploy (no false restoration);
  - two concurrent deploys (locking);
  - an interrupted deploy.
- [ ] `DEPLOY_ALERT_WEBHOOK_URL` set, and an alert received during the rehearsal.
- [ ] An authorized operator approves activation, then sets `PRODUCTION_DEPLOY_ENABLED=true`.

### New projects from this template

Change the Docker Hub namespace in `.github/docker-images.json`. Then update to your own values: the stack name,
deployment path, instance IDs and allowed repositories in `deploy/vps-deploy-stack.production.json`, the
matching `remoteDeploymentPath` in `.github/vps-deploy.production.json`, the `RUNTIME_SECRETS_APPLICATION`
value in `deploy/stack.env.example`, and the `stack-name` passed by both `release-config` and
`deploy-production` in `ci.yml`. The `release-config` check fails until these agree.

## Credentials

| Purpose | Repository secrets | Where they go |
|---|---|---|
| Private NuGet (Mailtrap on GitHub Packages) | `GH_PKG_USERNAME`, `GH_PKG_PAT` | `shared-ci`, mapped to `nuget-username` / `nuget-token`. The toolkit exposes them as `GITHUB_USERNAME` / `GITHUB_PAT` (referenced by `nuget.config`) only to restore steps |
| Docker build package restore | `GH_PKG_USERNAME`, `GH_PKG_PAT` | `publish-images`, mapped to `github-packages-username` / `github-packages-token` and mounted as BuildKit secrets |
| Docker Hub push | `DOCKERHUB_USERNAME`, `DOCKERHUB_TOKEN` | `publish-images`, mapped to `registry-username` / `registry-token`, `main` pushes only |
| VPS deploy and image pull | `production` environment secrets (see [GitHub configuration](#github-configuration)) | `deploy-production` only |

Callers map each secret by name and never use `secrets: inherit`. Workflow-level permissions are `{}`,
and each job is granted only `contents: read` (the alert job gets none). GitHub withholds secrets from fork pull requests, so
the Mailtrap restore fails for forks. This is a known limitation, and it is not worked around with
`pull_request_target`. Runtime application secrets never enter GitHub Actions: they are rendered on the VPS by DEVOPS-007.

## Toolkit reference pinning

The toolkit has no published release tag yet, so callers pin a reviewed full commit SHA (currently
`190253ec766e10185629ef3e6e123bf3d603aadf`, toolkit `main` with `release-config-check.yml` and the
deployment configuration file, #15).
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
