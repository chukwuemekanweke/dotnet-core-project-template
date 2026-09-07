using BackendProjectTemplate.Domain.Common.Localization;

namespace BackendProjectTemplate.Application.ReferenceData.Features.GetLanguages;

public sealed class GetLanguagesHandler
{
    public IReadOnlyList<GetLanguagesResponse> Handle() =>
    [
        new(SupportedLanguages.English, "English", "English"),
        new(SupportedLanguages.French, "French", "Français")
    ];
}
