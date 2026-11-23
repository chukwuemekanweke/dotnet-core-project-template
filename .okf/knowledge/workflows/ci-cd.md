---
type: workflow
title: CI/CD Ownership and Shared Toolkit Workflows
description: Application-vs-devops-toolkit responsibility split, the SHA-pinned ci.yml callers, release image publishing, gated VPS deployment, and known CI pitfalls
resource: okf://knowledge/workflows/ci-cd
tags:
    - constraint
    - workflow
    - ci
    - github-actions
    - devops-toolkit
governance: constraint
code_refs:
    - .github/workflows/ci.yml
    - .github/docker-images.json
    - .github/vps-deploy.production.json
    - deploy/stack.env.example
    - deploy/compose.production.yml
    - deploy/vps-deploy-stack.production.json
    - .github/vps-deploy.development.json
    - deploy/compose.development.yml
    - deploy/vps-deploy-stack.development.json
    - deploy/stack.development.env.example
    - nuget.config
sources:
    - kind: file
      path: .github/workflows/ci.yml
generated:
    at: "2026-11-22T00:00:00Z"
    by: agent
status: draft
---
## Ownership rule

This repository owns *configuration* (solution path, test globs, Dockerfile paths, image names,
production Compose, runtime secret key lists, triggers, environments, health checks, notifications).
`chukwuemekanweke/devops-toolkit` owns *automation* (restore/format/build, test discovery and execution,
TRX/coverage, image build/scan/publish, release manifests, VPS deployment, migrations, health gating,
restoration). Do not copy toolkit logic into `.github/`; change shared behaviour in the toolkit with its
own tests.

## Caller rules

- Reference toolkit workflows by full 40-char commit SHA (or a verified immutable release tag once one
  exists). Never `@main`, never an unverified `@v1`. Keep all callers on the same toolkit revision.
- Workflow-level `permissions: {}`; grant each job only what the called workflow declares.
- Map every secret by name (`nuget-username: ${{ secrets.GH_PKG_USERNAME }}` …). Never
  `secrets: inherit`, never `pull_request_target` with secrets, never runtime application secrets.
- Private NuGet credentials (`GH_PKG_USERNAME`/`GH_PKG_PAT`) surface as `GITHUB_USERNAME`/`GITHUB_PAT`
  for `nuget.config` and as the same BuildKit secret IDs in all four Dockerfiles.
- The four deployable component IDs are fixed: `webapi`, `consumer`, `jobs`, `migrator`.

## Release images

- `.github/docker-images.json` (toolkit schema v1) declares `webapi`, `consumer`, `jobs`,
  `migrator` → `docker.io/chukwuemekanweke/backendprojecttemplate-<component>`, root context, no
  `buildArgs`.
- `release-config` (in `ci.yml`) calls the toolkit's secret-free `release-config-check.yml` on every event.
  With the toolkit's own publish/deploy validation code it checks `docker-images.json`,
  `.github/vps-deploy.production.json`, `deploy/vps-deploy-stack.production.json`, and
  `deploy/compose.production.yml` rendered with `deploy/stack.env.example` against the host Compose
  policy. There is no repo-side validation script any more; do not reintroduce one — put generic checks
  in the toolkit. The toolkit no longer enforces this repo's own rules (exactly four components, no
  non-release services in production Compose, migrator only in the `migration` profile, health services
  having a Compose healthcheck); keep those by review.
- `publish-images` (in `ci.yml`) calls `docker-publish.yml` only on `push` to `main` after `shared-ci`
  and `release-config`; `deployment-id: ${{ github.sha }}` → write-once tag `release-<sha>`. No `latest`
  or other moving tag is published. All four components are in `package-auth-components` because every Dockerfile
  mounts BuildKit secrets `GITHUB_USERNAME`/`GITHUB_PAT` for the Mailtrap restore — never build args.
- The toolkit verifies the manifest against the config, records each pushed digest, scans each image by
  digest, and writes the `image@digest` references to the publish job summary; there is no caller-side
  re-verification job. Deployments consume the manifest digests, never tags.
- A failed/partial publish burns that sha's `release-*` tags; fix and land a new commit. Never delete or
  move a `release-*` tag.
