using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using BackendProjectTemplate.Domain.Subscriptions.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace BackendProjectTemplate.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Translates the PostgreSQL failures that protect subscription invariants into domain exceptions so
/// Application and Consumer code can react without depending on EF Core or Npgsql:
/// the BillingAccount entitlement exclusion constraint, the allocation idempotency key, plan-code
/// uniqueness and optimistic concurrency on <see cref="Subscription"/>.
/// </summary>
internal sealed class SubscriptionConflictTranslationInterceptor : SaveChangesInterceptor
{
    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        Translate(eventData.Exception);
        base.SaveChangesFailed(eventData);
    }

    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        Translate(eventData.Exception);
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    internal static void Translate(Exception exception)
    {
        if (exception is DbUpdateConcurrencyException concurrencyException &&
            concurrencyException.Entries.Any(entry => entry.Entity is Subscription))
        {
            throw new SubscriptionConcurrencyConflictException(
                "The subscription was changed by another operation. Reload it and retry.",
                exception);
        }

        if (exception is not DbUpdateException { InnerException: PostgresException postgresException })
        {
            return;
        }

        switch (postgresException)
        {
            case { SqlState: PostgresErrorCodes.ExclusionViolation, ConstraintName: SubscriptionPersistenceConstraints.EntitlementExclusionConstraintName }:
                throw new SubscriptionEntitlementConflictException(
                    "The BillingAccount already holds an overlapping subscription entitlement.",
                    exception);
            case { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: SubscriptionPersistenceConstraints.AllocationOperationUniqueIndexName }:
                throw new SubscriptionAllocationOperationConflictException(
                    "An allocation with the same idempotency key was committed concurrently.",
                    exception);
            case { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: SubscriptionPersistenceConstraints.PlanCodeUniqueIndexName }:
                throw new SubscriptionPlanCodeConflictException(
                    "A subscription plan with the same code already exists.",
                    exception);
        }
    }
}
