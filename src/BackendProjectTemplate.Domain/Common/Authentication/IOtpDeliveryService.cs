using BackendProjectTemplate.Domain.Identity.Entities;

namespace BackendProjectTemplate.Domain.Common.Authentication;

public interface IOtpDeliveryService
{
    Task SendSignUpOtpAsync(AppUser user, string otpCode, CancellationToken cancellationToken);
}
