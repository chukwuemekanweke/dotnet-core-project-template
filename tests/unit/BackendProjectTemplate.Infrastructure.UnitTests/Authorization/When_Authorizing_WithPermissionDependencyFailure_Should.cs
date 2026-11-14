using BackendProjectTemplate.Domain.Common.Authorization;
using BackendProjectTemplate.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BackendProjectTemplate.Infrastructure.UnitTests.Authorization;

public sealed class When_Authorizing_WithPermissionDependencyFailure_Should
{
    [Fact]
    public async Task FailClosed()
    {
        var userId = Guid.CreateVersion7();
        var requirement = new PermissionRequirement(ApplicationPermissions.Users.Read);
        var permissionService = Substitute.For<IUserPermissionService>();
        permissionService
            .HasPermissionAsync(userId, requirement.Permission, Arg.Any<CancellationToken>())
            .Returns<Task<bool>>(_ => throw new InvalidOperationException("Unavailable"));
        var context = new AuthorizationHandlerContext(
            [requirement],
            new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())],
                "tests")),
            null);
        var sut = new PermissionAuthorizationHandler(
            permissionService,
            Substitute.For<ILogger<PermissionAuthorizationHandler>>());

        await sut.HandleAsync(context);

        context.HasSucceeded.ShouldBeFalse();
    }
}
