using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Infrastructure.Persistence;
using Shouldly;

namespace BackendProjectTemplate.Infrastructure.UnitTests;

public sealed class SpecificationEvaluatorTests
{
    [Fact]
    public void GetQuery_AppliesCriteriaOrderingAndPaging()
    {
        var users = new[]
        {
            AppUser.Create("linus@example.com", "Linus", "Torvalds", DateTimeOffset.UtcNow),
            AppUser.Create("ada@example.com", "Ada", "Lovelace", DateTimeOffset.UtcNow),
            AppUser.Create("grace@example.com", "Grace", "Hopper", DateTimeOffset.UtcNow)
        }.AsQueryable();

        var specification = new OrderedPagedUsersSpecification();

        var result = SpecificationEvaluator.GetQuery(users, specification).ToList();

        result.Select(user => user.Email).ShouldBe(["linus@example.com", "grace@example.com"]);
    }

    private sealed class OrderedPagedUsersSpecification : Specification<AppUser>
    {
        public OrderedPagedUsersSpecification()
        {
            Where(user => user.Email != null && user.Email.Contains("@example.com"));
            ApplyOrderByDescending(user => user.FirstName);
            ApplyPaging(0, 2);
        }
    }
}
