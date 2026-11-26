using BackendProjectTemplate.Domain.Auditing;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments.Entities;

namespace BackendProjectTemplate.Application.Payments.Features.ActivatePaymentProvider;

public sealed class ActivatePaymentProviderHandler(
    IRepository<PaymentProvider> paymentProviderRepository,
    IAdministrativeAuditRecorder auditRecorder,
    IUnitOfWork unitOfWork)
{
    public async Task<ActivatePaymentProviderResult> HandleAsync(
        ActivatePaymentProviderCommand command,
        CancellationToken cancellationToken)
    {
        var paymentProvider = await paymentProviderRepository.GetByIdAsync(command.PaymentProviderId, cancellationToken);
        if (paymentProvider is null)
        {
            return new ActivatePaymentProviderResult(
                ActivatePaymentProviderStatus.ProviderNotFound,
                $"Payment provider '{command.PaymentProviderId}' was not found.");
        }

        if (paymentProvider.IsActive == command.IsActive)
        {
            return new ActivatePaymentProviderResult(ActivatePaymentProviderStatus.Success);
        }

        paymentProvider.SetActive(command.IsActive);
        paymentProviderRepository.Update(paymentProvider);
        await auditRecorder.RecordAsync(
            new AdministrativeAuditEntry(
                command.ActorContext,
                command.IsActive
                    ? AdministrativeAuditActions.PaymentProviders.Activated
                    : AdministrativeAuditActions.PaymentProviders.Deactivated,
                AdministrativeAuditTargetTypes.PaymentProvider,
                paymentProvider.Id,
                paymentProvider.ProviderName,
                null,
                command.Reason,
                new AdministrativeAuditChanges().Field("isActive", !command.IsActive, command.IsActive)),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ActivatePaymentProviderResult(ActivatePaymentProviderStatus.Success);
    }
}
