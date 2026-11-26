# Administrative audit history

The administrative audit journal is a durable, append-only record of privileged administrative actions:
who did what, to which resource, why, when, what changed, which tenant was affected and which request it
came from. It backs the admin portal's audit screen (FE-UI-015) and its CSV export.

It records administrative business actions, not database changes.

## Scope

**Recorded:** successfully committed, state-changing privileged mutations performed by an authenticated
person through an administrative capability. This includes delegated roles such as FinanceOfficer when they
allocate or cancel subscriptions.

**Not recorded:**

- registration, sign-in, profile, preference and password changes, including when an administrator makes them
  for their own account;
- customer purchases and cancellations, payments and webhooks;
- Consumer and Jobs work: renewals, expirations, scheduled activations and other automated transitions;
- reads and dashboard visits;
- failed, denied or validation-rejected requests;
- no-ops (for example locking an already locked user) and idempotent replays (for example a repeated
  allocation with the same idempotency key).

### Coverage

| Endpoint | Action | Target type | Target tenant | Reason |
| --- | --- | --- | --- | --- |
| `POST /admin/users/{id}/lock` | `users.locked` | `User` | global | required |
| `POST /admin/users/{id}/unlock` | `users.unlocked` | `User` | global | required |
| `PUT /admin/users/{id}/roles/{role}` | `users.roles.assigned` | `User` | global | required |
| `DELETE /admin/users/{id}/roles/{role}` | `users.roles.removed` | `User` | global | required |
| `DELETE /admin/users/{id}/sessions/{sessionId}` | `users.sessions.revoked` | `AuthenticationSession` | session owner's tenant | required |
| `DELETE /admin/users/{id}/sessions` | `users.sessions.revoked-all` | `User` (one record) | global | required |
| `POST /admin/subscriptions/allocations` | `subscriptions.allocated` | `Subscription` | subscription tenant | required |
| `POST /admin/subscriptions/{id}/immediate-cancellation` | `subscriptions.cancelled` | `Subscription` | subscription tenant | required |
| `POST /admin/subscriptions/{id}/cancellation-at-period-end` | `subscriptions.cancellation-scheduled` | `Subscription` | subscription tenant | required |
| `POST /admin/subscriptions/{id}/purchase-fulfillment-retries` | `subscriptions.purchase-fulfillment-retry-requested` | `Subscription` | subscription tenant | optional (new) |
| `POST /admin/subscription-plans` | `subscription-plans.created` | `SubscriptionPlan` | global | optional (new) |
| `PUT /admin/subscription-plans/{id}` | `subscription-plans.updated` | `SubscriptionPlan` | global | optional (new) |
| `PUT /admin/subscription-plans/{id}/activation` | `subscription-plans.activated` / `.deactivated` | `SubscriptionPlan` | global | optional (new) |
| `PUT /providers/active` | `providers.activated` / `.deactivated` | `Provider` (one record per changed provider) | global | optional (new) |
| `PUT /payment-providers/{id}/activation` | `payment-providers.activated` / `.deactivated` | `PaymentProvider` | global | optional (new) |

These are all 15 privileged, step-up-protected mutation endpoints in the repository. `kyc.manage` exists as a
permission but has no administrative mutation endpoint yet, so there is nothing to record for KYC.

Existing domain history is unchanged and still written: `AdministrativeSecurityAudit` for user security
actions, and `SubscriptionAllocation` and `SubscriptionStatusHistory` for subscriptions.

## Data model

Table `auditing.AdministrativeAudits`. It also has the standard entity audit columns (`CreatedAtUtc`,
`IsDeleted`, ...), which the application never changes after insert.

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | uuid | Version 7 GUID |
| `ActorUserId` | uuid | ASP.NET Identity user that acted (required) |
| `ActorStakeholderId` | uuid null | Stakeholder the request acted as |
| `ActorDisplayName` | varchar(250) | Name snapshot at the time of the action |
| `Action` | varchar(128) | Catalog identifier, for example `users.locked` |
| `Reason` | varchar(1000) null | Administrator-supplied justification |
| `TargetType` | varchar(100) | Catalog identifier, for example `Subscription` |
| `TargetId` | uuid | Affected resource |
| `TargetDisplayName` | varchar(250) null | Name snapshot of the affected resource |
| `ActorTenantId` | uuid null | The administrator's request tenant (`X-Tenant-Id`) |
| `TargetTenantId` | uuid null | Tenant of the affected resource; null only for global resources |
| `CorrelationId` | varchar(128) | Request correlation identifier |
| `FlowId` | varchar(128) null | Client flow identifier, truncated to 128 characters |
| `Changes` | jsonb null | Changed fields, see below |
| `OccurredAtUtc` | timestamptz | When the action happened |

