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
        const string firstEmail = "linus@example.com";
        const string secondEmail = "ada@example.com";
        const string thirdEmail = "grace@example.com";
        var createdAt = new DateTimeOffset(2026, 4, 4, 0, 0, 0, TimeSpan.Zero);

        var users = new[]
        {
            AppUser.Create(firstEmail, "Linus", "Torvalds", createdAt),
            AppUser.Create(secondEmail, "Ada", "Lovelace", createdAt),
            AppUser.Create(thirdEmail, "Grace", "Hopper", createdAt)
        }.AsQueryable();

        var specification = new OrderedPagedUsersSpecification();

        var result = SpecificationEvaluator.GetQuery(users, specification).ToList();

        result.Select(user => user.Email).ShouldBe([firstEmail, thirdEmail]);
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
