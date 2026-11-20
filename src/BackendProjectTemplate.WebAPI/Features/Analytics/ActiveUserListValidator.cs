using FluentValidation;

namespace BackendProjectTemplate.WebAPI.Features.Analytics;

public sealed class ActiveUserListValidator : AbstractValidator<ActiveUserListRequest>
{
    public ActiveUserListValidator(TimeProvider timeProvider)
    {
        RuleFor(request => request.Date).NotNull();
        RuleFor(request => request.Date)
            .Must(date => !date.HasValue || date.Value <= DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime))
            .WithMessage("Date must not be in the future.");
        RuleFor(request => request.TenantId)
            .Must(AdministrativeAnalyticsValidation.IsNullOrNonEmpty)
            .WithMessage("TenantId must not be an empty GUID.");
        RuleFor(request => request.Page).GreaterThan(0);
        RuleFor(request => request.PageSize).InclusiveBetween(1, 100);
    }
}
