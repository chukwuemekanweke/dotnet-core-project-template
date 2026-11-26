using Asp.Versioning;
using BackendProjectTemplate.Application.Auditing.Features.ExportAdministrativeAudits;
using BackendProjectTemplate.Application.Auditing.Features.ListAdministrativeAudits;
using BackendProjectTemplate.Domain.Auditing.ReadModels;
using BackendProjectTemplate.Domain.Common.Authorization;
using BackendProjectTemplate.WebAPI.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text;

namespace BackendProjectTemplate.WebAPI.Features.Auditing.AdminAuditLogs;

/// <summary>
/// Read-only access to the platform-wide administrative audit journal. Reading requires <c>audit.read</c> and
/// exporting requires the separate <c>audit.export</c> permission; both are initially granted to Administrator
/// only. The journal has no create, update or delete endpoints.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(EndpointUrl.AdminAuditLogs.Route)]
[ExcludeFromUserActivity]
public sealed class AdminAuditLogsController(
    ListAdministrativeAuditLogsValidator listValidator,
    ExportAdministrativeAuditLogsValidator exportValidator,
    ListAdministrativeAuditsHandler listHandler,
    ExportAdministrativeAuditsHandler exportHandler,
    TimeProvider timeProvider) : ControllerBase
{
    public const string CsvContentType = "text/csv";

    [HttpGet]
    [Authorize(Policy = PermissionPolicyNames.AuditRead)]
    [ProducesResponseType<AdministrativeAuditListPage>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AdministrativeAuditListPage>> List(
        [FromQuery] ListAdministrativeAuditLogsRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await listValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        }

        return Ok(await listHandler.HandleAsync(
            new ListAdministrativeAuditsQuery(AdministrativeAuditFilterMapper.ToFilter(request), request.Page, request.PageSize),
            cancellationToken));
    }

    /// <summary>
    /// Exports up to <see cref="ExportAdministrativeAuditsHandler.MaxRows"/> matching records as CSV, newest first.
    /// A filter matching more records returns 422 instead of a truncated file.
    /// </summary>
    [HttpGet("export")]
    [Authorize(Policy = PermissionPolicyNames.AuditExport)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, CsvContentType)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Export(
        [FromQuery] ExportAdministrativeAuditLogsRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await exportValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        }

        var result = await exportHandler.HandleAsync(
            new ExportAdministrativeAuditsQuery(AdministrativeAuditFilterMapper.ToFilter(request)),
            cancellationToken);
        if (result.Status == ExportAdministrativeAuditsStatus.TooManyRecords)
        {
            return Problem(result.Error, statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        var fileName = string.Create(
            CultureInfo.InvariantCulture,
            $"administrative-audit-{timeProvider.GetUtcNow():yyyyMMdd'T'HHmmss'Z'}.csv");
        return File(Encoding.UTF8.GetBytes(result.Csv!), CsvContentType, fileName);
    }
}
