using BackendProjectTemplate.Application.Authentication;
using BackendProjectTemplate.Application.Authentication.Features.ChangePassword;
using BackendProjectTemplate.Application.Authentication.Features.CheckEmailExistence;
using BackendProjectTemplate.Application.Authentication.Features.CompletePasswordReset;
using BackendProjectTemplate.Application.Authentication.Features.GoogleSignIn;
using BackendProjectTemplate.Application.Authentication.Features.GoogleSignUp;
using BackendProjectTemplate.Application.Authentication.Features.LogoutSession;
using BackendProjectTemplate.Application.Authentication.Features.RefreshSession;
using BackendProjectTemplate.Application.Authentication.Features.RequestEmailConfirmationOtp;
using BackendProjectTemplate.Application.Authentication.Features.RequestPasswordReset;
using BackendProjectTemplate.Application.Authentication.Features.SignIn;
using BackendProjectTemplate.Application.Authentication.Features.SignUp;
using BackendProjectTemplate.Application.Authentication.Features.SignUpOtp;
using BackendProjectTemplate.Application.Authentication.Stakeholders;
using BackendProjectTemplate.Application.Common.FileUploads;
using BackendProjectTemplate.Application.Notifications.Features.ProcessMailtrapDeliveryWebhook;
using BackendProjectTemplate.Application.Payments.Features.ActivatePaymentProvider;
using BackendProjectTemplate.Application.Payments.Features.GetStakeholderWalletTopUpTransactionDetail;
using BackendProjectTemplate.Application.Payments.Features.GetStakeholderWalletTransactions;
using BackendProjectTemplate.Application.Payments.Features.InitiatePayment;
using BackendProjectTemplate.Application.Payments.Features.ProcessCredoWebhook;
using BackendProjectTemplate.Application.Payments.Features.ProcessSafeHavenWebhook;
using BackendProjectTemplate.Application.Payments.Features.ReconcilePayments;
using BackendProjectTemplate.Application.Providers.Features.ActivateProvider;
using BackendProjectTemplate.Application.ReferenceData.Features.GetCountries;
using BackendProjectTemplate.Application.ReferenceData.Features.GetLanguages;
using BackendProjectTemplate.Application.Stakeholders.Features.CompleteAvatarUpload;
using BackendProjectTemplate.Application.Stakeholders.Features.CreateAvatarUpload;
using BackendProjectTemplate.Application.Stakeholders.Features.GetPreferences;
using BackendProjectTemplate.Application.Stakeholders.Features.GetProfile;
using BackendProjectTemplate.Application.Stakeholders.Features.UpdatePreferences;
using BackendProjectTemplate.Application.Stakeholders.Features.UpdateProfile;
using Microsoft.Extensions.DependencyInjection;

namespace BackendProjectTemplate.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<StakeholderResolver>();
        services.AddScoped<RegistrationCountryValidator>();
        services.AddScoped<FileUploadService>();
        services.AddScoped<CheckEmailExistenceHandler>();
        services.AddScoped<ChangePasswordHandler>();
        services.AddScoped<GoogleSignUpHandler>();
        services.AddScoped<GoogleSignInHandler>();
        services.AddScoped<CompletePasswordResetHandler>();
        services.AddScoped<LogoutSessionHandler>();
        services.AddScoped<RefreshSessionHandler>();
        services.AddScoped<SignUpHandler>();
        services.AddScoped<SignUpOtpHandler>();
        services.AddScoped<SignInHandler>();
        services.AddScoped<RequestEmailConfirmationOtpHandler>();
        services.AddScoped<RequestPasswordResetHandler>();
        services.AddScoped<ProcessMailtrapDeliveryWebhookHandler>();
        services.AddScoped<GetProfileHandler>();
        services.AddScoped<GetPreferencesHandler>();
        services.AddScoped<UpdatePreferencesHandler>();
        services.AddScoped<CreateAvatarUploadHandler>();
        services.AddScoped<CompleteAvatarUploadHandler>();
        services.AddScoped<UpdateProfileHandler>();
        services.AddScoped<ActivateProviderHandler>();
        services.AddScoped<ActivatePaymentProviderHandler>();
        services.AddScoped<GetCountriesHandler>();
        services.AddScoped<GetLanguagesHandler>();
        services.AddScoped<InitiatePaymentHandler>();
        services.AddScoped<GetStakeholderWalletTransactionsHandler>();
        services.AddScoped<GetStakeholderWalletTopUpTransactionDetailHandler>();
        services.AddScoped<ProcessSafeHavenAccountCreditWebhookHandler>();
        services.AddScoped<ProcessSafeHavenAccountDebitWebhookHandler>();
        services.AddScoped<ProcessSafeHavenVirtualAccountTransferWebhookHandler>();
        services.AddScoped<ProcessCredoWebhookHandler>();
        services.AddScoped<PaymentReconciliationService>();

        return services;
    }
}
