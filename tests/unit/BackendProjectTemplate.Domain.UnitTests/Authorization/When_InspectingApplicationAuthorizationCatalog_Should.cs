using BackendProjectTemplate.Domain.Common.Authorization;

namespace BackendProjectTemplate.Domain.UnitTests.Authorization;

public sealed class When_InspectingApplicationAuthorizationCatalog_Should
{
    [Fact]
    public void ContainUniqueRolesPermissionsAndValidMappings()
    {
        ApplicationRoles.All.Count.ShouldBe(4);
        ApplicationRoles.All.Distinct(StringComparer.Ordinal).Count().ShouldBe(ApplicationRoles.All.Count);
        ApplicationRoles.All.Select(ApplicationRoles.GetId).Distinct().Count().ShouldBe(ApplicationRoles.All.Count);
        ApplicationPermissions.All.Count.ShouldBe(10);

        foreach (var role in ApplicationRoles.All)
        {
            RolePermissionMappings.ForRole(role)
                .ShouldAllBe(permission => ApplicationPermissions.All.Contains(permission));
        }

        RolePermissionMappings.ForRole(ApplicationRoles.Administrator)
            .SetEquals(ApplicationPermissions.All)
            .ShouldBeTrue();
    }
}
