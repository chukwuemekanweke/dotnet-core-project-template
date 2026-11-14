using BackendProjectTemplate.Domain.Payments.Services;
using Microsoft.AspNetCore.DataProtection;

namespace BackendProjectTemplate.Infrastructure.Payments;

internal sealed class RecurringPaymentAuthorizationProtector(IDataProtectionProvider dataProtectionProvider)
    : IRecurringPaymentAuthorizationProtector
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(
        "BackendProjectTemplate.Payments.RecurringAuthorization.v1");

    public string Protect(string authorizationReference) => _protector.Protect(authorizationReference);

    public string Unprotect(string protectedAuthorizationReference) => _protector.Unprotect(protectedAuthorizationReference);
}
