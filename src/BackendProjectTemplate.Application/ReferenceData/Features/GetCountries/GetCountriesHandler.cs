using BackendProjectTemplate.Application.ReferenceData.Specifications;
using BackendProjectTemplate.Domain.Common.Caching;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.ReferenceData.Entities;

namespace BackendProjectTemplate.Application.ReferenceData.Features.GetCountries;

public sealed class GetCountriesHandler(IRepository<Country> countries, IJsonCache cache)
{
    private const string CacheKey = "reference-data:countries";

    public async Task<IReadOnlyList<GetCountriesResponse>> HandleAsync(CancellationToken cancellationToken)
    {
        var cached = await cache.GetAsync<GetCountriesResponse[]>(CacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var response = (await countries.ListAsync(new EnabledCountriesSpecification(), cancellationToken))
            .Select(country => new GetCountriesResponse(
                country.Name,
                country.ShortCode,
                country.CallingCode,
                country.FlagUrl))
            .ToArray();

        await cache.SetAsync(CacheKey, response, TimeSpan.FromHours(12), cancellationToken);

        return response;
    }
}
