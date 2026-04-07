/*
    Seeds [notifications].[EmailProviders].

    Provider credentials and secrets should remain in configuration or a secret store.
    This table only controls the selectable provider definitions.
*/
SET NOCOUNT ON;

DECLARE @UtcNow datetimeoffset = SYSUTCDATETIME();

MERGE [notifications].[EmailProviders] AS [Target]
USING
(
VALUES
    (N'Logging (Stub)', N'logging', 1),
    (N'Mailtrap', N'mailtrap', 0)
) AS [Source] ([ProviderName], [ProviderKey], [IsActive])
ON [Target].[ProviderKey] = [Source].[ProviderKey]
WHEN MATCHED AND
(
    [Target].[ProviderName] <> [Source].[ProviderName]
    OR [Target].[IsActive] <> [Source].[IsActive]
)
THEN UPDATE SET
    [ProviderName] = [Source].[ProviderName],
    [IsActive] = [Source].[IsActive],
    [UpdatedAtUtc] = @UtcNow
WHEN NOT MATCHED BY TARGET
THEN INSERT
(
    [Id],
    [ProviderName],
    [ProviderKey],
    [IsActive],
    [CreatedAtUtc],
    [UpdatedAtUtc]
)
VALUES
(
    NEWID(),
    [Source].[ProviderName],
    [Source].[ProviderKey],
    [Source].[IsActive],
    @UtcNow,
    @UtcNow
);
