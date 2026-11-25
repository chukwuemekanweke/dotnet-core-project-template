using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Authorization;
using BackendProjectTemplate.WebAPI.Features.Payments.Providers;
using BackendProjectTemplate.WebAPI.Infrastructure.StepUpMfa;
using Microsoft.AspNetCore.Authorization;
using Shouldly;
using System.Reflection;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Payments.Providers;

public sealed class When_InspectingPaymentProviderAuthorization_Should
{
    [Fact]
    public void RequireReadForListingAndManageWithStepUpForActivation()
    {
        var controller = typeof(PaymentProvidersController);
        var list = controller.GetMethod(nameof(PaymentProvidersController.List))!;
        var setActivation = controller.GetMethod(nameof(PaymentProvidersController.SetActivation))!;

        controller.GetCustomAttributes<AuthorizeAttribute>().ShouldBeEmpty();
        controller.GetCustomAttributes<AllowAnonymousAttribute>().ShouldBeEmpty();
        list.GetCustomAttributes<AuthorizeAttribute>().ShouldHaveSingleItem()
            .Policy.ShouldBe(PermissionPolicyNames.PaymentsRead);
        list.GetCustomAttributes<RequireStepUpMfaAttribute>().ShouldBeEmpty();
        list.GetCustomAttributes<AllowAnonymousAttribute>().ShouldBeEmpty();
        setActivation.GetCustomAttributes<AuthorizeAttribute>().ShouldHaveSingleItem()
            .Policy.ShouldBe(PermissionPolicyNames.PaymentsManage);
        setActivation.GetCustomAttributes<AllowAnonymousAttribute>().ShouldBeEmpty();
        var stepUp = setActivation.GetCustomAttributes<RequireStepUpMfaAttribute>().ShouldHaveSingleItem();
        typeof(RequireStepUpMfaAttribute)
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(field => field.FieldType == typeof(string))
            .GetValue(stepUp)
            .ShouldBe(StepUpMfaOperations.SetPaymentProviderActivation);
    }
}
