/*
    Fails initial subscription purchase payments that were committed but never accepted by the provider (still
    PendingInitiation with no provider reference an hour later), and releases their Pending subscriptions. The
    customer never received a checkout, so such a payment can never succeed; left alone, an idempotent replay
    returns a purchase with no payment link and the subscription blocks a new purchase. PaymentInitiationService
    does this when the provider call fails; this idempotent backfill covers purchases from before that, or whose
    request died before it could record the failure. Renewal payments have no purchase row and are not touched.
    It releases the subscription itself because 0003 has already run by the time this script fails the payment.
*/

WITH failed AS (
    UPDATE payments."PaymentTransactions" pt
    SET
        "PaymentStatus" = 4,                  -- PaymentStatus.Failed
        "FailureReason" = 'provider_initiation_failed',
        "StatusChangeReason" = 'provider_initiation_failed',
        "FailedAtUtc" = date_trunc('microseconds', NOW()),
        "UpdatedAtUtc" = NOW(),
        "UpdatedBy" = 'database-migrator'
    FROM payments."SubscriptionPurchases" p
    WHERE p."PaymentTransactionId" = pt."Id"
      AND p."IsDeleted" = FALSE
      AND pt."IsDeleted" = FALSE
      AND pt."PaymentIntent" = 2              -- PaymentIntent.Subscription
      AND pt."PaymentStatus" = 1              -- PaymentStatus.PendingInitiation
      AND pt."ProviderReference" IS NULL
      AND pt."CreatedAtUtc" < NOW() - INTERVAL '1 hour'
    RETURNING pt."Id" AS "PaymentTransactionId", p."Id" AS "PurchaseId", p."SubscriptionId"
),
expired AS (
    UPDATE payments."Subscriptions" s
    SET
        "Status" = 4,                         -- SubscriptionStatus.Expired
        "EndedAtUtc" = date_trunc('microseconds', NOW()),
        "NextBillingAtUtc" = NULL,
        "UpdatedAtUtc" = NOW(),
        "UpdatedBy" = 'database-migrator'
    FROM failed f
    WHERE s."Id" = f."SubscriptionId"
      AND s."Status" = 1                      -- SubscriptionStatus.Pending
      AND s."IsDeleted" = FALSE
    RETURNING s."Id" AS "SubscriptionId", s."EndedAtUtc", f."PurchaseId", f."PaymentTransactionId"
)
INSERT INTO payments."SubscriptionStatusHistory" (
    "Id", "SubscriptionId", "PreviousStatus", "NewStatus", "Reason", "OccurredAtUtc", "ActorId", "OperationId",
    "PaymentTransactionId", "Details", "IsDeleted", "CreatedAtUtc", "CreatedBy", "UpdatedAtUtc", "UpdatedBy")
SELECT
    gen_random_uuid(), e."SubscriptionId", 1, 4, 'initial_payment_unsuccessful', e."EndedAtUtc", NULL, e."PurchaseId",
    e."PaymentTransactionId", NULL, FALSE, NOW(), 'database-migrator', NOW(), 'database-migrator'
FROM expired e
ON CONFLICT ("SubscriptionId", "OperationId") DO NOTHING;
