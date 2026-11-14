using BackendProjectTemplate.Domain.Common.Authorization;
using BackendProjectTemplate.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using System.Security.Claims;

namespace BackendProjectTemplate.Infrastructure.UnitTests.Authorization;

public sealed class When_Authorizing_WithUnknownPermission_Should
{
    [Fact]
    public async Task FailClosed()
    {
        var requirement = new PermissionRequirement("unknown.manage");
        var permissionService = Substitute.For<IUserPermissionService>();
        var context = new AuthorizationHandlerContext(
            [requirement],
            new ClaimsPrincipal(new ClaimsIdentity([], "tests")),
            null);
        var sut = new PermissionAuthorizationHandler(
            permissionService,
            Substitute.For<ILogger<PermissionAuthorizationHandler>>());

        await sut.HandleAsync(context);

        context.HasSucceeded.ShouldBeFalse();
        await permissionService.DidNotReceiveWithAnyArgs()
            .HasPermissionAsync(default, string.Empty, default);
    }
}
