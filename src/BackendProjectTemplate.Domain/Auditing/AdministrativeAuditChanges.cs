using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace BackendProjectTemplate.Domain.Auditing;

/// <summary>
/// Builds the <c>Changes</c> payload of an administrative audit record from explicitly selected, safe fields:
/// <c>{"field": {"previous": ..., "current": ...}}</c>. Unchanged fields are dropped and JSON primitive types are
/// preserved. Callers choose every field; never pass whole entities, credentials, secrets or tokens.
/// </summary>
public sealed partial class AdministrativeAuditChanges
{
    public const string PreviousProperty = "previous";
    public const string CurrentProperty = "current";

    private readonly JsonObject _fields = [];

    public bool IsEmpty => _fields.Count == 0;

    /// <summary>Records a field when its previous and current values differ. Enum values should be passed as names.</summary>
    public AdministrativeAuditChanges Field<T>(string name, T? previous, T? current)
    {
        if (!IsValidFieldName(name))
        {
            throw new ArgumentException($"Change field '{name}' must be a camelCase identifier.", nameof(name));
        }

        var previousNode = JsonSerializer.SerializeToNode(previous);
        var currentNode = JsonSerializer.SerializeToNode(current);
        if (JsonNode.DeepEquals(previousNode, currentNode))
        {
            return this;
        }

        _fields[name] = new JsonObject
        {
            [PreviousProperty] = previousNode,
            [CurrentProperty] = currentNode
        };
        return this;
    }

    /// <summary>Serialized payload, or null when no field changed.</summary>
    public string? ToJson() => IsEmpty ? null : _fields.ToJsonString();

    public static bool IsValidFieldName(string? name) =>
        !string.IsNullOrEmpty(name) && name.Length <= 64 && FieldNamePattern().IsMatch(name);

    [GeneratedRegex("^[a-z][A-Za-z0-9]*$", RegexOptions.CultureInvariant)]
    private static partial Regex FieldNamePattern();
}
