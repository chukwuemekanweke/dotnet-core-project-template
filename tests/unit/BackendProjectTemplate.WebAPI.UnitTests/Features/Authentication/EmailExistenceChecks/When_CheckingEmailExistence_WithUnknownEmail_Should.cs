using BackendProjectTemplate.Application.Authentication.Features.CheckEmailExistence;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.WebAPI.Features.Authentication.EmailExistenceChecks;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Authentication.EmailExistenceChecks;

public sealed class When_CheckingEmailExistence_WithUnknownEmail_Should
{
    [Fact]
    public async Task ReturnDoesNotExist()
    {
        var context = new AuthenticationControllerTestContext();
        var validator = Substitute.For<IValidator<EmailExistenceCheckRequest>>();
        var request = new EmailExistenceCheckRequest("missing@example.com");
        validator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(new ValidationResult());
        context.IdentityService.FindByEmailAsync(request.Email).Returns((AppUser?)null);
        var sut = new EmailExistenceChecksController(context.CreateCheckEmailExistenceHandler(), validator);

        var result = await sut.Handle(request, CancellationToken.None);

        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBeOfType<CheckEmailExistenceResponse>().Exists.ShouldBeFalse();
    }
}
