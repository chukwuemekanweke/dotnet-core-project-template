using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BackendProjectTemplate.WebAPI.Infrastructure.StepUpMfa;

public sealed class StepUpMfaRequestCanonicalizer(StepUpMfaOperationRegistry operationRegistry)
{
    public bool TryCreateFingerprint(
        string operation,
        string method,
        string path,
        IReadOnlyDictionary<string, string[]>? query,
        JsonElement? body,
        out string fingerprint)
    {
        fingerprint = string.Empty;
        var normalizedPath = NormalizePath(path);
        if (!operationRegistry.Matches(operation, method, normalizedPath))
            return false;

        var canonical = new StringBuilder()
            .Append(method.ToUpperInvariant()).Append('\n')
            .Append(operation).Append('\n')
            .Append(normalizedPath).Append('\n')
            .Append(CanonicalizeQuery(query)).Append('\n')
            .Append(body.HasValue ? CanonicalizeJson(body.Value) : string.Empty)
            .ToString();
        fingerprint = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
        return true;
    }

    public async Task<string?> CreateFingerprintAsync(
        string operation,
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        JsonElement? body = null;
        request.EnableBuffering();
        using var requestBody = new MemoryStream();
        await request.Body.CopyToAsync(requestBody, cancellationToken);
        request.Body.Position = 0;
        if (requestBody.Length > 0)
        {
            requestBody.Position = 0;
            using var document = await JsonDocument.ParseAsync(requestBody, cancellationToken: cancellationToken);
            body = document.RootElement.Clone();
        }

        var query = request.Query.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Select(value => value ?? string.Empty).ToArray(),
            StringComparer.Ordinal);
        return TryCreateFingerprint(operation, request.Method, request.Path, query, body, out var fingerprint)
            ? fingerprint
            : null;
    }

    private static string NormalizePath(string path)
    {
        var normalized = path.Trim();
        if (!normalized.StartsWith('/'))
            normalized = $"/{normalized}";
        if (normalized.Length > 1)
            normalized = normalized.TrimEnd('/');
        return Uri.UnescapeDataString(normalized).ToLowerInvariant();
    }

    private static string CanonicalizeQuery(IReadOnlyDictionary<string, string[]>? query)
    {
        if (query is null || query.Count == 0)
            return string.Empty;

        return string.Join(
            "&",
            query.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .SelectMany(pair => pair.Value.Order(StringComparer.Ordinal)
                    .Select(value => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(value ?? string.Empty)}")));
    }

    private static string CanonicalizeJson(JsonElement element)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            WriteCanonicalJson(writer, element);
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteCanonicalJson(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject().OrderBy(property => property.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteCanonicalJson(writer, property.Value);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                    WriteCanonicalJson(writer, item);
                writer.WriteEndArray();
                break;
            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;
            case JsonValueKind.Number:
                writer.WriteRawValue(element.GetRawText(), skipInputValidation: true);
                break;
            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;
            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;
            default:
                writer.WriteNullValue();
                break;
        }
    }
}
