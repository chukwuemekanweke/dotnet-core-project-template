using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.WebAPI.Features.Subscriptions;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Subscriptions;

public sealed class When_ParsingAdministrativeEnums_WithStrictParser_Should
{
    [Theory]
    [InlineData("promotion", true)]
    [InlineData("Trial", true)]
    [InlineData("2", false)]
    [InlineData("99", false)]
    [InlineData("", false)]
    [InlineData("Gift", false)]
    public void AcceptOnlyDefinedNames(string value, bool expected) =>
        StrictEnumParser.IsValid<SubscriptionAllocationSource>(value).ShouldBe(expected);
}