- Secret values here overlap public names (`GH_PKG_USERNAME`/`DOCKERHUB_USERNAME` = `chukwuemekanweke`
  = Docker Hub namespace = repo owner). GitHub drops any job output containing a referenced secret
  ("Skip output … may contain secret"), which emptied the publish matrix at toolkit `4ab2e18`; fixed in
  toolkit `a7a4f80` (#13). `vps-deploy.yml` outputs moved to a secret-free `report` job in `1891bbf` (#14);
  callers pin `190253e` (release-config-check and the deploy config file, #15) for all four workflows.

## Production deployment

- `deploy-production` calls `vps-deploy.yml` (same toolkit SHA) after `publish-images`,
  only on push to `main` AND `vars.PRODUCTION_DEPLOY_ENABLED == 'true'`. It is deliberately off; do not
  enable it or weaken the condition — activation needs the non-production rehearsal and operator approval
  listed in docs/ci-cd.md. It deploys `manifest-artifact` (never reconstructed tags). Its topology lives in
  `.github/vps-deploy.production.json` (`deploy-config-path`): maps `migrator`→`database-migrator`, runs
  migration service `database-migrator`, and checks `webapi`,`consumer`,`jobs` plus
  `http://127.0.0.1:8080/health`. Only `environment` and `stack-name` stay inline; `release-config` must
  pass the same `deploy-config-path`, `environment` and `stack-name`. Never also set a file-owned input
  (`compose-files`, `health-checks`, …) inline: the toolkit rejects a setting declared in both places.
- VPS/registry credentials are `production` environment secrets mapped by name; the alert webhook
  (`DEPLOY_ALERT_WEBHOOK_URL`) is a repository secret used only by `notify-deployment-failure`.
- `deploy/vps-deploy-stack.production.json` is a repository copy of the host registry; the host file wins,
  so update both together. `deploy/stack.env.example` must list every variable the production Compose file
  requires (`${VAR:?…}`) with non-secret placeholders — a new required variable fails `release-config`
  until it is added there and to the host `stack.env`. Infrastructure stays external (managed
  PostgreSQL/Redis/RabbitMQ); an added infrastructure service must be digest-pinned and pass the host
  policy.
- Restoration restores images and secret generations only, never the schema: migrations must stay
  expand → migrate → contract compatible with the previous release.

## Development deployment

- `deploy-development` mirrors `deploy-production` for stack `backendprojecttemplate-development`
  (GitHub Environment `development`, `ssh-port: 35340`), gated by `vars.DEVELOPMENT_DEPLOY_ENABLED == 'true'`.
  `release-config-development` checks its files on every event and `publish-images` needs it. Keep its
  `deploy-config-path`/`environment`/`stack-name` identical to `deploy-development`.
- It shares one VPS with the unrelated `pickly` stack, so `deploy/compose.development.yml` differs from
  production only where the host forces it: WebAPI on `127.0.0.1:8090` (pickly owns `:8080`), every service
  also joins the pre-created internal external network `shared-postgres` (alias `postgres` →
  `pickly-postgres-1`; the only entry in the registry's `allowedExternalNetworks`), and the OTLP endpoint
  defaults to empty (exporters off) because there is no local `otel-collector`.
- The host Caddy (`template-api.chidelu.com` → `127.0.0.1:8090`) reaches WebAPI through Docker's port
  publishing, so requests arrive from the `app` network gateway. That network is pinned to
  `172.30.231.0/24` and WebAPI sets `ForwardedHeaders__KnownProxies__0: 172.30.231.1`; keep the two in
  step, or client IPs (rate-limit partitions, audit IPs) collapse to the gateway.
- Docker cannot change an existing network's IPAM (subnet/gateway) or remove it while containers use it; a
  changed definition fails the deploy at the migration `docker compose run` ("migration container could not
  be started"). Change network IPAM only under a **new network name**, so Compose creates it and the rollout
  recreates containers onto it; the old network stays for restoring the previous release. (`private` →
  `app` for the pinned subnet.)
- The development *deployment* still runs the .NET hosts as `Production`: only Production loads the
  `/run/secrets/app` KeyPerFile secrets (see [[production-configuration]]). Never switch that stack to
  `Development` without changing the secret bootstrap.
- Non-secret R2 settings (endpoint, application folder, bucket names, public base URL) are Compose defaults in
  `deploy/compose.development.yml` for WebAPI and Consumer, overridable from `stack.env` `CLOUDFLARE_R2_*`. Only
  the R2 access key pair is a Doppler runtime secret. An empty non-secret value fails every storage call with
  "Cloudflare R2 configuration is incomplete", because the seeded active `FileStorage` provider is `cloudflare_r2`.
- Runtime-secret instances `bpt-dev-{webapi,consumer,jobs,migrator}` (Doppler project
  `backend-project-template`, configs `dev_*`, groups 21001–21004, environment segment `dev`);
  `deploy/stack.development.env.example` must carry the same group ids as the host `stack.env`.

## CI pitfalls

- `ci.yml` is the only CI workflow; it is a thin caller (`shared-ci`, `release-config`, `publish-images`,
  `deploy-production`) plus the caller-owned `notify-deployment-failure` alert (alerting stays in the
  caller by toolkit design). The legacy self-contained jobs and the temporary parity check were retired after
  parity (PR #68: 7 unit projects/609 tests, 5 integration projects/104 tests, 0 skipped) and the first
  verified toolkit release (`4fdbdf5`).
- Integration fixtures must keep failing (not skipping) on `DockerUnavailableException` with a message
  containing "Docker is unavailable" — the toolkit's TRX scan matches that text.
- `devops-toolkit` must stay public. GitHub rejects a public caller's reference to a private reusable
  workflow ("workflow was not found"), and the toolkit's helper checkout uses the caller's
  `GITHUB_TOKEN`. Do not add a PAT to work around it.
- Re-run all jobs of a failed run, never only the failed ones: the toolkit combines artifacts by
  run-attempt name, so a partial re-run drops the earlier attempt's TRX/coverage.
- Known intermittent WebAPI integration failures (timing/race sensitive, not CI defects):
  `When_GrantingEntitlements_Concurrently_Should.AllowExactlyOneOverlappingEntitlementAndOneAllocationPerIdempotencyKey`
  (idempotency-key race) and
  `When_ReportingAdminSubscriptionAnalytics_Should.AuthorizeFilterAndExcludeComplimentaryAllocationRevenue`
  (server-defaulted allocation start vs clock: Active/Pending boundary), and the Jobs unit test
  `When_ProcessingOutboxMessages_WithPendingMessage_Should.MarkMessageAsSent` (fails under full-solution
  parallel load, passes in isolation). Legacy history also shows
  admin-payments reporting and passkey-ceremony flakes.
