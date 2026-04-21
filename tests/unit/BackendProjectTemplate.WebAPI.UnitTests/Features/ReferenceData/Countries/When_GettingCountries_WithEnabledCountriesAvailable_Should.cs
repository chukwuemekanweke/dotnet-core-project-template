using BackendProjectTemplate.Application.ReferenceData.Features.GetCountries;
using BackendProjectTemplate.Domain.Common.Caching;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.ReferenceData.Entities;
using BackendProjectTemplate.WebAPI.Features.ReferenceData.Countries;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.ReferenceData.Countries;

public sealed class When_GettingCountries_WithEnabledCountriesAvailable_Should
{
    [Fact]
    public async Task ReturnCountries()
    {
        var repository = Substitute.For<IRepository<Country>>();
        var cache = Substitute.For<IJsonCache>();
        var response = new[]
        {
            new GetCountriesResponse("Nigeria", "NG", "+234", "https://example.com/ng.svg")
        };

        cache.GetAsync<GetCountriesResponse[]>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(response);

        var sut = new CountriesController(new GetCountriesHandler(repository, cache));

        var result = await sut.Handle(CancellationToken.None);

        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBe(response);
    }
}
