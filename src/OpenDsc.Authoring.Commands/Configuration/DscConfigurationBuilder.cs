// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Nodes;

using OpenDsc.Schema;

namespace OpenDsc.Authoring.Commands;

/// <summary>
/// A mutable builder for constructing <see cref="DscConfigDocument"/> instances incrementally.
/// </summary>
/// <example>
/// <code>
/// var builder = new DscConfigurationBuilder();
/// builder.AddResource("SSHD", "OpenDsc.Windows/Service", new Dictionary&lt;string, object?&gt; {
///     ["name"] = "sshd", ["status"] = "Running"
/// });
/// var doc = builder.Build();
/// </code>
/// </example>
public sealed class DscConfigurationBuilder
{
    private readonly List<DscConfigResource> resources = [];

    /// <summary>
    /// The JSON schema URI for the configuration document.
    /// </summary>
    public string Schema { get; set; } = "https://aka.ms/dsc/schemas/v3/bundled/config/document.json";

    /// <summary>
    /// The resource instances added to this configuration so far.
    /// </summary>
    public IReadOnlyList<DscConfigResource> Resources => resources;

    /// <summary>
    /// Adds a resource instance to the configuration.
    /// </summary>
    /// <param name="name">The unique display name for this resource instance.</param>
    /// <param name="type">The fully qualified resource type (e.g. <c>OpenDsc.Windows/Service</c>).</param>
    /// <param name="properties">The desired-state properties as key-value pairs.</param>
    /// <param name="dependsOn">
    /// Optional dependencies. Plain names are resolved to <c>[resourceId()]</c> expressions.
    /// Strings starting with <c>[</c> are passed through as raw expressions.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> or <paramref name="type"/> is null or empty,
    /// or when a dependency name cannot be resolved.
    /// </exception>
    public void AddResource(string name, string type, IDictionary<string, object?> properties, string[]? dependsOn = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(type);

        var jsonProperties = new Dictionary<string, JsonNode?>();

        foreach (var kvp in properties)
        {
            jsonProperties[kvp.Key] = ConvertToJsonNode(kvp.Value);
        }

        var resolvedDependsOn = ResolveDependsOn(dependsOn, type);

        var resource = new DscConfigResource
        {
            Name = name,
            Type = type,
            Properties = jsonProperties,
            DependsOn = resolvedDependsOn,
        };

        resources.Add(resource);
    }

    /// <summary>
    /// Builds the final <see cref="DscConfigDocument"/> from the current state.
    /// </summary>
    /// <returns>A new <see cref="DscConfigDocument"/> containing all added resources.</returns>
    public DscConfigDocument Build()
    {
        return new DscConfigDocument
        {
            Schema = Schema,
            Resources = [.. resources],
        };
    }

    /// <summary>
    /// Resolves dependsOn entries: plain names become <c>[resourceId()]</c> expressions,
    /// strings starting with <c>[</c> pass through unchanged.
    /// </summary>
    private List<string>? ResolveDependsOn(string[]? dependsOn, string currentType)
    {
        if (dependsOn is null || dependsOn.Length == 0)
        {
            return null;
        }

        var resolved = new List<string>(dependsOn.Length);

        foreach (var dep in dependsOn)
        {
            if (dep.StartsWith('['))
            {
                resolved.Add(dep);
                continue;
            }

            // Look up the resource by name to get its type
            var match = resources.FirstOrDefault(r =>
                string.Equals(r.Name, dep, StringComparison.OrdinalIgnoreCase));

            if (match is null)
            {
                throw new ArgumentException(
                    $"Cannot resolve dependency '{dep}'. No resource with that name has been added to the configuration. " +
                    $"Ensure the dependency is added before the resource that depends on it, or use a raw expression like " +
                    $"\"[resourceId('Type/Name', '{dep}')]\".");
            }

            resolved.Add($"[resourceId('{match.Type}', '{match.Name}')]");
        }

        return resolved;
    }

    /// <summary>
    /// Converts a CLR value to a <see cref="JsonNode"/> for the properties dictionary.
    /// </summary>
    private static JsonNode? ConvertToJsonNode(object? value)
    {
        return value switch
        {
            null => null,
            JsonNode node => node,
            string s => JsonValue.Create(s),
            bool b => JsonValue.Create(b),
            int i => JsonValue.Create(i),
            long l => JsonValue.Create(l),
            double d => JsonValue.Create(d),
            float f => JsonValue.Create(f),
            IDictionary<string, object?> dict => ConvertDictToJsonObject(dict),
            System.Collections.IDictionary dict => ConvertDictToJsonObject(dict),
            System.Collections.IEnumerable enumerable => ConvertEnumerableToJsonArray(enumerable),
            _ => JsonValue.Create(value.ToString()),
        };
    }

    private static JsonObject ConvertDictToJsonObject(IDictionary<string, object?> dict)
    {
        var obj = new JsonObject();

        foreach (var kvp in dict)
        {
            obj[kvp.Key] = ConvertToJsonNode(kvp.Value);
        }

        return obj;
    }

    private static JsonObject ConvertDictToJsonObject(System.Collections.IDictionary dict)
    {
        var obj = new JsonObject();

        foreach (System.Collections.DictionaryEntry entry in dict)
        {
            obj[entry.Key.ToString()!] = ConvertToJsonNode(entry.Value);
        }

        return obj;
    }

    private static JsonArray ConvertEnumerableToJsonArray(System.Collections.IEnumerable enumerable)
    {
        var array = new JsonArray();

        foreach (var item in enumerable)
        {
            array.Add(ConvertToJsonNode(item));
        }

        return array;
    }
}
