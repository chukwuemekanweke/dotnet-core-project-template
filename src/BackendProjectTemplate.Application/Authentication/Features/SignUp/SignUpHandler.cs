using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Authentication.Entities;
using Microsoft.AspNetCore.Identity;

namespace BackendProjectTemplate.Application.Authentication.Features.SignUp;

public sealed class SignUpHandler(
    IAuthenticationIdentityService identityService,
    IOtpDeliveryService otpDeliveryService,
    IEventPublisher eventPublisher,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(3);

    public async Task<SignUpResult> HandleAsync(SignUpRequest request, CancellationToken cancellationToken)
    {
        if (await identityService.FindByEmailAsync(request.Email) is not null)
        {
            return new SignUpResult(SignUpStatus.DuplicateEmail, null);
        }

        var now = timeProvider.GetUtcNow();
        var user = AppUser.Create(request.Email, request.FirstName, request.LastName, now);
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        var createResult = await identityService.CreateAsync(user, request.Password);

        if (!createResult.Succeeded)
        {
            if (createResult.Errors.Any(error => error.Code is nameof(IdentityErrorDescriber.DuplicateEmail) or nameof(IdentityErrorDescriber.DuplicateUserName)))
            {
                return new SignUpResult(SignUpStatus.DuplicateEmail, null);
            }

            return new SignUpResult(SignUpStatus.ValidationFailed, null, createResult.ToValidationDictionary());
        }

        var otpCode = await identityService.GenerateSignUpOtpAsync(user);
        await eventPublisher.PublishAsync(new UserCreated(user.Id, user.Email!)
        {
            OccuredAt = now
        }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await otpDeliveryService.SendSignUpOtpAsync(user, otpCode, cancellationToken);

        return new SignUpResult(SignUpStatus.Accepted, now.Add(OtpLifetime));
    }
}
