using BackendProjectTemplate.Application.Auditing.Features.ExportAdministrativeAudits;
using BackendProjectTemplate.Application.Auditing.Features.ListAdministrativeAudits;
using BackendProjectTemplate.Domain.Auditing.ReadModels;
using BackendProjectTemplate.WebAPI.Features.Auditing.AdminAuditLogs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Auditing.AdminAuditLogs;

internal sealed class AdminAuditLogsControllerTestContext
{
    public IAdministrativeAuditReadModelRepository Repository { get; } = Substitute.For<IAdministrativeAuditReadModelRepository>();

    public AdminAuditLogsController CreateController()
    {
        var controller = new AdminAuditLogsController(
            new ListAdministrativeAuditLogsValidator(),
            new ExportAdministrativeAuditLogsValidator(),
            new ListAdministrativeAuditsHandler(Repository),
            new ExportAdministrativeAuditsHandler(Repository),
            TimeProvider.System)
        {
            ProblemDetailsFactory = new TestProblemDetailsFactory(),
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        return controller;
    }

    private sealed class TestProblemDetailsFactory : ProblemDetailsFactory
    {
        public override ProblemDetails CreateProblemDetails(
            HttpContext httpContext,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null) =>
            new() { Status = statusCode, Title = title, Type = type, Detail = detail, Instance = instance };

        public override ValidationProblemDetails CreateValidationProblemDetails(
            HttpContext httpContext,
            ModelStateDictionary modelStateDictionary,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null) =>
            new(modelStateDictionary) { Status = statusCode, Title = title, Type = type, Detail = detail, Instance = instance };
    }
}
