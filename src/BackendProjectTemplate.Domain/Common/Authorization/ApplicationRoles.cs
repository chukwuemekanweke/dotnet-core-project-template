namespace BackendProjectTemplate.Domain.Common.Authorization;

public static class ApplicationRoles
{
    public const string Administrator = "Administrator";
    public const string ComplianceOfficer = "ComplianceOfficer";
    public const string FinanceOfficer = "FinanceOfficer";
    public const string SupportAgent = "SupportAgent";

    private static readonly IReadOnlyDictionary<string, Guid> RoleIds = new Dictionary<string, Guid>(StringComparer.Ordinal)
    {
        [Administrator] = Guid.Parse("019a86a6-2eed-7a1d-a8f0-f78309f45801"),
        [ComplianceOfficer] = Guid.Parse("019a86a6-2eed-7a1d-a8f0-f78309f45802"),
        [SupportAgent] = Guid.Parse("019a86a6-2eed-7a1d-a8f0-f78309f45803"),
        [FinanceOfficer] = Guid.Parse("019a86a6-2eed-7a1d-a8f0-f78309f45804")
    };

    public static IReadOnlyCollection<string> All { get; } = RoleIds.Keys.ToArray();

    public static Guid GetId(string roleName) => RoleIds[roleName];
}
