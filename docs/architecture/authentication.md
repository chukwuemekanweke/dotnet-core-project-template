# Authentication and sessions

The backend uses ASP.NET Core Identity for users, passwords, lockout, email confirmation, and external-login associations. Successful authentication creates an `AuthenticationSession`, then issues the normal JWT access token and rotating refresh token. Google authentication extends this lifecycle; it does not create a separate session model.

## Google authentication lifecycle

The BFF starts an attempt with `POST /api/v1/authentication/google/flows`. The response contains an opaque flow token, a cryptographically random nonce, and an expiry. The BFF stores the flow token in an HttpOnly cookie and gives only the nonce to Google Identity Services. Flow data is stored as short-lived JSON in Redis for ten minutes; Google ID tokens are never stored.

The BFF submits the flow token and Google ID token to `POST /api/v1/authentication/sessions/google`. The backend atomically takes the flow, validates the ID token signature, issuer, allowed audience, lifetime, required claims, and nonce, then chooses one outcome:

- `authenticated`: the Google `sub` is already linked. The backend creates the normal application session and returns access and refresh tokens.
- `link_required`: no external login exists, but an Identity user has the validated Google email. Matching email alone never links accounts.
- `registration_required`: neither the Google `sub` nor validated email belongs to an application account.

Continuation and terminal operations atomically take the Redis flow. A consumed marker remains briefly after expiry so replay, expiry, and unknown tokens produce distinct stable codes: `google_flow_consumed`, `google_flow_expired`, and `google_flow_invalid`.

## Existing-account linking

`POST /api/v1/authentication/google-links` accepts the flow token and the existing account password. The server uses only the validated Google identity stored in the `link_required` flow. Password verification uses Identity failed-access and reset APIs, including configured lockout. After proof succeeds, `AddLoginAsync` associates provider `Google` with Google's immutable `sub`; the password remains usable. A normal session is returned when the account email is confirmed.

## Google registration and email authority

`POST /api/v1/authentication/registrations/google` accepts the `registration_required` flow token plus the remaining profile fields. Email and Google `sub` always come from the server-side flow. Gmail/Googlemail identities and Workspace identities with an `hd` claim are treated as authoritative when `email_verified` is true, so registration confirms the email and returns a normal session immediately. A verified Google account backed by an external mailbox is not treated as authoritative: the account is created, the existing email-confirmation process continues, and the API returns `email_verification_required` without tokens. That 403 Problem Details response includes the backend-authoritative `email` and the email-confirmation expiry as `retryAtUtc`, allowing the confirmation screen to continue without asking for the address again.

## Configuration

Google authentication uses `Authentication:Google`:

```json
{
  "Enabled": true,
  "ClientIds": ["web-client-id.apps.googleusercontent.com"],
  "FlowLifetime": "00:10:00"
}
```

Startup fails when Google authentication is enabled without at least one non-empty client ID, or when the flow lifetime is outside five to ten minutes. Multiple client IDs are allowed. No Google client secret is used.
