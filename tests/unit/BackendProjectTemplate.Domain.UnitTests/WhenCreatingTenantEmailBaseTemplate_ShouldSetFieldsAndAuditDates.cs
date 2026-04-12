using BackendProjectTemplate.Domain.Notifications.Entities;

namespace BackendProjectTemplate.Domain.UnitTests;

public sealed class WhenCreatingTenantEmailBaseTemplate_ShouldSetFieldsAndAuditDates
{
    [Fact]
    public void Verify()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var tenantId = Guid.CreateVersion7();

        var template = TenantEmailBaseTemplate.Create(
            tenantId,
            " Primary tenant brand ",
            " <html><body>{{:BodyHtml:}}</body></html> ",
            utcNow);

        template.TenantId.ShouldBe(tenantId);
        template.Description.ShouldBe("Primary tenant brand");
        template.HtmlTemplate.ShouldBe("<html><body>{{:BodyHtml:}}</body></html>");
        template.CreatedAtUtc.ShouldBe(utcNow);
        template.UpdatedAtUtc.ShouldBe(utcNow);
    }
}