Indexes: `(OccurredAtUtc, Id)`, `(ActorUserId, OccurredAtUtc)`, `(Action, OccurredAtUtc)`,
`(TargetType, TargetId, OccurredAtUtc)`, `(TargetTenantId, OccurredAtUtc)` and `(CorrelationId)`. Free-text
search is a `%term%` ILIKE scan, which cannot use these indexes. Add a trigram index only when volume shows a need.

Actor, target and tenant identifiers have no foreign keys. Records stay readable after a user, plan,
provider or tenant is renamed or deleted, and the display names keep the values from the time of the action.

### Actor identity

The actor always comes from the validated access token, never from the request body. The stakeholder
claim is resolved to its `Stakeholder.AppUserId`. User-administration endpoints also know the Identity user
from the token subject, and recording fails if the two disagree. The display name is the stakeholder's
first and last name. When no name is available it falls back to `Unnamed administrator`. Emails and phone
numbers are never stored.

### Tenants

`ActorTenantId` and `TargetTenantId` mean different things. For example, an administrator working in their
own tenant can allocate a subscription in a customer's tenant. Each target type is declared tenant-scoped
(`Subscription`, `AuthenticationSession`) or global (`User`, `SubscriptionPlan`, `Provider`, `PaymentProvider`). The
domain rejects a tenant-scoped record without a target tenant and a global record with one. A null target
tenant therefore always means a deliberately global action and never an unresolved one.

### Changes

`Changes` holds only fields chosen by the action, in one shape:

```json
{
  "isActive": { "previous": true, "current": false },
  "prices": { "previous": { "Monthly": 5000 }, "current": { "Monthly": 7500 } }
}
```

Field names are camelCase. Values keep their JSON types, and unchanged fields are omitted. On creation,
`previous` is `null`. The payload is limited to 16 KB. It never contains entities, credentials, provider
configuration, tokens, hashes or security stamps. `Changes` is null when there is no meaningful field change,
as with revoke-all-sessions and fulfillment retry.

## Recording and transactions

An administrative handler calls `IAdministrativeAuditRecorder.RecordAsync(...)` after its change succeeds
and before it saves. The recorder only adds the row to the request's write `DbContext`. The row is
committed by the handler's own `SaveChanges` or transaction, so the change and its audit commit together or
not at all. If recording fails, for example because the actor cannot be resolved, the mutation fails and
nothing is committed.

- User administration records inside the existing advisory-locked transaction, next to
  `AdministrativeSecurityAudit`.
- Subscription and plan handlers record in their existing single `SaveChanges`, with domain history and outbox
  messages.
- Provider activation needs two saves because the "one active provider per type" index requires the old
  provider to be deactivated first. Both saves now run in one explicit transaction. Previously they
  committed separately, which could leave a partly switched provider.

When one request changes several resources, each gets its own record with the same `CorrelationId`.

### Adding a new administrative mutation

1. Add an action constant to `AdministrativeAuditActions` (and a target type to
   `AdministrativeAuditTargetTypes` if needed, declaring whether it is tenant-scoped).
2. Pass the `ActorContext` from the controller (`ActorContext.FromCurrentActor`).
3. On the success path only, after the change, call `RecordAsync` with explicit `AdministrativeAuditChanges` fields
   and the reason, before saving. Skip it for no-ops and replays.
4. Ensure every save the action needs is in one transaction.
5. Test that the action is recorded, and that no-op and failure paths record nothing.

Action identifiers are persisted. Never rename one. To stop recording an action, move it to
`AdministrativeAuditActions.Retired`, where it stays a valid filter.

