using BackendProjectTemplate.Domain.Authentication.Entities;

namespace BackendProjectTemplate.Domain.UnitTests.Authentication.SecurityActivities;

public sealed class When_CreatingAccountSecurityChange_WithNonChangeActivityType_Should
{
    [Theory]
    [InlineData(SecurityActivityType.InitialLogin)]
    [InlineData(SecurityActivityType.TokenRefresh)]
    [InlineData(SecurityActivityType.FailedSignIn)]
    public void Throw(SecurityActivityType activityType)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => SecurityActivity.CreateAccountSecurityChange(
            activityType,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            null,
            "Mozilla/5.0",
            null,
            null,
            null,
            DateTimeOffset.UtcNow));
    }
}
