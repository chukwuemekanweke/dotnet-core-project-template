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

    public Task<IdentityResult> CreateAsync(AppUser user, string password) =>
        userManager.CreateAsync(user, password);

    public Task<string> GenerateSignUpOtpAsync(AppUser user) =>
        userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);

    public Task<bool> VerifySignUpOtpAsync(AppUser user, string otp) =>
        userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider, otp);

    public Task<bool> CheckPasswordAsync(AppUser user, string password) =>
        userManager.CheckPasswordAsync(user, password);

    public Task<IdentityResult> UpdateAsync(AppUser user) =>
        userManager.UpdateAsync(user);
}
