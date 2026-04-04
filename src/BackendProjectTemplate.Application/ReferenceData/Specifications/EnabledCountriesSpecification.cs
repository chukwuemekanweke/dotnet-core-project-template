using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.ReferenceData.Entities;

namespace BackendProjectTemplate.Application.ReferenceData.Specifications;

public sealed class EnabledCountriesSpecification : Specification<Country>
{
    public EnabledCountriesSpecification() => ApplyOrderBy(country => country.Name);
}
