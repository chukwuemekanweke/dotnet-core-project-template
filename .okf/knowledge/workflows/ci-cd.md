---
type: workflow
title: CI/CD Ownership and Shared Toolkit Workflows
description: Application-vs-devops-toolkit responsibility split, pinned reusable-workflow callers, and the DEVOPS-012 migration state
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
    - .github/workflows/ci-shared-validation.yml
    - .github/scripts/verify-shared-ci-artifacts.py
    - .github/scripts/verify-release-images.py
    - .github/docker-images.json
    - nuget.config
sources:
    - kind: file
      path: .github/workflows/ci-shared-validation.yml
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

- `.github/docker-images.json` (toolkit schema v1) declares exactly `webapi`, `consumer`, `jobs`,
  `migrator` → `docker.io/chukwuemekanweke/backendprojecttemplate-<component>`, root context, no
  `buildArgs`. `verify-release-images.py config` enforces this on every PR; keep the script's
  `EXPECTED_COMPONENTS` in step with the file when the deployment contract intentionally changes.
- `publish-images` (in `ci-shared-validation.yml`) calls `docker-publish.yml` only on `push` to `main`
  after `shared-ci`, `parity-check` and `image-config`; `deployment-id: ${{ github.sha }}` → write-once
  tag `release-<sha>`. All four components are in `package-auth-components` because every Dockerfile
  mounts BuildKit secrets `GITHUB_USERNAME`/`GITHUB_PAT` for the Mailtrap restore — never build args.
- `verify-release` checks `manifest-json` equals the `release-manifest-<sha>` artifact, the manifest
  matches the config (components, repositories, deployment id, source commit) and every `image@digest`
  resolves. Deployments consume the manifest digests, never tags.
- A failed/partial publish burns that sha's `release-*` tags; fix and land a new commit. Never delete or
  move a `release-*` tag.

## Current migration state (DEVOPS-012)

- `ci.yml` still runs its own verification and legacy `docker-build-push` matrix (`:latest`/`:<sha>`
  tags). Retire both only after `publish-images` + `verify-release` succeed on `main`.
- `ci-shared-validation.yml` calls `dotnet-ci.yml` with a `parity-check` job
  (`verify-shared-ci-artifacts.py`) asserting the same project set as `ci.yml`'s discovery, zero
  skipped/failed tests, and parseable coverage; retire that script when `ci.yml` is retired. It never
  deploys.
- Integration fixtures must keep failing (not skipping) on `DockerUnavailableException` with a message
  containing "Docker is unavailable" — the toolkit's TRX scan matches that text.
- Parity was demonstrated on PR #68 (commit `5c260b2`): legacy run 45024354827 and shared run
  45024355193 attempt 2 both ran 7 unit projects (609 tests) and 5 integration projects (104 tests),
  with 0 skipped tests in each.
- `devops-toolkit` must stay public. GitHub rejects a public caller's reference to a private reusable
  workflow ("workflow was not found"), and the toolkit's helper checkout uses the caller's
  `GITHUB_TOKEN`. Do not add a PAT to work around it.
- Re-run all jobs of a failed shared run, never only the failed ones: the toolkit combines artifacts by
  run-attempt name, so a partial re-run drops the earlier attempt's unit TRX.
- `When_GrantingEntitlements_Concurrently_Should.AllowExactlyOneOverlappingEntitlementAndOneAllocationPerIdempotencyKey`
  is a pre-existing intermittent failure in both workflows (an idempotency-key race). It is not a CI
  parity difference.
