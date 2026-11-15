using BackendProjectTemplate.Domain.Kyc;
using BackendProjectTemplate.Domain.Kyc.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace BackendProjectTemplate.Infrastructure.Persistence.Interceptors;

internal sealed class KycConflictTranslationInterceptor : SaveChangesInterceptor
{
    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        if (eventData.Exception is DbUpdateConcurrencyException concurrencyException &&
            concurrencyException.Entries.Any(entry => entry.Entity is KycVerification))
        {
            throw new KycVerificationConcurrencyException(
                "The KYC verification was changed by another operation. Reload it and retry.", concurrencyException);
        }

        if (eventData.Exception is not DbUpdateException
            {
                InnerException: PostgresException
                {
                    SqlState: PostgresErrorCodes.UniqueViolation
                } postgresException
            } exception)
        {
            return base.SaveChangesFailedAsync(eventData, cancellationToken);
        }

        if (postgresException.ConstraintName == KycPersistenceConstraints.ActiveStakeholderUniqueIndexName)
        {
            throw new KycActiveVerificationConflictException(
                "An active KYC verification already exists for this stakeholder.", exception);
        }

        if (postgresException.ConstraintName == KycPersistenceConstraints.WebhookRequestUniqueIndexName)
        {
            throw new KycWebhookDuplicateException(
                "The KYC webhook request has already been received.", exception);
        }

        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }
}
