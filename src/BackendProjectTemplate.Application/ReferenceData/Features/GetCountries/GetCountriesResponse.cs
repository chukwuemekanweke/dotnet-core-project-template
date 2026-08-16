namespace BackendProjectTemplate.Application.ReferenceData.Features.GetCountries;

public sealed record GetCountriesResponse(
    Guid CountryId,
    string Name,
    string ShortCode,
    string? CallingCode,
    string FlagUrl);
