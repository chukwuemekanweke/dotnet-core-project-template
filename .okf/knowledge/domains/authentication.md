---
type: domain
title: Authentication Domain
description: Sign-up/sign-in, sessions and refresh tokens, login activity history, Google sign-in, password reset, session revocation, and the ASP.NET Core Identity base
resource: okf://knowledge/domains/authentication
tags:
    - context
    - domain
    - authentication
    - identity
    - sessions
governance: context
code_refs:
    - src/BackendProjectTemplate.Application/Authentication/**
    - src/BackendProjectTemplate.Domain/Authentication/**
    - src/BackendProjectTemplate.Infrastructure/Authentication/**
    - src/BackendProjectTemplate.Infrastructure/Persistence/LoginActivityReadModelRepository.cs
    - src/BackendProjectTemplate.WebAPI/Features/Authentication/**
    - src/BackendProjectTemplate.WebAPI/Features/Stakeholders/LoginActivity/**
    - src/BackendProjectTemplate.Consumer/Authentication/**
    - src/BackendProjectTemplate.Jobs/Authentication/**
    - src/BackendProjectTemplate.Contracts/Commands/Authentication/**
    - tests/unit/BackendProjectTemplate.Application.UnitTests/Authentication/**
    - tests/unit/BackendProjectTemplate.WebAPI.UnitTests/Features/Stakeholders/LoginActivity/**
    - tests/integration/BackendProjectTemplate.WebAPI.IntegrationTests/Authentication/**
    - tests/integration/BackendProjectTemplate.WebAPI.IntegrationTests/Stakeholders/LoginActivity/**
sources:
    - kind: file
      path: src/BackendProjectTemplate.Domain/Authentication/Entities/AuthenticationSession.cs
generated:
    at: "2026-09-23T00:00:00Z"
    by: human
status: stable
---
## Scope

Sign-up (with email OTP confirmation), sign-in (including Google sign-in), session lifecycle (list/refresh/logout/revoke-other/revoke-single sessions), password change/reset, email-existence checks,
login-activity recording/history and IP-address-location enrichment, and active-session/device management.

## Foundation

ASP.NET Core Identity is the authentication base (see [[dotnet-style]]) — do not reintroduce custom password hashing, token issuance, or credential-check flows when Identity already covers the need.
Sessions and refresh tokens are modeled as first-class domain entities (`AuthenticationSession`, `AuthenticationRefreshToken`) alongside Identity rather than replacing Identity's user/credential
model.

## Google authentication continuation

Google authentication is a single entry point backed by a 5-10 minute Redis `IJsonCache` flow.
The flow binds the Google ID token to a cryptographically random nonce and stores the validated
`sub`, email, `email_verified`, hosted domain, tenant, continuation state and expiry server-side.
Flow data is atomically taken before use and terminal flows retain a short consumed marker for replay detection.

`GoogleSignInHandler` returns one of three primary outcomes: a normal `AuthenticationSession` and
tokens for an existing `Google` external login, `link_required` for a matching password account, or
`registration_required` for a new account. Matching email never links automatically.
`LinkGoogleAccountHandler` requires the existing password and uses Identity
failed-access/reset/lockout APIs before `AddLoginAsync`. Google registration consumes only validated
flow identity; Gmail and Workspace (`hd`) email is authoritative, while external-mailbox Google
accounts continue through normal email confirmation.
Because `AppUser` and active `Stakeholder` are one-to-one, authentication resolves a stakeholder by
`AppUserId`; Google flows then validate the resolved stakeholder's tenant explicitly instead of using
tenant identity as an additional stakeholder lookup key.
For that non-authoritative-email registration path, the `email_verification_required` 403 Problem
Details response includes the server-side flow `email` and the confirmation expiry as `retryAtUtc`;
clients must use this continuation metadata instead of asking the user to re-enter the Google email.

## Cross-cutting

- Sign-up, email confirmation, and access-token refresh publish `Contracts` events (`UserCreated`, `UserEmailConfirmed`, `UserAccessTokenRefreshed`, `UserSignInSuccessful`, `UserSignInFailed`) through
  the outbox — see [[async-processing]]. `Consumer` handlers react to these (e.g. IP-location enrichment, welcome email).
- Follows the standard [[vertical-slices]] feature-folder shape and the [[unit-testing]]/[[integration-testing]] conventions.
