using BackendProjectTemplate.Domain.Authentication.Entities;

namespace BackendProjectTemplate.Domain.UnitTests.Authentication.SecurityActivities;

public sealed class When_CreatingFailedSignIn_WithFailureReason_Should
{
    [Fact]
    public void CaptureFailedSignInWithReason()
    {
        var failureReason = "invalid_two_factor_code";

        var activity = SecurityActivity.CreateFailedSignIn(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            null,
            "Mozilla/5.0",
            "Desktop",
            "Windows",
            "Chrome",
            DateTimeOffset.UtcNow,
            failureReason);

        activity.ActivityType.ShouldBe(SecurityActivityType.FailedSignIn);
        activity.FailureReason.ShouldBe(failureReason);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RejectMissingReason(string failureReason)
    {
        Should.Throw<ArgumentException>(() => SecurityActivity.CreateFailedSignIn(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            null,
            "Mozilla/5.0",
            null,
            null,
            null,
            DateTimeOffset.UtcNow,
            failureReason));
    }
}
