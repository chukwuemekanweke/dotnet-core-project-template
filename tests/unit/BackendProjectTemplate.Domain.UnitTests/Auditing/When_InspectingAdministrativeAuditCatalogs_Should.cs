using BackendProjectTemplate.Domain.Auditing;
using System.Reflection;
using System.Text.RegularExpressions;

namespace BackendProjectTemplate.Domain.UnitTests.Auditing;

public sealed class When_InspectingAdministrativeAuditCatalogs_Should
{
    [Fact]
    public void ExposeEveryDeclaredIdentifierWithAStableShape()
    {
        var declaredActions = typeof(AdministrativeAuditActions)
            .GetNestedTypes()
            .SelectMany(type => type.GetFields(BindingFlags.Public | BindingFlags.Static))
            .Where(field => field.IsLiteral)
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToArray();

        declaredActions.ShouldBeUnique();
        AdministrativeAuditActions.All.ShouldBe(declaredActions, ignoreOrder: true);
        declaredActions.ShouldAllBe(action => Regex.IsMatch(action, "^[a-z-]+(\\.[a-z-]+)+$"));
        AdministrativeAuditActions.Retired.Overlaps(AdministrativeAuditActions.All).ShouldBeFalse();
        AdministrativeAuditActions.IsQueryable(AdministrativeAuditActions.Users.Locked).ShouldBeTrue();
        AdministrativeAuditActions.IsQueryable("users.deleted").ShouldBeFalse();

        AdministrativeAuditTargetTypes.All.ShouldBe(
        [
            AdministrativeAuditTargetTypes.User,
            AdministrativeAuditTargetTypes.AuthenticationSession,
            AdministrativeAuditTargetTypes.Subscription,
            AdministrativeAuditTargetTypes.SubscriptionPlan,
            AdministrativeAuditTargetTypes.Provider,
            AdministrativeAuditTargetTypes.PaymentProvider
        ], ignoreOrder: true);
        AdministrativeAuditTargetTypes.IsTenantScoped(AdministrativeAuditTargetTypes.Subscription).ShouldBeTrue();
        AdministrativeAuditTargetTypes.IsTenantScoped(AdministrativeAuditTargetTypes.User).ShouldBeFalse();
        Should.Throw<ArgumentOutOfRangeException>(() => AdministrativeAuditTargetTypes.IsTenantScoped("Invoice"));
    }
}
