using BackendProjectTemplate.DatabaseMigrator.IntegrationTests.Infrastructure;
using Shouldly;

namespace BackendProjectTemplate.DatabaseMigrator.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class When_RunningMigrator_WithUnpaidPendingSubscription_Should(PostgresFixture fixture) : IAsyncLifetime
{
    private const string ExpiredPaymentSubscriptionId = "019a9a00-0000-7000-8000-000000000001";
    private const string InFlightPaymentSubscriptionId = "019a9a00-0000-7000-8000-000000000002";

    private readonly MigrationDatabase _database = fixture.CreateDatabase();

    public async Task InitializeAsync()
    {
        var firstRun = await MigratorProcess.RunAsync(_database.ConnectionString);
        firstRun.ExitCode.ShouldBe(0, firstRun.Output);

        // Two Pending purchases: one whose payment expired before the unsuccessful-payment event existed, and
        // one whose payment is still in flight. Foreign keys are irrelevant to the backfill, so they are skipped.
        await _database.ExecuteAsync($"""
            SET session_replication_role = replica;
            INSERT INTO payments."PaymentTransactions" (
                "Id", "Amount", "CountryId", "CreatedAtUtc", "CurrencyId", "InitiatedByUserId", "IsDeleted",
                "MerchantReference", "PaymentIntent", "PaymentMethodType", "PaymentProviderId", "PaymentStatus",
                "ProviderPayloadMetadata", "StakeholderId", "TenantId", "UpdatedAtUtc")
            VALUES
                ('019a9a00-0000-7000-8000-0000000000a1', 500000, gen_random_uuid(), NOW(), gen_random_uuid(), gen_random_uuid(), FALSE,
                 'pay_expired', 2, 0, gen_random_uuid(), 6, jsonb_build_object(), gen_random_uuid(), gen_random_uuid(), NOW()),
                ('019a9a00-0000-7000-8000-0000000000a2', 500000, gen_random_uuid(), NOW(), gen_random_uuid(), gen_random_uuid(), FALSE,
                 'pay_initiated', 2, 0, gen_random_uuid(), 2, jsonb_build_object(), gen_random_uuid(), gen_random_uuid(), NOW());
            INSERT INTO payments."Subscriptions" (
                "Id", "AutoRenew", "BillingAccountId", "BillingInterval", "CancelAtPeriodEnd", "CreatedAtUtc", "IsDeleted",
                "PlanId", "Status", "TenantId", "UpdatedAtUtc")
            VALUES
                ('{ExpiredPaymentSubscriptionId}', FALSE, gen_random_uuid(), 1, FALSE, NOW(), FALSE, gen_random_uuid(), 1, gen_random_uuid(), NOW()),
                ('{InFlightPaymentSubscriptionId}', FALSE, gen_random_uuid(), 1, FALSE, NOW(), FALSE, gen_random_uuid(), 1, gen_random_uuid(), NOW());
            INSERT INTO payments."SubscriptionPurchases" (
                "Id", "BillingAccountId", "BillingInterval", "CreatedAtUtc", "ExpectedAmount", "ExpectedCurrencyId",
                "InitiatedByStakeholderId", "IsDeleted", "OperationId", "PaymentTransactionId", "PlanCode", "PlanId",
                "RequestFingerprint", "SubscriptionId", "TenantId", "UpdatedAtUtc")
            VALUES
                (gen_random_uuid(), gen_random_uuid(), 1, NOW(), 500000, gen_random_uuid(), gen_random_uuid(), FALSE,
                 gen_random_uuid(), '019a9a00-0000-7000-8000-0000000000a1', 'STARTER_MONTHLY', gen_random_uuid(), 'fp',
                 '{ExpiredPaymentSubscriptionId}', gen_random_uuid(), NOW()),
                (gen_random_uuid(), gen_random_uuid(), 1, NOW(), 500000, gen_random_uuid(), gen_random_uuid(), FALSE,
                 gen_random_uuid(), '019a9a00-0000-7000-8000-0000000000a2', 'STARTER_MONTHLY', gen_random_uuid(), 'fp',
                 '{InFlightPaymentSubscriptionId}', gen_random_uuid(), NOW());
            SET session_replication_role = origin;
            """);
    }

    public Task DisposeAsync() => _database.DropAsync();

    [Fact]
    public async Task ExpireOnlySubscriptionsWhoseInitialPaymentCanNeverSucceed()
    {
        // When: running twice proves the backfill is idempotent.
        var result = await MigratorProcess.RunAsync(_database.ConnectionString);
        result.ExitCode.ShouldBe(0, result.Output);
        var secondRun = await MigratorProcess.RunAsync(_database.ConnectionString);
        secondRun.ExitCode.ShouldBe(0, secondRun.Output);

        // Then
        (await StatusOfAsync(ExpiredPaymentSubscriptionId)).ShouldBe(4);
        (await StatusOfAsync(InFlightPaymentSubscriptionId)).ShouldBe(1);
        var historyRows = await _database.ScalarAsync<long>($"""
            SELECT COUNT(*) FROM payments."SubscriptionStatusHistory"
            WHERE "SubscriptionId" = '{ExpiredPaymentSubscriptionId}'
              AND "Reason" = 'initial_payment_unsuccessful' AND "PreviousStatus" = 1 AND "NewStatus" = 4
            """);
        historyRows.ShouldBe(1);
    }

    private Task<int> StatusOfAsync(string subscriptionId) =>
        _database.ScalarAsync<int>($"""SELECT "Status" FROM payments."Subscriptions" WHERE "Id" = '{subscriptionId}'""");
}
