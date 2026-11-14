using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Authorization;
using BackendProjectTemplate.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace BackendProjectTemplate.Infrastructure.Authorization;

public sealed class ApplicationAuthorizationSeeder(
    AppDbContext database,
    RoleManager<IdentityRole<Guid>> roleManager,
    UserManager<AppUser> userManager,
    IOptions<AdministratorBootstrapOptions> bootstrapOptions,
    ILogger<ApplicationAuthorizationSeeder> logger) : IApplicationAuthorizationSeeder
{
    private const string BootstrapClaimType = "authorization.bootstrap";
    private const string BootstrapCompleted = "completed";
    private const long SeedLockId = 1_014_036_486;

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        await database.Database.ExecuteSqlRawAsync(
            $"SELECT pg_advisory_xact_lock({SeedLockId});",
            cancellationToken);

        var roles = new Dictionary<string, IdentityRole<Guid>>(StringComparer.Ordinal);
        foreach (var roleName in ApplicationRoles.All)
        {
            var role = await EnsureRoleAsync(roleName);
            await ReconcilePermissionClaimsAsync(role, RolePermissionMappings.ForRole(roleName));
            roles.Add(roleName, role);
        }

        await BootstrapAdministratorAsync(roles[ApplicationRoles.Administrator]);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<IdentityRole<Guid>> EnsureRoleAsync(string roleName)
    {
        var existing = await roleManager.FindByNameAsync(roleName);
        if (existing is not null)
        {
            if (!string.Equals(existing.Name, roleName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Application role '{roleName}' has an unexpected persisted name.");
            }

            return existing;
        }

        var role = new IdentityRole<Guid>
        {
            Id = ApplicationRoles.GetId(roleName),
            Name = roleName
        };
        EnsureSucceeded(await roleManager.CreateAsync(role), $"create application role '{roleName}'");
        logger.LogInformation("Created application role {RoleName}", roleName);
        return role;
    }

    private async Task ReconcilePermissionClaimsAsync(
        IdentityRole<Guid> role,
        IReadOnlySet<string> expectedPermissions)
    {
        var existingClaims = await roleManager.GetClaimsAsync(role);
        var retainedPermissions = new HashSet<string>(StringComparer.Ordinal);

        foreach (var claimGroup in existingClaims
            .Where(claim => claim.Type == CustomClaimTypes.Permission)
            .GroupBy(claim => claim.Value, StringComparer.Ordinal))
        {
            if (expectedPermissions.Contains(claimGroup.Key) && claimGroup.Count() == 1)
            {
                retainedPermissions.Add(claimGroup.Key);
                continue;
            }

            EnsureSucceeded(
                await roleManager.RemoveClaimAsync(role, claimGroup.First()),
                $"remove stale permission claim from role '{role.Name}'");
        }

        foreach (var permission in expectedPermissions.Except(retainedPermissions, StringComparer.Ordinal))
        {
            EnsureSucceeded(
                await roleManager.AddClaimAsync(role, new Claim(CustomClaimTypes.Permission, permission)),
                $"add permission '{permission}' to role '{role.Name}'");
        }
    }

    private async Task BootstrapAdministratorAsync(IdentityRole<Guid> administratorRole)
    {
        var claims = await roleManager.GetClaimsAsync(administratorRole);
        if (claims.Any(claim => claim.Type == BootstrapClaimType && claim.Value == BootstrapCompleted))
        {
            return;
        }

        var existingAdministrators = await userManager.GetUsersInRoleAsync(ApplicationRoles.Administrator);
        if (existingAdministrators.Count > 0)
        {
            await MarkBootstrapCompletedAsync(administratorRole);
            logger.LogInformation("Administrator bootstrap marked complete because an administrator already exists");
            return;
        }

        var options = bootstrapOptions.Value;
        if (!options.Enabled)
        {
            logger.LogInformation("Administrator bootstrap is disabled and no administrator exists");
            return;
        }

        if (options.UserId is not Guid userId)
        {
            throw new InvalidOperationException(
                $"'{AdministratorBootstrapOptions.SectionName}:UserId' is required when administrator bootstrap is enabled.");
        }

        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException($"Administrator bootstrap user '{userId}' does not exist.");
        if (user.IsDeleted || !user.EmailConfirmed)
        {
            throw new InvalidOperationException(
                $"Administrator bootstrap user '{userId}' must be active and have a verified email address.");
        }

        EnsureSucceeded(
            await userManager.AddToRoleAsync(user, ApplicationRoles.Administrator),
            $"assign administrator role to bootstrap user '{userId}'");
        await MarkBootstrapCompletedAsync(administratorRole);
        logger.LogWarning("Initial administrator bootstrap completed for user {UserId}", userId);
    }

    private async Task MarkBootstrapCompletedAsync(IdentityRole<Guid> administratorRole) =>
        EnsureSucceeded(
            await roleManager.AddClaimAsync(
                administratorRole,
                new Claim(BootstrapClaimType, BootstrapCompleted)),
            "mark administrator bootstrap complete");

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join(", ", result.Errors.Select(error => $"{error.Code}: {error.Description}"));
        throw new InvalidOperationException($"Unable to {operation}. {errors}");
    }
}
