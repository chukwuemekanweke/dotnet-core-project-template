using BackendProjectTemplate.Domain.Kyc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace BackendProjectTemplate.Infrastructure.Persistence.Interceptors;

internal sealed class KycConflictTranslationInterceptor : SaveChangesInterceptor
{
    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        if (eventData.Exception is DbUpdateException
            {
                InnerException: PostgresException
                {
                    SqlState: PostgresErrorCodes.UniqueViolation,
                    ConstraintName: KycPersistenceConstraints.ActiveStakeholderUniqueIndexName
                }
            })
        {
            throw new KycActiveVerificationConflictException(
                "An active KYC verification already exists for this stakeholder.", eventData.Exception);
        }

        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }
}
