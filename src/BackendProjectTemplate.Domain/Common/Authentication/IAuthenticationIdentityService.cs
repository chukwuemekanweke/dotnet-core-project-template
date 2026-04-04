using BackendProjectTemplate.Domain.Authentication.Entities;
using Microsoft.AspNetCore.Identity;

namespace BackendProjectTemplate.Domain.Common.Authentication;

public interface IAuthenticationIdentityService
{
    Task<AppUser?> FindByEmailAsync(string email);
    Task<IdentityResult> CreateAsync(AppUser user, string password);
    Task<string> GenerateSignUpOtpAsync(AppUser user);
    Task<bool> VerifySignUpOtpAsync(AppUser user, string otp);
    Task<bool> CheckPasswordAsync(AppUser user, string password);
    Task<IdentityResult> UpdateAsync(AppUser user);
}
