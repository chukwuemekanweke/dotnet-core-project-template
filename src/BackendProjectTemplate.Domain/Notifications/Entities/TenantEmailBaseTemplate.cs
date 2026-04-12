using BackendProjectTemplate.Domain.Common.Entities;

namespace BackendProjectTemplate.Domain.Notifications.Entities;

public sealed class TenantEmailBaseTemplate : Entity
{
    private TenantEmailBaseTemplate()
    {
    }

    private TenantEmailBaseTemplate(
        Guid tenantId,
        string description,
        string htmlTemplate,
        DateTimeOffset utcNow)
    {
        TenantId = tenantId;
        Description = description.Trim();
        HtmlTemplate = htmlTemplate.Trim();
        SetAuditDates(utcNow);
    }

    public Guid TenantId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string HtmlTemplate { get; private set; } = string.Empty;

    public static TenantEmailBaseTemplate Create(
        Guid tenantId,
        string description,
        string htmlTemplate,
        DateTimeOffset utcNow) =>
        new(tenantId, description, htmlTemplate, utcNow);
}
