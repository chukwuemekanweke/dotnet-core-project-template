using BackendProjectTemplate.Domain.Authentication.Entities;

namespace BackendProjectTemplate.Domain.UnitTests.Authentication;

public sealed class When_AdministrativelyLockingAppUser_Should
{
    [Fact]
    public void PreserveDedicatedStateAndRemainIdempotent()
    {
        var user = AppUser.Create("locked@example.com");
        var actorUserId = Guid.CreateVersion7();
        var lockedAtUtc = DateTimeOffset.Parse("2026-11-17T02:00:00Z");

        user.LockAdministratively(actorUserId, lockedAtUtc).ShouldBeTrue();
        user.LockAdministratively(actorUserId, lockedAtUtc.AddMinutes(1)).ShouldBeFalse();
        user.IsAdministrativelyLocked.ShouldBeTrue();
        user.AdministrativelyLockedAtUtc.ShouldBe(lockedAtUtc);
        user.AdministrativelyLockedByUserId.ShouldBe(actorUserId);

        user.UnlockAdministratively().ShouldBeTrue();
        user.UnlockAdministratively().ShouldBeFalse();
        user.IsAdministrativelyLocked.ShouldBeFalse();
        user.AdministrativelyLockedAtUtc.ShouldBeNull();
        user.AdministrativelyLockedByUserId.ShouldBeNull();
    }
}
