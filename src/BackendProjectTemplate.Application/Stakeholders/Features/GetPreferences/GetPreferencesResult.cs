namespace BackendProjectTemplate.Application.Stakeholders.Features.GetPreferences;

public sealed record GetPreferencesResult(
    GetPreferencesStatus Status,
    GetPreferencesResponse? Preferences = null);
