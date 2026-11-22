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

## Current migration state (DEVOPS-012)

- `ci.yml` remains authoritative and is the only workflow that publishes images. Do not remove its
  verification jobs until shared-CI parity is recorded.
- `ci-shared-validation.yml` is a temporary comparison caller of `dotnet-ci.yml` with a `parity-check`
  job (`verify-shared-ci-artifacts.py`) asserting the same project set as `ci.yml`'s discovery, zero
  skipped/failed tests, and parseable coverage. It never publishes or deploys. Retire the script with it.
- Integration fixtures must keep failing (not skipping) on `DockerUnavailableException` with a message
  containing "Docker is unavailable" — the toolkit's TRX scan matches that text.
- Prerequisite: the toolkit's reusable workflows and helper checkout run under the caller's
  `GITHUB_TOKEN`, so a public caller requires a public toolkit. Do not add a PAT to work around it.
