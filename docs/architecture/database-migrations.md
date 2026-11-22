# Database Migrations

`BackendProjectTemplate.DatabaseMigrator` is a one-shot executable. It starts, runs the database
initialization pipeline once, and exits. Its exit code is the only success signal:

| Outcome | Exit code |
| --- | --- |
| Every stage succeeded (including "nothing pending") | `0` |
| Database unreachable, migration or script failure, missing connection string | `1` |
| Cancelled (SIGTERM/Ctrl+C) before completion | `1` |
| Production secret validation failure (thrown before the host is built) | nonzero (.NET unhandled-exception code) |

The migrator exposes no HTTP endpoint or health check. A migrator that is still running has not succeeded.

## Pipeline

`DatabaseInitializationExtensions.InitializeDatabaseAsync` runs these stages in order, logging the start,
completion or failure of each:

1. **EnsureDatabase** — creates the target database if it does not exist.
2. **PreDeploy** — `Scripts/PreDeploy/*.sql`, ordered by file name (ordinal, case-insensitive).
3. **EfCoreMigrations** — `MigrateAsync`; EF Core records applied migrations in `__EFMigrationsHistory`.
4. **AuthorizationSeed** — find-or-create of application roles and the optional administrator bootstrap.
5. **PostDeploy** — `Scripts/PostDeploy/*.sql`, ordered by file name.

Scripts are split into batches on lines containing only `GO`. Each batch runs as its own statement with no
transaction spanning the script, the stage, or the pipeline. **Stages and batches that succeed stay
committed when a later one fails.** The migrator never runs reverse migrations.

## Rules for migration and script authors

### Every script runs on every deployment

PreDeploy and PostDeploy scripts are not journaled: all of them run on every migrator execution, including
retries of a failed deployment. Every script must be safe to run any number of times:

- Use `CREATE ... IF NOT EXISTS`, `CREATE OR REPLACE`, `DROP ... IF EXISTS`.
- Seed with upsert patterns (`INSERT ... WHERE NOT EXISTS`, `ON CONFLICT`, or update-when-distinct, as the
  existing `0001`/`0002` PostDeploy scripts do).
- Guard one-off data fixes so that a second run does nothing (for example, filter on the old state).
- If a script has several dependent statements that must succeed together, wrap them in an explicit
  `BEGIN; ... COMMIT;` in a single batch.

### Expand / migrate / contract

A successful migration changes the database before the new application release is rolled out. If the
rollout then fails, the deployment toolkit restores the **previous application images** — it does not
restore the previous schema. The previous release must therefore keep working against the migrated schema.

1. **Expand** — add new tables, nullable columns, or columns with defaults, new indexes, and new views.
   Do not change or remove anything the current release reads or writes.
2. **Migrate** — ship application code that uses the new schema, and backfill data (in a PostDeploy script
   or a later migration) while both shapes still exist.
3. **Contract** — only in a later release, once no deployed or rollback-eligible release depends on them,
   drop the old columns, tables, or constraints.

Dropping or renaming a column, changing its type, or adding a `NOT NULL` column without a default in the same
release that stops using it breaks the previous release. If that release is restored after a failed rollout,
its queries fail against the new schema, so automatic restoration would bring back images that cannot run.

## Failure recovery

| Situation | What happened | What to do |
| --- | --- | --- |
| Migration fails before rollout | Long-running services were not touched. Stages before the failure may be committed. | Read the migrator logs. Look for the last `stage ... failed` / `script ... failed` line. Fix forward and redeploy. |
| A script failed partway | Batches before the failing one are committed. | Make the script idempotent if needed, fix it, and redeploy. The retry re-runs every script. |
| Retry of the same release | EF Core skips applied migrations. Scripts run again. | No action needed if scripts follow the rules above. |
| Migration succeeded, rollout failed | The new schema is live. The toolkit may restore the previous images. | Confirm the previous release still works against the new schema (expand/contract). Fix forward with a corrective migration. Never hand-edit `__EFMigrationsHistory` to "undo" a migration. |
| Migration cancelled or timed out | Same as a failure. Committed stages remain. | Investigate long-running statements and locks, then redeploy. |

On a DEVOPS-010 host, a failed or timed-out migrator container is kept stopped for diagnosis:
`docker logs <stack>-migration-<attempt>`. Manual database repair (restoring a backup, reverting data) is an
explicit operator action. The migrator and the toolkit never do it automatically.

## Running it

- Local: `docker compose up` runs the migrator first. WebAPI, Consumer and Jobs wait for
  `service_completed_successfully`. To run it on its own, use
  `docker compose run --rm database-migrator`, or `dotnet run` with `DOTNET_ENVIRONMENT` set to a
  non-Production environment.
- Production: `deploy/compose.production.yml` puts `database-migrator` in the `migration` profile, so
  `docker compose up` never starts it and no service depends on it. The DEVOPS-010 `vps-deploy` workflow
  (`migration-service: database-migrator`) runs it with `docker compose run`, enforces the timeout, and starts
  the rollout only after exit code `0`. It mounts only its own DEVOPS-007 secret generation
  (`ConnectionStrings__PostgresWrite`) at `/run/secrets/app`.