## API

Both endpoints are Administrator-only, read-only, and need an active session but not step-up MFA. The
journal is platform-wide. The `tenantId` filter only narrows results and never grants access. There are no
endpoints to create, change or delete audit records.

### `GET /api/v1/admin/audit-logs`

Permission `audit.read`.

| Query | Meaning |
| --- | --- |
| `search` | 2-100 characters; case-insensitive literal match on actor name, target name or reason (`%` and `_` are not wildcards) |
| `action` | A current or retired catalog action; unknown values return 400 |
| `actorUserId` | Identity user id of the administrator |
| `targetType` | Catalog target type; unknown values return 400 |
| `targetId` | Affected resource id |
| `tenantId` | Affected (target) tenant; excludes global records |
| `correlationId` | Request correlation id |
| `fromUtc` / `toUtc` | Inclusive start / exclusive end; `toUtc` must be later than `fromUtc` |
| `page` / `pageSize` | Defaults 1 / 20; `pageSize` at most 100 |

Results are ordered by `occurredAtUtc` descending, then `id` descending.

```json
{
  "items": [
    {
      "id": "019abc...",
      "occurredAtUtc": "2026-11-24T16:02:00+00:00",
      "actor": { "userId": "019a...", "stakeholderId": "019a...", "displayName": "Chidi Nwosu" },
      "action": "users.locked",
      "target": { "type": "User", "id": "019a...", "displayName": "Tunde Bakare" },
      "reason": "Suspected account takeover",
      "actorTenantId": "019a...",
      "targetTenantId": null,
      "correlationId": "0HN8...:00000003",
      "flowId": "0193...",
      "changes": { "isAdministrativelyLocked": { "previous": false, "current": true } }
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 1
}
```

`changes` is a JSON object or `null`, never an escaped string.

### `GET /api/v1/admin/audit-logs/export`

Permission `audit.export`, which is separate from `audit.read`. It accepts the same filters as the list
except paging and returns `text/csv` named `administrative-audit-<UTC timestamp>.csv`, ordered like the list.
Columns: `id, occurredAtUtc, actorUserId, actorDisplayName, action, targetType, targetId, targetDisplayName,
reason, targetTenantId, correlationId`. `Changes` is not exported.

Cells follow RFC 4180 quoting. Any cell starting with `=`, `+`, `-`, `@`, tab or carriage return is prefixed with
an apostrophe so spreadsheets do not run it as a formula. Up to 10,000 rows are exported. When the filter
matches more, the endpoint returns `422` with a problem message asking to narrow the filter, and never sends a
truncated file. An empty result is a header-only file.

## Request contract changes for the frontend

All changes are additive. Existing requests keep working unchanged.

| Request | Change |
| --- | --- |
| `POST /admin/subscription-plans` | optional `reason` (max 1000) |
| `PUT /admin/subscription-plans/{id}` | optional `reason` (max 1000) |
| `PUT /admin/subscription-plans/{id}/activation` | optional `reason` (max 1000); can now return 400 for an oversized reason |
| `POST /admin/subscriptions/{id}/purchase-fulfillment-retries` | optional `reason` (max 1000) |
| `PUT /providers/active` | optional `reason` (max 1000) |
| `PUT /payment-providers/{id}/activation` | optional `reason` (max 1000); can now return 400 for an oversized reason |

The step-up proof covers the JSON body, so send `reason` in the same body the proof was issued for. The portal
should ask for a reason for consequential plan and provider changes. The backend does not require one yet, to
keep existing clients working.

## Data protection and limits

- The journal is append-only within the application: no endpoint or domain method changes or deletes a record. It
  is not cryptographically tamper-proof. Anyone with direct database privileges can still alter rows, so
  database permissions and backups remain the control for that.
- Display names, reasons and changes are untrusted text. Render them escaped. The CSV export applies
  formula-injection protection.
- Reasons are free text. The portal should tell administrators not to enter personal or financial details.
- Not implemented: retention or archival, stronger tamper-resistance, and background exports.
- There is no historical backfill. The journal starts empty at deployment, and earlier actions are only in the
  domain-specific history tables.
