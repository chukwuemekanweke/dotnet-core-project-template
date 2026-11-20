using BackendProjectTemplate.Application.Analytics;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace BackendProjectTemplate.WebAPI.Features.Analytics;

public sealed class ActiveUserAnalyticsValidator : AbstractValidator<ActiveUserAnalyticsRequest>
{
    public ActiveUserAnalyticsValidator(IOptions<AdministrativeAnalyticsOptions> options, TimeProvider timeProvider)
    {
        var settings = options.Value;
        RuleFor(request => request.TenantId)
            .Must(AdministrativeAnalyticsValidation.IsNullOrNonEmpty)
            .WithMessage("TenantId must not be an empty GUID.");
        RuleFor(request => request)
            .Must(request => AdministrativeAnalyticsValidation.IsValidRange(
                request.From,
                request.To,
                settings.DefaultRangeDays,
                settings.MaximumRangeDays,
                DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime)))
            .WithMessage($"The inclusive reporting range must not be in the future and must be no longer than {settings.MaximumRangeDays} days.");
    }
}
