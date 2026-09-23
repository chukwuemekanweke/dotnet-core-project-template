---
type: domain
title: Stakeholders Domain
description: Stakeholder profile, preferences (language/account settings), and avatar upload
resource: okf://knowledge/domains/stakeholders
tags:
    - context
    - domain
    - stakeholders
    - profile
governance: context
code_refs:
    - src/BackendProjectTemplate.Application/Stakeholders/**
    - src/BackendProjectTemplate.Application/Authentication/Stakeholders/**
    - src/BackendProjectTemplate.Domain/Stakeholders/**
    - src/BackendProjectTemplate.WebAPI/Features/Stakeholders/**
    - tests/unit/BackendProjectTemplate.Application.UnitTests/Stakeholders/**
    - tests/integration/BackendProjectTemplate.WebAPI.IntegrationTests/Stakeholders/**
sources:
    - kind: file
      path: src/BackendProjectTemplate.Domain/Stakeholders/Entities/Stakeholder.cs
generated:
    at: "2026-09-23T00:00:00Z"
    by: human
status: stable
---
## Scope

Stakeholder profile (get/update), account preferences including language selection (get/update), and avatar upload (create/complete avatar upload, using the presigned-upload flow in [[file-storage]]).
`Stakeholder` read models back the profile/preferences read paths (see [[persistence]]).

A `Stakeholder` represents the actor identity used across observability (`StakeholderId`, see [[observability]]) and is distinct from `AppUser`/Identity, which owns credentials — see [[authentication]].

Note: `BackendProjectTemplate.Modules.Stakeholders` is an early scaffold project with no implementation yet; do not treat it as the current home for stakeholder logic until it is actually populated.

Follows the standard [[vertical-slices]] feature-folder shape and the [[unit-testing]]/[[integration-testing]] conventions.
