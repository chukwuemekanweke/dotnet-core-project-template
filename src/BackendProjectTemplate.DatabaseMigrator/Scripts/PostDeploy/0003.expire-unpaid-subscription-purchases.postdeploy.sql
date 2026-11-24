/*
    Releases Pending subscriptions whose initial payment concluded unsuccessfully (Failed, Cancelled, Expired or
    Underpaid). Such a subscription can never activate, yet it would otherwise remain the BillingAccount's
    current subscription and block a new purchase. The UnsuccessfulPaymentConcluded consumer does this for new
    outcomes; this idempotent backfill covers purchases that concluded before that event existed or whose event
    was missed. The purchase id is the deterministic history operation id.
*/

WITH unpaid AS (
    SELECT s."Id" AS "SubscriptionId", p."Id" AS "PurchaseId", pt."Id" AS "PaymentTransactionId"
    FROM payments."Subscriptions" s
    INNER JOIN payments."SubscriptionPurchases" p ON p."SubscriptionId" = s."Id"
    INNER JOIN payments."PaymentTransactions" pt ON pt."Id" = p."PaymentTransactionId"
    WHERE s."Status" = 1                      -- SubscriptionStatus.Pending
      AND s."IsDeleted" = FALSE
      AND pt."PaymentStatus" IN (4, 5, 6, 7)  -- Failed, Cancelled, Expired, Underpaid
),
expired AS (
    UPDATE payments."Subscriptions" s
    SET
        "Status" = 4,                         -- SubscriptionStatus.Expired
        "EndedAtUtc" = date_trunc('microseconds', NOW()),
        "NextBillingAtUtc" = NULL,
        "UpdatedAtUtc" = NOW(),
        "UpdatedBy" = 'database-migrator'
    FROM unpaid u
    WHERE s."Id" = u."SubscriptionId"
      AND s."Status" = 1
    RETURNING s."Id" AS "SubscriptionId", s."EndedAtUtc", u."PurchaseId", u."PaymentTransactionId"
)
INSERT INTO payments."SubscriptionStatusHistory" (
    "Id", "SubscriptionId", "PreviousStatus", "NewStatus", "Reason", "OccurredAtUtc", "ActorId", "OperationId",
    "PaymentTransactionId", "Details", "IsDeleted", "CreatedAtUtc", "CreatedBy", "UpdatedAtUtc", "UpdatedBy")
SELECT
    gen_random_uuid(), e."SubscriptionId", 1, 4, 'initial_payment_unsuccessful', e."EndedAtUtc", NULL, e."PurchaseId",
    e."PaymentTransactionId", NULL, FALSE, NOW(), 'database-migrator', NOW(), 'database-migrator'
FROM expired e
ON CONFLICT ("SubscriptionId", "OperationId") DO NOTHING;
