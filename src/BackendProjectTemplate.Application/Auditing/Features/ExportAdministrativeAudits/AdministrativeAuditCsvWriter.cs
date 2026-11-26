using BackendProjectTemplate.Domain.Auditing.ReadModels;
using System.Globalization;
using System.Text;

namespace BackendProjectTemplate.Application.Auditing.Features.ExportAdministrativeAudits;

/// <summary>
/// Writes RFC 4180 CSV. Display names and reasons are untrusted text: any cell that a spreadsheet could
/// interpret as a formula is prefixed with an apostrophe, and cells containing separators, quotes or line breaks
/// are quoted with embedded quotes doubled.
/// </summary>
public static class AdministrativeAuditCsvWriter
{
    public static readonly IReadOnlyList<string> Headers =
    [
        "id",
        "occurredAtUtc",
        "actorUserId",
        "actorDisplayName",
        "action",
        "targetType",
        "targetId",
        "targetDisplayName",
        "reason",
        "targetTenantId",
        "correlationId"
    ];

    private static readonly char[] FormulaPrefixes = ['=', '+', '-', '@', '\t', '\r'];
    private static readonly char[] QuotedCharacters = [',', '"', '\r', '\n'];

    public static string Write(IEnumerable<AdministrativeAuditExportRow> rows)
    {
        var builder = new StringBuilder();
        AppendRow(builder, Headers);
        foreach (var row in rows)
        {
            AppendRow(builder,
            [
                row.Id.ToString(),
                row.OccurredAtUtc.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture),
                row.ActorUserId.ToString(),
                row.ActorDisplayName,
                row.Action,
                row.TargetType,
                row.TargetId.ToString(),
                row.TargetDisplayName,
                row.Reason,
                row.TargetTenantId?.ToString(),
                row.CorrelationId
            ]);
        }

        return builder.ToString();
    }

    public static string EscapeCell(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var safe = value.IndexOfAny(FormulaPrefixes) == 0 ? "'" + value : value;
        return safe.IndexOfAny(QuotedCharacters) >= 0
            ? "\"" + safe.Replace("\"", "\"\"", StringComparison.Ordinal) + "\""
            : safe;
    }

    private static void AppendRow(StringBuilder builder, IReadOnlyList<string?> cells)
    {
        for (var index = 0; index < cells.Count; index++)
        {
            if (index > 0)
            {
                builder.Append(',');
            }

            builder.Append(EscapeCell(cells[index]));
        }

        builder.Append("\r\n");
    }
}
