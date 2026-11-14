namespace BackendProjectTemplate.Domain.Notifications.Entities;

public enum EmailDeliveryStatus
{
    Pending = 1,
    Sent = 2,
    Delivered = 3,
    Bounced = 4,
    Rejected = 5
}
