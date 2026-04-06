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
        var firstEmail = InfrastructureTestData.Email();
        var secondEmail = InfrastructureTestData.Email();
        var thirdEmail = InfrastructureTestData.Email();
        var firstName = InfrastructureTestData.FirstName();
        var secondName = InfrastructureTestData.FirstName();
        var thirdName = InfrastructureTestData.FirstName();
        var firstLastName = InfrastructureTestData.LastName();
        var secondLastName = InfrastructureTestData.LastName();
        var thirdLastName = InfrastructureTestData.LastName();
        var createdAt = new DateTimeOffset(2026, 4, 4, 0, 0, 0, TimeSpan.Zero);

        var users = new[]
        {
            AppUser.Create(firstEmail, firstName, firstLastName, createdAt),
            AppUser.Create(secondEmail, secondName, secondLastName, createdAt),
            AppUser.Create(thirdEmail, thirdName, thirdLastName, createdAt)
        }.AsQueryable();

        var specification = new OrderedPagedUsersSpecification();

        var result = SpecificationEvaluator.GetQuery(users, specification).ToList();

        var expectedEmails = users
            .OrderByDescending(user => user.FirstName)
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
            ApplyOrderByDescending(user => user.FirstName);
            ApplyPaging(0, 2);
        }
    }
}
