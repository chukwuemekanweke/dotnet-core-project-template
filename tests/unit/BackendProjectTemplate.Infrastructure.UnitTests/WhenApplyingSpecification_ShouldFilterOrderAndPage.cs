using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Infrastructure.Persistence;
using Shouldly;

namespace BackendProjectTemplate.Infrastructure.UnitTests;

public sealed class WhenApplyingSpecification_ShouldFilterOrderAndPage
{
    [Fact]
    public void Verify()
    {
        const string firstEmail = "alpha@example.com";
        const string secondEmail = "bravo@example.com";
        const string thirdEmail = "charlie@example.com";
        var createdAt = new DateTimeOffset(2026, 4, 4, 0, 0, 0, TimeSpan.Zero);

        var users = new[]
        {
            AppUser.Create(firstEmail, createdAt),
            AppUser.Create(secondEmail, createdAt),
            AppUser.Create(thirdEmail, createdAt)
        }.AsQueryable();

        var specification = new OrderedPagedUsersSpecification();

        var result = SpecificationEvaluator.GetQuery(users, specification).ToList();

        var expectedEmails = users
            .OrderByDescending(user => user.Email)
            .Take(2)
            .Select(user => user.Email)
            .ToList();

        result.Select(user => user.Email).ShouldBe(expectedEmails);
    }

    private sealed class OrderedPagedUsersSpecification : Specification<AppUser>
    {
        public OrderedPagedUsersSpecification()
        {
            Where(user => user.Email != null && user.Email.Contains("@example.com"));
            ApplyOrderByDescending(user => user.Email);
            ApplyPaging(0, 2);
        }
    }
}
