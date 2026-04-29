// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;

using OpenDsc.Schema;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace OpenDsc.Authoring.Commands;

/// <summary>
/// Serializes <see cref="DscConfigDocument"/> instances to YAML or JSON format.
/// </summary>
internal static class DscConfigurationSerializer
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly ISerializer s_yamlSerializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .Build();

    /// <summary>
    /// Serializes a configuration document to JSON.
    /// </summary>
    /// <param name="document">The configuration document to serialize.</param>
    /// <returns>A JSON string representation.</returns>
    public static string ToJson(DscConfigDocument document)
    {
        var obj = ToSerializableObject(document);

        return JsonSerializer.Serialize(obj, s_jsonOptions);
    }

    /// <summary>
    /// Serializes a configuration document to YAML.
    /// </summary>
    /// <param name="document">The configuration document to serialize.</param>
    /// <returns>A YAML string representation.</returns>
    public static string ToYaml(DscConfigDocument document)
    {
        var obj = ToSerializableObject(document);

        return s_yamlSerializer.Serialize(obj);
    }

    /// <summary>
    /// Converts the document to a plain dictionary structure suitable for both JSON and YAML serializers.
    /// This avoids issues with JsonNode not being recognized by YamlDotNet.
    /// </summary>
    private static Dictionary<string, object?> ToSerializableObject(DscConfigDocument document)
    {
        var resources = new List<Dictionary<string, object?>>();

        foreach (var resource in document.Resources)
        {
            var resourceDict = new Dictionary<string, object?>
            {
                ["type"] = resource.Type,
                ["name"] = resource.Name,
                ["properties"] = ConvertProperties(resource.Properties),
            };

            if (resource.DependsOn is not null && resource.DependsOn.Count > 0)
            {
                resourceDict["dependsOn"] = resource.DependsOn.ToList();
            }

            resources.Add(resourceDict);
        }

        return new Dictionary<string, object?>
        {
            ["$schema"] = document.Schema,
            ["resources"] = resources,
        };
    }

    private static Dictionary<string, object?> ConvertProperties(IReadOnlyDictionary<string, JsonNode?> properties)
    {
        var dict = new Dictionary<string, object?>();

        foreach (var kvp in properties)
        {
            dict[kvp.Key] = ConvertJsonNode(kvp.Value);
        }

        return dict;
    }

    private static object? ConvertJsonNode(JsonNode? node)
    {
        if (node is null)
        {
            return null;
        }

        if (node is JsonValue value)
        {
            if (value.TryGetValue<bool>(out var boolVal))
            {
                return boolVal;
            }

            if (value.TryGetValue<int>(out var intVal))
            {
                return intVal;
            }

            if (value.TryGetValue<long>(out var longVal))
            {
                return longVal;
            }

            if (value.TryGetValue<double>(out var doubleVal))
            {
                return doubleVal;
            }

            if (value.TryGetValue<string>(out var strVal))
            {
                return strVal;
            }

            return value.ToString();
        }

        if (node is JsonObject obj)
        {
            var dict = new Dictionary<string, object?>();

            foreach (var prop in obj)
            {
                dict[prop.Key] = ConvertJsonNode(prop.Value);
            }

            return dict;
        }

        if (node is JsonArray array)
        {
            return array.Select(ConvertJsonNode).ToList();
        }

        return node.ToString();
    }
}
