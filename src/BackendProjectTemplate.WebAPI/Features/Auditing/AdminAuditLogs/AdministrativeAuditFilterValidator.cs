using BackendProjectTemplate.Domain.Auditing;
using BackendProjectTemplate.Domain.Auditing.Entities;
using FluentValidation;

namespace BackendProjectTemplate.WebAPI.Features.Auditing.AdminAuditLogs;

/// <summary>
/// Validation shared by the list and export. Action filters accept current and retired catalog identifiers so
/// historical records stay reachable; unknown identifiers are rejected rather than silently matching nothing.
/// </summary>
public abstract class AdministrativeAuditFilterValidator<TRequest> : AbstractValidator<TRequest>
    where TRequest : IAdministrativeAuditFilterRequest
{
    public const int MinSearchLength = 2;
    public const int MaxSearchLength = 100;

    protected AdministrativeAuditFilterValidator()
    {
        RuleFor(request => request.Search)
            .Must(value => string.IsNullOrWhiteSpace(value) || value.Trim().Length is >= MinSearchLength and <= MaxSearchLength)
            .WithMessage($"Search must contain between {MinSearchLength} and {MaxSearchLength} characters.");
        RuleFor(request => request.Action)
            .Must(value => value is null || AdministrativeAuditActions.IsQueryable(value))
            .WithMessage("Action must be a known administrative audit action.");
        RuleFor(request => request.TargetType)
            .Must(value => value is null || AdministrativeAuditTargetTypes.IsKnown(value))
            .WithMessage("TargetType must be a known administrative audit target type.");
        RuleFor(request => request.ActorUserId)
            .Must(BeNullOrNonEmpty)
            .WithMessage("ActorUserId must not be an empty GUID.");
        RuleFor(request => request.TargetId)
            .Must(BeNullOrNonEmpty)
            .WithMessage("TargetId must not be an empty GUID.");
        RuleFor(request => request.TenantId)
            .Must(BeNullOrNonEmpty)
            .WithMessage("TenantId must not be an empty GUID.");
        RuleFor(request => request.CorrelationId)
            .MaximumLength(AdministrativeAudit.MaxCorrelationLength);
        RuleFor(request => request.ToUtc)
            .GreaterThan(request => request.FromUtc)
            .When(request => request.FromUtc.HasValue && request.ToUtc.HasValue)
            .WithMessage("ToUtc must be later than FromUtc.");
    }

    private static bool BeNullOrNonEmpty(Guid? value) => !value.HasValue || value.Value != Guid.Empty;
}
