using BackendProjectTemplate.Domain.Common.Auditing;

namespace BackendProjectTemplate.Application.Stakeholders.Features.UpdatePreferences;

public sealed record UpdatePreferencesCommand(
    string Theme,
    string Language,
    ActorContext ActorContext);
