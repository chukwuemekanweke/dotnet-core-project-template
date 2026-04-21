using BackendProjectTemplate.Domain.Stakeholders.Entities;

namespace BackendProjectTemplate.Domain.UnitTests;

public sealed class WhenCreatingTenant_Should
{
    [Fact]
    public void SetFieldsAndAuditDates()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var tenantId = Guid.CreateVersion7();

        var tenant = Tenant.Create(
            tenantId,
            " Moveaex ",
            " Moveaex ",
            utcNow);

        tenant.Id.ShouldBe(tenantId);
        tenant.Name.ShouldBe("Moveaex");
        tenant.BrandKey.ShouldBe("moveaex");
        tenant.CreatedAtUtc.ShouldBe(default);
        tenant.UpdatedAtUtc.ShouldBe(default);
    }
}

