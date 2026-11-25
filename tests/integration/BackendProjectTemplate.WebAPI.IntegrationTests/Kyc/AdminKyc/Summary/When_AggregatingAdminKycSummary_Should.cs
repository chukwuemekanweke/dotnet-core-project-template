using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Authentication.Persistence;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Kyc;
using BackendProjectTemplate.Domain.Kyc.Entities;
using BackendProjectTemplate.Domain.Kyc.ReadModels;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.Infrastructure.Persistence;
using BackendProjectTemplate.WebAPI.IntegrationTests.Subscriptions.AdminSubscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Data.Common;

namespace BackendProjectTemplate.WebAPI.IntegrationTests.Kyc.AdminKyc.Summary;

/// <summary>
/// Exercises the PostgreSQL aggregate behind <c>GET /api/v1/admin/kyc/summary</c> with a fixed reporting
/// instant so window boundaries are deterministic. Every scenario is isolated by unique provider keys or
/// the per-test target tenant.
/// </summary>
[Collection(nameof(ContainersCollection))]
public sealed class When_AggregatingAdminKycSummary_Should(ContainersFixture fixture)
    : AdminSubscriptionIntegrationTestBase(fixture)
{
    private static readonly DateTimeOffset AsOfUtc = new(2026, 11, 25, 19, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset RejectedFromUtc = AsOfUtc.AddDays(-7);

    private readonly string _providerKey = $"kyc-{Guid.CreateVersion7():N}";
    private readonly string _otherProviderKey = $"kyc-{Guid.CreateVersion7():N}";
    private readonly List<Guid> _extraStakeholderIds = [];
    private readonly List<string> _extraEmails = [];

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await SeedVerificationsAsync();
    }

    public override async Task DisposeAsync()
    {
        using (var scope = CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.KycVerifications.IgnoreQueryFilters()
                .Where(item => item.ProviderKey == _providerKey || item.ProviderKey == _otherProviderKey)
                .ExecuteDeleteAsync();
            await dbContext.Stakeholders.IgnoreQueryFilters()
                .Where(item => _extraStakeholderIds.Contains(item.Id))
                .ExecuteDeleteAsync();
            var userRepository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();
            foreach (var email in _extraEmails)
            {
                var user = await userRepository.GetByEmailAsync(email);
                if (user is not null)
                {
                    userRepository.Remove(user);
                }
            }

            await dbContext.SaveChangesAsync();
        }

        await base.DisposeAsync();
    }

    [Fact]
    public async Task CountVisibleAttemptsAcrossTenantsForProvider()
    {
        var summary = await GetSummaryAsync(new AdminKycSummaryFilter(null, _providerKey, RejectedFromUtc, AsOfUtc));

        // Pending: target + foreign second stakeholder. InProgress: target second + foreign.
        // Rejected: lower boundary, one-microsecond-before-upper boundary, started-before-window, foreign.
        summary.ShouldBe(new AdminKycSummaryReadModel(2, 2, 4));
    }

    [Fact]
    public async Task NarrowByTenantAcrossProviders()
    {
        var summary = await GetSummaryAsync(new AdminKycSummaryFilter(TargetTenantId, null, RejectedFromUtc, AsOfUtc));

        summary.ShouldBe(new AdminKycSummaryReadModel(2, 1, 4));
    }

    [Fact]
    public async Task NarrowByTenantAndProvider()
    {
        var summary = await GetSummaryAsync(new AdminKycSummaryFilter(TargetTenantId, _providerKey, RejectedFromUtc, AsOfUtc));

        summary.ShouldBe(new AdminKycSummaryReadModel(1, 1, 3));
    }

    [Fact]
    public async Task NarrowByProviderOnly()
    {
        var summary = await GetSummaryAsync(new AdminKycSummaryFilter(null, _otherProviderKey, RejectedFromUtc, AsOfUtc));

        summary.ShouldBe(new AdminKycSummaryReadModel(1, 0, 1));
    }

    [Fact]
    public async Task ReturnZeroCountsForEmptyDataset()
    {
        var summary = await GetSummaryAsync(
            new AdminKycSummaryFilter(null, $"kyc-{Guid.CreateVersion7():N}", RejectedFromUtc, AsOfUtc));

        summary.ShouldBe(new AdminKycSummaryReadModel(0, 0, 0));
    }

    [Fact]
    public async Task AggregateInOneDatabaseCommandWithoutMaterializingAttempts()
    {
        var capture = new CommandCapture();
        using var scope = CreateScope();
        var options = new DbContextOptionsBuilder<AppReadDbContext>(
                scope.ServiceProvider.GetRequiredService<DbContextOptions<AppReadDbContext>>())
            .AddInterceptors(capture)
            .Options;
        await using var dbContext = new AppReadDbContext(options);

        var summary = await new KycAdministrationReadModelRepository(dbContext).GetSummaryAsync(
            new AdminKycSummaryFilter(null, _providerKey, RejectedFromUtc, AsOfUtc),
            CancellationToken.None);

        summary.ShouldBe(new AdminKycSummaryReadModel(2, 2, 4));
        var sql = capture.Commands.ShouldHaveSingleItem();
        sql.ShouldContain("COUNT(", Case.Insensitive);
        sql.ShouldContain("GROUP BY", Case.Insensitive);
        sql.ShouldContain("FILTER (WHERE", Case.Insensitive);
        sql.ShouldNotContain("KycWebhookInboxes", Case.Insensitive);
        sql.ShouldNotContain("\"ExternalApplicantId\"", Case.Insensitive);
        sql.ShouldNotContain("\"VerificationUrl\"", Case.Insensitive);
        dbContext.ChangeTracker.Entries().ShouldBeEmpty();
    }

    private async Task<AdminKycSummaryReadModel> GetSummaryAsync(AdminKycSummaryFilter filter)
    {
        using var scope = CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IKycAdministrationReadModelRepository>()
            .GetSummaryAsync(filter, CancellationToken.None);
    }

    private async Task SeedVerificationsAsync()
    {
        var targetSecond = await CreateStakeholderAsync(TargetTenantId, isDeleted: false);
        var targetThird = await CreateStakeholderAsync(TargetTenantId, isDeleted: false);
        var foreignSecond = await CreateStakeholderAsync(ForeignTenantId, isDeleted: false);
        var deletedStakeholder = await CreateStakeholderAsync(TargetTenantId, isDeleted: true);

        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var inWindow = AsOfUtc.AddDays(-2);
        dbContext.KycVerifications.AddRange(
            // Active attempts (one per stakeholder is enforced by a partial unique index).
            Pending(TargetTenantId, TargetStakeholderId, _providerKey, inWindow),
            InProgress(TargetTenantId, targetSecond, _providerKey, inWindow),
            Pending(TargetTenantId, targetThird, _otherProviderKey, inWindow),
            InProgress(ForeignTenantId, ForeignStakeholderId, _providerKey, inWindow),
            Pending(ForeignTenantId, foreignSecond, _providerKey, inWindow),

            // Rejected window boundaries: [AsOfUtc - 7 days, AsOfUtc).
            Rejected(TargetTenantId, TargetStakeholderId, _providerKey, RejectedFromUtc.AddHours(-1), RejectedFromUtc),
            Rejected(TargetTenantId, TargetStakeholderId, _providerKey, AsOfUtc.AddDays(-1), AsOfUtc.AddMicroseconds(-1)),
            Rejected(TargetTenantId, TargetStakeholderId, _providerKey, AsOfUtc.AddDays(-1), AsOfUtc),
            Rejected(TargetTenantId, TargetStakeholderId, _providerKey, RejectedFromUtc.AddDays(-1), RejectedFromUtc.AddMicroseconds(-1)),
            // Started well before the window, rejected inside it: counted by CompletedAtUtc, not StartedAtUtc.
            Rejected(TargetTenantId, targetSecond, _providerKey, AsOfUtc.AddDays(-30), inWindow),
            // Started inside the window, rejected after the reporting instant: excluded.
            Rejected(TargetTenantId, targetSecond, _providerKey, inWindow, AsOfUtc.AddHours(1)),
            Rejected(ForeignTenantId, ForeignStakeholderId, _providerKey, inWindow, inWindow),
            Rejected(TargetTenantId, targetThird, _otherProviderKey, inWindow, inWindow),

            // Other terminal outcomes inside the window never count as rejections.
            Verified(TargetTenantId, TargetStakeholderId, _providerKey, inWindow),
            Expired(TargetTenantId, TargetStakeholderId, _providerKey, inWindow),
            Failed(TargetTenantId, TargetStakeholderId, _providerKey, inWindow),

            // Soft-deleted attempts are invisible, as in the administrative list and detail endpoints.
            SoftDeleted(Rejected(TargetTenantId, targetSecond, _providerKey, inWindow, inWindow)),
            SoftDeleted(Pending(TargetTenantId, targetThird, _providerKey, inWindow)),

            // Attempts of a soft-deleted stakeholder are invisible, as in the administrative list.
            Pending(TargetTenantId, deletedStakeholder, _providerKey, inWindow),
            Rejected(TargetTenantId, deletedStakeholder, _providerKey, inWindow, inWindow));
        await dbContext.SaveChangesAsync();
    }

    private async Task<Guid> CreateStakeholderAsync(Guid tenantId, bool isDeleted)
    {
        using var scope = CreateScope();
        var identityService = scope.ServiceProvider.GetRequiredService<IAuthenticationIdentityService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var email = WebApiIntegrationTestData.Email();
        var user = AppUser.Create(email, "Ada", "Lovelace");
        (await identityService.CreateAsync(user, Password)).Succeeded.ShouldBeTrue();
        _extraEmails.Add(email);

        var template = await dbContext.Stakeholders.AsNoTracking().FirstAsync(item => item.TenantId == tenantId);
        var stakeholder = Stakeholder.Create(
            user.Id,
            tenantId,
            template.CountryId,
            template.StakeholderTypeId,
            "Ada",
            "Lovelace");
        if (isDeleted)
        {
            stakeholder.SetDeletedAudit(AsOfUtc, "integration-test");
        }

        dbContext.Stakeholders.Add(stakeholder);
        await dbContext.SaveChangesAsync();
        _extraStakeholderIds.Add(stakeholder.Id);
        return stakeholder.Id;
    }

    private static KycVerification Pending(Guid tenantId, Guid stakeholderId, string providerKey, DateTimeOffset startedAtUtc) =>
        KycVerification.Start(tenantId, stakeholderId, providerKey, $"applicant-{Guid.CreateVersion7():N}", null, startedAtUtc);

    private static KycVerification InProgress(Guid tenantId, Guid stakeholderId, string providerKey, DateTimeOffset startedAtUtc)
    {
        var verification = Pending(tenantId, stakeholderId, providerKey, startedAtUtc);
        verification.ApplyProviderResult(KycVerificationTransition.InProgress, null, startedAtUtc.AddMinutes(5));
        verification.Status.ShouldBe(KycVerificationStatus.InProgress);
        return verification;
    }

    private static KycVerification Rejected(
        Guid tenantId,
        Guid stakeholderId,
        string providerKey,
        DateTimeOffset startedAtUtc,
        DateTimeOffset rejectedAtUtc)
    {
        var verification = InProgress(tenantId, stakeholderId, providerKey, startedAtUtc);
        verification.ApplyProviderResult(KycVerificationTransition.Rejected, null, rejectedAtUtc);
        verification.CompletedAtUtc.ShouldBe(rejectedAtUtc);
        return verification;
    }

    private static KycVerification Verified(Guid tenantId, Guid stakeholderId, string providerKey, DateTimeOffset atUtc)
    {
        var verification = Pending(tenantId, stakeholderId, providerKey, atUtc);
        verification.ApplyProviderResult(KycVerificationTransition.Verified, null, atUtc);
        return verification;
    }

    private static KycVerification Expired(Guid tenantId, Guid stakeholderId, string providerKey, DateTimeOffset atUtc)
    {
        var verification = Pending(tenantId, stakeholderId, providerKey, atUtc);
        verification.MarkExpired(atUtc);
        return verification;
    }

    private static KycVerification Failed(Guid tenantId, Guid stakeholderId, string providerKey, DateTimeOffset atUtc)
    {
        var verification = Pending(tenantId, stakeholderId, providerKey, atUtc);
        verification.MarkFailed(atUtc);
        return verification;
    }

    private static KycVerification SoftDeleted(KycVerification verification)
    {
        verification.SetDeletedAudit(AsOfUtc, "integration-test");
        return verification;
    }

    private sealed class CommandCapture : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
