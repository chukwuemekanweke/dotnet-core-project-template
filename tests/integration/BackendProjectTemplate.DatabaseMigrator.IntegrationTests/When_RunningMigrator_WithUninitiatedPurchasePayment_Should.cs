using BackendProjectTemplate.DatabaseMigrator.IntegrationTests.Infrastructure;
using Shouldly;

namespace BackendProjectTemplate.DatabaseMigrator.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class When_RunningMigrator_WithUninitiatedPurchasePayment_Should(PostgresFixture fixture) : IAsyncLifetime
{
    private const string StalePaymentId = "019a9b00-0000-7000-8000-0000000000a1";
    private const string RecentPaymentId = "019a9b00-0000-7000-8000-0000000000a2";
    private const string AcceptedPaymentId = "019a9b00-0000-7000-8000-0000000000a3";
    private const string StaleSubscriptionId = "019a9b00-0000-7000-8000-000000000001";
    private const string RecentSubscriptionId = "019a9b00-0000-7000-8000-000000000002";
    private const string AcceptedSubscriptionId = "019a9b00-0000-7000-8000-000000000003";

    private readonly MigrationDatabase _database = fixture.CreateDatabase();

    public async Task InitializeAsync()
    {
        var firstRun = await MigratorProcess.RunAsync(_database.ConnectionString);
        firstRun.ExitCode.ShouldBe(0, firstRun.Output);

        // Three Pending purchases whose payment is still PendingInitiation: one abandoned two hours ago, one
        // created just now (its request may still be in flight), and one the provider already accepted.
        // Foreign keys are irrelevant to the backfill, so they are skipped.
        await _database.ExecuteAsync($"""
            SET session_replication_role = replica;
            INSERT INTO payments."PaymentTransactions" (
                "Id", "Amount", "CountryId", "CreatedAtUtc", "CurrencyId", "InitiatedByUserId", "IsDeleted",
                "MerchantReference", "PaymentIntent", "PaymentMethodType", "PaymentProviderId", "PaymentStatus",
                "ProviderPayloadMetadata", "ProviderReference", "StakeholderId", "TenantId", "UpdatedAtUtc")
            VALUES
                ('{StalePaymentId}', 500000, gen_random_uuid(), NOW() - INTERVAL '2 hours', gen_random_uuid(), gen_random_uuid(), FALSE,
                 'pay_stale', 2, 0, gen_random_uuid(), 1, jsonb_build_object(), NULL, gen_random_uuid(), gen_random_uuid(), NOW()),
                ('{RecentPaymentId}', 500000, gen_random_uuid(), NOW(), gen_random_uuid(), gen_random_uuid(), FALSE,
                 'pay_recent', 2, 0, gen_random_uuid(), 1, jsonb_build_object(), NULL, gen_random_uuid(), gen_random_uuid(), NOW()),
                ('{AcceptedPaymentId}', 500000, gen_random_uuid(), NOW() - INTERVAL '2 hours', gen_random_uuid(), gen_random_uuid(), FALSE,
                 'pay_accepted', 2, 0, gen_random_uuid(), 1, jsonb_build_object(), 'provider-ref', gen_random_uuid(), gen_random_uuid(), NOW());
            INSERT INTO payments."Subscriptions" (
                "Id", "AutoRenew", "BillingAccountId", "BillingInterval", "CancelAtPeriodEnd", "CreatedAtUtc", "IsDeleted",
                "PlanId", "Status", "TenantId", "UpdatedAtUtc")
            VALUES
                ('{StaleSubscriptionId}', FALSE, gen_random_uuid(), 1, FALSE, NOW(), FALSE, gen_random_uuid(), 1, gen_random_uuid(), NOW()),
                ('{RecentSubscriptionId}', FALSE, gen_random_uuid(), 1, FALSE, NOW(), FALSE, gen_random_uuid(), 1, gen_random_uuid(), NOW()),
                ('{AcceptedSubscriptionId}', FALSE, gen_random_uuid(), 1, FALSE, NOW(), FALSE, gen_random_uuid(), 1, gen_random_uuid(), NOW());
            INSERT INTO payments."SubscriptionPurchases" (
                "Id", "BillingAccountId", "BillingInterval", "CreatedAtUtc", "ExpectedAmount", "ExpectedCurrencyId",
                "InitiatedByStakeholderId", "IsDeleted", "OperationId", "PaymentTransactionId", "PlanCode", "PlanId",
                "RequestFingerprint", "SubscriptionId", "TenantId", "UpdatedAtUtc")
            VALUES
                (gen_random_uuid(), gen_random_uuid(), 1, NOW(), 500000, gen_random_uuid(), gen_random_uuid(), FALSE,
                 gen_random_uuid(), '{StalePaymentId}', 'STARTER_MONTHLY', gen_random_uuid(), 'fp', '{StaleSubscriptionId}', gen_random_uuid(), NOW()),
                (gen_random_uuid(), gen_random_uuid(), 1, NOW(), 500000, gen_random_uuid(), gen_random_uuid(), FALSE,
                 gen_random_uuid(), '{RecentPaymentId}', 'STARTER_MONTHLY', gen_random_uuid(), 'fp', '{RecentSubscriptionId}', gen_random_uuid(), NOW()),
                (gen_random_uuid(), gen_random_uuid(), 1, NOW(), 500000, gen_random_uuid(), gen_random_uuid(), FALSE,
                 gen_random_uuid(), '{AcceptedPaymentId}', 'STARTER_MONTHLY', gen_random_uuid(), 'fp', '{AcceptedSubscriptionId}', gen_random_uuid(), NOW());
            SET session_replication_role = origin;
            """);
    }

    public Task DisposeAsync() => _database.DropAsync();

    [Fact]
    public async Task FailOnlyAbandonedPaymentsAndReleaseTheirSubscriptions()
    {
        // When: running twice proves the backfill is idempotent.
        var result = await MigratorProcess.RunAsync(_database.ConnectionString);
        result.ExitCode.ShouldBe(0, result.Output);
        var secondRun = await MigratorProcess.RunAsync(_database.ConnectionString);
        secondRun.ExitCode.ShouldBe(0, secondRun.Output);

        // Then
        (await PaymentStatusOfAsync(StalePaymentId)).ShouldBe(4);
        (await SubscriptionStatusOfAsync(StaleSubscriptionId)).ShouldBe(4);
        (await PaymentStatusOfAsync(RecentPaymentId)).ShouldBe(1);
        (await SubscriptionStatusOfAsync(RecentSubscriptionId)).ShouldBe(1);
        (await PaymentStatusOfAsync(AcceptedPaymentId)).ShouldBe(1);
        (await SubscriptionStatusOfAsync(AcceptedSubscriptionId)).ShouldBe(1);
        var historyRows = await _database.ScalarAsync<long>($"""
            SELECT COUNT(*) FROM payments."SubscriptionStatusHistory"
            WHERE "SubscriptionId" = '{StaleSubscriptionId}'
              AND "Reason" = 'initial_payment_unsuccessful' AND "PreviousStatus" = 1 AND "NewStatus" = 4
            """);
        historyRows.ShouldBe(1);
    }

    private Task<int> PaymentStatusOfAsync(string paymentId) =>
        _database.ScalarAsync<int>($"""SELECT "PaymentStatus" FROM payments."PaymentTransactions" WHERE "Id" = '{paymentId}'""");

    private Task<int> SubscriptionStatusOfAsync(string subscriptionId) =>
        _database.ScalarAsync<int>($"""SELECT "Status" FROM payments."Subscriptions" WHERE "Id" = '{subscriptionId}'""");
}
