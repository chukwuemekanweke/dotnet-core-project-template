/*
    Seeds the subscription packages. Each package is one plan with a monthly and an annual price; the annual
    price is not a separate plan. Prices are stored in whole minor currency units (kobo for NGN), matching
    PaymentTransaction.Amount. This script runs on every deploy and is authoritative for the plans it lists:
    changing a value here reprices future purchases only, because purchases, periods and renewals keep
    immutable price snapshots.

    The legacy single-interval STARTER_MONTHLY and STARTER_YEARLY plans are deactivated rather than deleted, so
    subscriptions and history that reference them stay valid. They are never created on a fresh database.
*/

-- Plans: code, name, card description, ordered feature bullets.
WITH plan_values("Id", "Code", "Name", "Description", "Features", "CurrencyCode", "IsActive") AS (VALUES
    ('019a7f08-1f3d-7c25-a6ab-4dcfb6f80611'::uuid, 'BASIC', 'Basic',
        'Everything you need to get started.',
        ARRAY['Core features', 'Up to 3 team members', 'Email support']::text[],
        'NGN', TRUE),
    ('019a7f08-1f3d-7c25-a6ab-4dcfb6f80612'::uuid, 'STANDARD', 'Standard',
        'More power and flexibility for growing teams.',
        ARRAY['All Basic features', 'Up to 10 team members', 'Priority support', 'Advanced reports']::text[],
        'NGN', TRUE),
    ('019a7f08-1f3d-7c25-a6ab-4dcfb6f80613'::uuid, 'PREMIUM', 'Premium',
        'Advanced capabilities for larger organizations.',
        ARRAY['All Standard features', 'Unlimited team members', 'Admin controls & audit logs', 'Dedicated support']::text[],
        'NGN', TRUE)
),
resolved_plans AS (
    SELECT v."Id", v."Code", v."Name", v."Description", v."Features", c."Id" AS "CurrencyId", v."IsActive"
    FROM plan_values v
    INNER JOIN payments."Currencies" c ON c."CurrencyCode" = v."CurrencyCode"
),
plans_updated AS (
    UPDATE payments."SubscriptionPlans" p
    SET
        "Name" = v."Name",
        "Description" = v."Description",
        "Features" = v."Features",
        "IsActive" = v."IsActive",
        "UpdatedAtUtc" = NOW()
    FROM resolved_plans v
    WHERE p."Code" = v."Code"
      AND (p."Name" IS DISTINCT FROM v."Name"
           OR p."Description" IS DISTINCT FROM v."Description"
           OR p."Features" IS DISTINCT FROM v."Features"
           OR p."IsActive" IS DISTINCT FROM v."IsActive")
)
INSERT INTO payments."SubscriptionPlans" (
    "Id", "Code", "Name", "Description", "Features", "CurrencyId", "IsActive",
    "CreatedAtUtc", "UpdatedAtUtc", "IsDeleted")
SELECT
    v."Id", v."Code", v."Name", v."Description", v."Features", v."CurrencyId", v."IsActive",
    NOW(), NOW(), FALSE
FROM resolved_plans v
WHERE NOT EXISTS (
    SELECT 1 FROM payments."SubscriptionPlans" p WHERE p."Code" = v."Code"
);

-- Prices: one per plan and billing interval (1 = Monthly, 2 = Yearly). Annual = 10 x monthly.
WITH price_values("Code", "BillingInterval", "Price") AS (VALUES
    ('BASIC', 1, 500000::numeric),
    ('BASIC', 2, 5000000::numeric),
    ('STANDARD', 1, 1500000::numeric),
    ('STANDARD', 2, 15000000::numeric),
    ('PREMIUM', 1, 4000000::numeric),
    ('PREMIUM', 2, 40000000::numeric)
),
resolved_prices AS (
    SELECT p."Id" AS "PlanId", v."BillingInterval", v."Price"
    FROM price_values v
    INNER JOIN payments."SubscriptionPlans" p ON p."Code" = v."Code"
),
prices_updated AS (
    UPDATE payments."SubscriptionPlanPrices" sp
    SET "Price" = v."Price", "UpdatedAtUtc" = NOW()
    FROM resolved_prices v
    WHERE sp."PlanId" = v."PlanId"
      AND sp."BillingInterval" = v."BillingInterval"
      AND sp."Price" IS DISTINCT FROM v."Price"
)
INSERT INTO payments."SubscriptionPlanPrices" (
    "Id", "PlanId", "BillingInterval", "Price", "CreatedAtUtc", "UpdatedAtUtc", "IsDeleted")
SELECT gen_random_uuid(), v."PlanId", v."BillingInterval", v."Price", NOW(), NOW(), FALSE
FROM resolved_prices v
WHERE NOT EXISTS (
    SELECT 1 FROM payments."SubscriptionPlanPrices" sp
    WHERE sp."PlanId" = v."PlanId" AND sp."BillingInterval" = v."BillingInterval"
);

-- Retire the legacy single-interval Starter plans.
UPDATE payments."SubscriptionPlans"
SET "IsActive" = FALSE, "UpdatedAtUtc" = NOW()
WHERE "Code" IN ('STARTER_MONTHLY', 'STARTER_YEARLY')
  AND "IsActive" = TRUE;
