using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Authentication.Entities;
using Microsoft.AspNetCore.Identity;

namespace BackendProjectTemplate.Infrastructure.Authentication;

public sealed class IdentityUserService(UserManager<AppUser> userManager) : IAuthenticationIdentityService
{
    public Task<AppUser?> FindByIdAsync(Guid userId) =>
        userManager.FindByIdAsync(userId.ToString());

    public Task<AppUser?> FindByEmailAsync(string email) =>
        userManager.FindByEmailAsync(email);

    public Task<AppUser?> FindByLoginAsync(string loginProvider, string providerKey) =>
        userManager.FindByLoginAsync(loginProvider, providerKey);

    public Task<bool> IsLockedOutAsync(AppUser user) =>
        userManager.IsLockedOutAsync(user);

    public async Task<DateTimeOffset?> GetLockoutEndUtcAsync(AppUser user)
    {
        var lockoutEnd = await userManager.GetLockoutEndDateAsync(user);
        return lockoutEnd?.ToUniversalTime();
    }

    public Task<IdentityResult> CreateAsync(AppUser user) =>
        userManager.CreateAsync(user);

    public Task<IdentityResult> CreateAsync(AppUser user, string password) =>
        userManager.CreateAsync(user, password);

    public Task<IdentityResult> AddLoginAsync(AppUser user, string loginProvider, string providerKey, string displayName) =>
        userManager.AddLoginAsync(user, new UserLoginInfo(loginProvider, providerKey, displayName));

    public Task<string> GenerateSignUpOtpAsync(AppUser user) =>
        userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);

    public Task<bool> VerifySignUpOtpAsync(AppUser user, string otp) =>
        userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider, otp);

    public Task<bool> CheckPasswordAsync(AppUser user, string password) =>
        userManager.CheckPasswordAsync(user, password);

    public Task<IdentityResult> AccessFailedAsync(AppUser user) =>
        userManager.AccessFailedAsync(user);

    public Task<IdentityResult> ResetAccessFailedCountAsync(AppUser user) =>
        userManager.ResetAccessFailedCountAsync(user);

    public Task<IdentityResult> UpdateAsync(AppUser user) =>
        userManager.UpdateAsync(user);
}
