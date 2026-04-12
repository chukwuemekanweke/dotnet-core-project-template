using BackendProjectTemplate.Domain.Stakeholders.Entities;

namespace BackendProjectTemplate.Domain.UnitTests;

public sealed class WhenCreatingTenant_ShouldSetFieldsAndAuditDates
{
    [Fact]
    public void Verify()
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
        tenant.CreatedAtUtc.ShouldBe(utcNow);
        tenant.UpdatedAtUtc.ShouldBe(utcNow);
    }
}
