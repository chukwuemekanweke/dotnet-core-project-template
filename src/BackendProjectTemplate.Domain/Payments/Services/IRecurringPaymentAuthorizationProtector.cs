namespace BackendProjectTemplate.Domain.Payments.Services;

public interface IRecurringPaymentAuthorizationProtector
{
    string Protect(string authorizationReference);
    string Unprotect(string protectedAuthorizationReference);
}
