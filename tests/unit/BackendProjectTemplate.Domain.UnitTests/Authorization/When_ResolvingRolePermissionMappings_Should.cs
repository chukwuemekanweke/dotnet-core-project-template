using BackendProjectTemplate.Domain.Common.Authorization;

namespace BackendProjectTemplate.Domain.UnitTests.Authorization;

public sealed class When_ResolvingRolePermissionMappings_Should
{
    [Fact]
    public void EnforceLeastPrivilegeAndCombineMultipleRoles()
    {
        var compliance = RolePermissionMappings.ForRole(ApplicationRoles.ComplianceOfficer);
        compliance.ShouldContain(ApplicationPermissions.Kyc.Manage);
        compliance.ShouldNotContain(ApplicationPermissions.Payments.Manage);

        var finance = RolePermissionMappings.ForRole(ApplicationRoles.FinanceOfficer);
        finance.ShouldContain(ApplicationPermissions.Payments.Manage);
        finance.ShouldNotContain(ApplicationPermissions.Users.Manage);

        var support = RolePermissionMappings.ForRole(ApplicationRoles.SupportAgent);
        support.ShouldContain(ApplicationPermissions.Users.Read);
        support.ShouldNotContain(ApplicationPermissions.Users.Manage);
        support.ShouldNotContain(ApplicationPermissions.Kyc.Manage);

        var combined = RolePermissionMappings.ForRoles(
            [ApplicationRoles.ComplianceOfficer, ApplicationRoles.FinanceOfficer]);
        combined.ShouldContain(ApplicationPermissions.Kyc.Manage);
        combined.ShouldContain(ApplicationPermissions.Payments.Manage);
    }
}
