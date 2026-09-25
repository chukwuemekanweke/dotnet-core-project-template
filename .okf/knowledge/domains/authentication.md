---
type: domain
title: Authentication Domain
description: Sign-up/sign-in, TOTP MFA and recovery codes, sessions and refresh tokens, Google sign-in, password reset, session revocation, and the ASP.NET Core Identity base
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
authenticator-app MFA and recovery codes, login-activity recording/history and IP-address-location enrichment, and active-session/device management.

## Foundation

ASP.NET Core Identity is the authentication base (see [[dotnet-style]]) — do not reintroduce custom password hashing, token issuance, or credential-check flows when Identity already covers the need.
Sessions and refresh tokens are modeled as first-class domain entities (`AuthenticationSession`, `AuthenticationRefreshToken`) alongside Identity rather than replacing Identity's user/credential
model.

## Authenticator MFA boundary

ASP.NET Core Identity owns the authenticator key, TOTP verification, two-factor enabled flag, and
hashed single-use recovery codes. The application does not maintain a parallel MFA user store or
implement TOTP cryptography.

Password and Google first factors converge on a five-minute, five-attempt `TwoFactorChallenge`
stored server-side through Redis-backed `IJsonCache`. The client receives only a cryptographically
random opaque token. A challenge captures the user, stakeholder, tenant, original password/Google
method, actor context, expiry, and original IP/user-agent needed for final session creation. Cache
take-and-remove plus terminal markers detects subsequent replay. The preceding Google flow is
consumed after the MFA challenge assumes continuation ownership.

For MFA-enabled users, first-factor success does not create an `AuthenticationSession`, issue tokens,
or publish `UserSignInSuccessful`; it returns `two_factor_required`.
`CompleteTwoFactorChallengeHandler` rechecks tenant, account, lockout, email confirmation, and MFA
state, verifies an Identity authenticator token or redeems an Identity recovery code, and only then
calls `AuthenticationSessionIssuer` and publishes normal sign-in success. Invalid proofs increment
Identity failed access and consume one challenge attempt.

MFA management uses the active-session policy. Setup reads/creates Identity's authenticator key without
replacing an in-progress key. Enrollment, recovery-code regeneration, and disable update the security
stamp and revoke active application sessions; disable also resets the authenticator key. Recovery
codes are returned only when generated and are never copied into application persistence or cache.
Refresh-token rotation remains the existing transparent session flow and never requests MFA again;
its security-stamp check rejects credentials invalidated by MFA security changes.

## Google authentication continuation

Google authentication is a single entry point backed by a 5-10 minute Redis `IJsonCache` flow.
The flow binds the Google ID token to a cryptographically random nonce and stores the validated
`sub`, email, `email_verified`, hosted domain, tenant, continuation state and expiry server-side.
Flow data uses the existing cache take-and-remove operation before use, and terminal flows retain a
short consumed marker for replay detection.

`GoogleSignInHandler` returns one of four primary outcomes: a normal `AuthenticationSession` and
tokens for an existing `Google` external login with MFA disabled, `two_factor_required` for an
MFA-enabled linked account, `link_required` for a matching password account, or
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
