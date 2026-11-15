using BackendProjectTemplate.Application.Subscriptions.Features.GetSubscriptionDetail;
using FluentValidation;

namespace BackendProjectTemplate.WebAPI.Features.Subscriptions.AdminSubscriptions;

public sealed class GetSubscriptionDetailValidator : AbstractValidator<GetSubscriptionDetailRequest>
{
    public GetSubscriptionDetailValidator() =>
        RuleFor(request => request.HistoryLimit).InclusiveBetween(1, GetSubscriptionDetailHandler.MaxHistoryLimit);
}
