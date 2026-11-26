using BackendProjectTemplate.Domain.Auditing;

namespace BackendProjectTemplate.Domain.UnitTests.Auditing;

public sealed class When_BuildingAdministrativeAuditChanges_Should
{
    [Fact]
    public void KeepOnlyChangedFieldsWithTheirJsonTypes()
    {
        var changes = new AdministrativeAuditChanges()
            .Field("monthlyPrice", 5000m, 7500m)
            .Field("isActive", true, false)
            .Field("name", "Basic", "Basic")
            .Field("planCode", (string?)null, "PREMIUM")
            .Field("roles", new[] { "SupportAgent" }, new[] { "FinanceOfficer", "SupportAgent" })
            .Field("features", new[] { "Email" }, new[] { "Email" });

        changes.ToJson().ShouldBe(
            "{\"monthlyPrice\":{\"previous\":5000,\"current\":7500}," +
            "\"isActive\":{\"previous\":true,\"current\":false}," +
            "\"planCode\":{\"previous\":null,\"current\":\"PREMIUM\"}," +
            "\"roles\":{\"previous\":[\"SupportAgent\"],\"current\":[\"FinanceOfficer\",\"SupportAgent\"]}}");

        var unchanged = new AdministrativeAuditChanges().Field("isActive", true, true);
        unchanged.IsEmpty.ShouldBeTrue();
        unchanged.ToJson().ShouldBeNull();

        Should.Throw<ArgumentException>(() => new AdministrativeAuditChanges().Field("IsActive", true, false));
        Should.Throw<ArgumentException>(() => new AdministrativeAuditChanges().Field("is_active", true, false));
        Should.Throw<ArgumentException>(() => new AdministrativeAuditChanges().Field("", true, false));
    }
}
