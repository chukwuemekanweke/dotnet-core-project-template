using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.WebAPI.Infrastructure.StepUpMfa;
using Shouldly;
using System.Text.Json;

namespace BackendProjectTemplate.WebAPI.UnitTests.Infrastructure.StepUpMfa;

public sealed class When_CanonicalizingStepUpMfaRequests_Should
{
    private readonly StepUpMfaRequestCanonicalizer _sut = new(new StepUpMfaOperationRegistry());

    [Fact]
    public void ProduceSameFingerprintWhenJsonPropertyOrderDiffers()
    {
        using var first = JsonDocument.Parse("""{"tenantId":"a","reason":"test","nested":{"b":2,"a":1}}""");
        using var second = JsonDocument.Parse("""{"nested":{"a":1,"b":2},"reason":"test","tenantId":"a"}""");

        _sut.TryCreateFingerprint(
            StepUpMfaOperations.AllocateSubscription,
            "POST",
            "/api/v1/admin/subscriptions/allocations",
            null,
            first.RootElement,
            out var firstFingerprint).ShouldBeTrue();
        _sut.TryCreateFingerprint(
            StepUpMfaOperations.AllocateSubscription,
            "post",
            "/API/V1/admin/subscriptions/allocations/",
            null,
            second.RootElement,
            out var secondFingerprint).ShouldBeTrue();

        secondFingerprint.ShouldBe(firstFingerprint);
    }

    [Fact]
    public void ChangeFingerprintWhenTargetOrPayloadChanges()
    {
        var firstId = Guid.CreateVersion7();
        var secondId = Guid.CreateVersion7();
        using var active = JsonDocument.Parse("""{"isActive":true}""");
        using var inactive = JsonDocument.Parse("""{"isActive":false}""");

        _sut.TryCreateFingerprint(
            StepUpMfaOperations.SetSubscriptionPlanActivation,
            "PUT",
            $"/api/v1/admin/subscription-plans/{firstId}/activation",
            null,
            active.RootElement,
            out var original).ShouldBeTrue();
        _sut.TryCreateFingerprint(
            StepUpMfaOperations.SetSubscriptionPlanActivation,
            "PUT",
            $"/api/v1/admin/subscription-plans/{secondId}/activation",
            null,
            active.RootElement,
            out var differentTarget).ShouldBeTrue();
        _sut.TryCreateFingerprint(
            StepUpMfaOperations.SetSubscriptionPlanActivation,
            "PUT",
            $"/api/v1/admin/subscription-plans/{firstId}/activation",
            null,
            inactive.RootElement,
            out var differentPayload).ShouldBeTrue();

        differentTarget.ShouldNotBe(original);
        differentPayload.ShouldNotBe(original);
    }

    [Fact]
    public void RejectMethodPathAndOperationSubstitution()
    {
        _sut.TryCreateFingerprint(
            StepUpMfaOperations.UpdateSubscriptionPlan,
            "POST",
            $"/api/v1/admin/subscription-plans/{Guid.CreateVersion7()}",
            null,
            null,
            out _).ShouldBeFalse();
        _sut.TryCreateFingerprint(
            StepUpMfaOperations.UpdateSubscriptionPlan,
            "PUT",
            "/api/v1/providers/active",
            null,
            null,
            out _).ShouldBeFalse();
    }

    [Fact]
    public void BindAdministrativeUserProofsToTargetRoleAndReason()
    {
        var firstUserId = Guid.CreateVersion7();
        var secondUserId = Guid.CreateVersion7();
        using var firstReason = JsonDocument.Parse("""{"reason":"Security incident"}""");
        using var secondReason = JsonDocument.Parse("""{"reason":"Support request"}""");

        _sut.TryCreateFingerprint(
            StepUpMfaOperations.AssignUserRole,
            "PUT",
            $"/api/v1/admin/users/{firstUserId}/roles/SupportAgent",
            null,
            firstReason.RootElement,
            out var original).ShouldBeTrue();
        _sut.TryCreateFingerprint(
            StepUpMfaOperations.AssignUserRole,
            "PUT",
            $"/api/v1/admin/users/{secondUserId}/roles/SupportAgent",
            null,
            firstReason.RootElement,
            out var differentTarget).ShouldBeTrue();
        _sut.TryCreateFingerprint(
            StepUpMfaOperations.AssignUserRole,
            "PUT",
            $"/api/v1/admin/users/{firstUserId}/roles/FinanceOfficer",
            null,
            firstReason.RootElement,
            out var differentRole).ShouldBeTrue();
        _sut.TryCreateFingerprint(
            StepUpMfaOperations.AssignUserRole,
            "PUT",
            $"/api/v1/admin/users/{firstUserId}/roles/SupportAgent",
            null,
            secondReason.RootElement,
            out var differentReason).ShouldBeTrue();

        differentTarget.ShouldNotBe(original);
        differentRole.ShouldNotBe(original);
        differentReason.ShouldNotBe(original);
    }
}
