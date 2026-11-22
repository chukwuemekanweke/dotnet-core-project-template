---
type: workflow
title: CI/CD Ownership and Shared Toolkit Workflows
description: Application-vs-devops-toolkit responsibility split, the SHA-pinned ci.yml callers, release image publishing, and known CI pitfalls
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
    - .github/scripts/verify-release-images.py
    - .github/docker-images.json
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

- `.github/docker-images.json` (toolkit schema v1) declares exactly `webapi`, `consumer`, `jobs`,
  `migrator` → `docker.io/chukwuemekanweke/backendprojecttemplate-<component>`, root context, no
  `buildArgs`. `verify-release-images.py config` enforces this on every PR; keep the script's
  `EXPECTED_COMPONENTS` in step with the file when the deployment contract intentionally changes.
- `publish-images` (in `ci.yml`) calls `docker-publish.yml` only on `push` to `main` after `shared-ci`
  and `image-config`; `deployment-id: ${{ github.sha }}` → write-once tag `release-<sha>`. No `latest`
  or other moving tag is published. All four components are in `package-auth-components` because every Dockerfile
  mounts BuildKit secrets `GITHUB_USERNAME`/`GITHUB_PAT` for the Mailtrap restore — never build args.
- `verify-release` checks `manifest-json` equals the `release-manifest-<sha>` artifact, the manifest
  matches the config (components, repositories, deployment id, source commit) and every `image@digest`
  resolves. Deployments consume the manifest digests, never tags.
- A failed/partial publish burns that sha's `release-*` tags; fix and land a new commit. Never delete or
  move a `release-*` tag.
- Secret values here overlap public names (`GH_PKG_USERNAME`/`DOCKERHUB_USERNAME` = `chukwuemekanweke`
  = Docker Hub namespace = repo owner). GitHub drops any job output containing a referenced secret
  ("Skip output … may contain secret"), which emptied the publish matrix at toolkit `4ab2e18`; fixed in
  toolkit `a7a4f80` (#13). `vps-deploy.yml` outputs moved to a secret-free `report` job in `1891bbf` (#14);
  callers pin `1891bbf` for all three workflows.

## CI pitfalls

- `ci.yml` is the only CI workflow; it is a thin caller (`shared-ci`, `image-config`, `publish-images`,
  `verify-release`). The legacy self-contained jobs and the temporary parity check were retired after
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
  (server-defaulted allocation start vs clock: Active/Pending boundary). Legacy history also shows
  admin-payments reporting and passkey-ceremony flakes.
