/*
    Subscription plan prices are stored in whole minor currency units, matching PaymentTransaction.Amount.
    Updating a plan changes future purchases only; SubscriptionPeriods retain immutable price snapshots.
*/

WITH plan_values("Id", "Code", "Name", "Price", "CurrencyCode", "BillingInterval", "IsActive") AS (VALUES
    ('019a7f08-1f3d-7c25-a6ab-4dcfb6f80601'::uuid, 'STARTER_MONTHLY', 'Starter Monthly', 500000::numeric, 'NGN', 1, TRUE),
    ('019a7f08-1f3d-7c25-a6ab-4dcfb6f80602'::uuid, 'STARTER_YEARLY', 'Starter Yearly', 5000000::numeric, 'NGN', 2, TRUE)
),
resolved_plans AS (
    SELECT
        v."Id",
        v."Code",
        v."Name",
        v."Price",
        c."Id" AS "CurrencyId",
        v."BillingInterval",
        v."IsActive"
    FROM plan_values v
    INNER JOIN payments."Currencies" c ON c."CurrencyCode" = v."CurrencyCode"
),
plans_updated AS (
    UPDATE payments."SubscriptionPlans" p
    SET
        "Name" = v."Name",
        "Price" = v."Price",
        "CurrencyId" = v."CurrencyId",
        "BillingInterval" = v."BillingInterval",
        "IsActive" = v."IsActive",
        "UpdatedAtUtc" = NOW()
    FROM resolved_plans v
    WHERE p."Code" = v."Code"
      AND (p."Name" IS DISTINCT FROM v."Name"
           OR p."Price" IS DISTINCT FROM v."Price"
           OR p."CurrencyId" IS DISTINCT FROM v."CurrencyId"
           OR p."BillingInterval" IS DISTINCT FROM v."BillingInterval"
           OR p."IsActive" IS DISTINCT FROM v."IsActive")
)
INSERT INTO payments."SubscriptionPlans" (
    "Id", "Code", "Name", "Price", "CurrencyId", "BillingInterval", "IsActive",
    "CreatedAtUtc", "UpdatedAtUtc", "IsDeleted")
SELECT
    v."Id", v."Code", v."Name", v."Price", v."CurrencyId", v."BillingInterval", v."IsActive",
    NOW(), NOW(), FALSE
FROM resolved_plans v
WHERE NOT EXISTS (
    SELECT 1 FROM payments."SubscriptionPlans" p WHERE p."Code" = v."Code"
);
