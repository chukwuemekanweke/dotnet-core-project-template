using BackendProjectTemplate.Application.Payments.Features.GetAdminPaymentDetail;

namespace BackendProjectTemplate.WebAPI.Features.Payments.AdminPayments;

public sealed record GetAdminPaymentDetailRequest(
    int WebhookHistoryLimit = GetAdminPaymentDetailHandler.DefaultWebhookHistoryLimit);
