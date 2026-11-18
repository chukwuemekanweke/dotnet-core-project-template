using BackendProjectTemplate.Application.Payments.Features.GetAdminPaymentDetail;
using FluentValidation;

namespace BackendProjectTemplate.WebAPI.Features.Payments.AdminPayments;

public sealed class GetAdminPaymentDetailValidator : AbstractValidator<GetAdminPaymentDetailRequest>
{
    public GetAdminPaymentDetailValidator() =>
        RuleFor(request => request.WebhookHistoryLimit)
            .InclusiveBetween(1, GetAdminPaymentDetailHandler.MaxWebhookHistoryLimit);
}
