using BackendProjectTemplate.Application.Analytics;
using BackendProjectTemplate.Domain.Common.Authorization;
using BackendProjectTemplate.WebAPI.Features.Analytics;
using BackendProjectTemplate.WebAPI.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Shouldly;
using System.Reflection;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Analytics;

public sealed class When_ValidatingAdministrativeAnalyticsEndpoints_Should
{
    [Fact]
    public async Task RejectInvalidRangesIntervalsTenantsAndPagination()
    {
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero));
        var options = Options.Create(new AdministrativeAnalyticsOptions());
        var registrations = new RegistrationAnalyticsValidator(options, timeProvider);
        var registrationList = new RegistrationUserListValidator(options, timeProvider);
        var activeList = new ActiveUserListValidator(timeProvider);

        (await registrations.ValidateAsync(new RegistrationAnalyticsRequest(
            new DateOnly(2025, 1, 1),
            new DateOnly(2026, 11, 21),
            "Quarter",
            Guid.Empty))).IsValid.ShouldBeFalse();
        (await registrationList.ValidateAsync(new RegistrationUserListRequest(
            Page: 0,
            PageSize: 101))).IsValid.ShouldBeFalse();
        (await activeList.ValidateAsync(new ActiveUserListRequest(
            new DateOnly(2026, 11, 21),
            Page: 1,
            PageSize: 20))).IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData(typeof(AdminAnalyticsController))]
    [InlineData(typeof(AdminAnalyticsUsersController))]
    public void RequireDedicatedPermissionAndExcludeReportViewsFromActivity(Type controllerType)
    {
        controllerType.GetCustomAttribute<AuthorizeAttribute>()!.Policy
            .ShouldBe(PermissionPolicyNames.UserAnalyticsRead);
        controllerType.GetCustomAttribute<ExcludeFromUserActivityAttribute>().ShouldNotBeNull();
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
