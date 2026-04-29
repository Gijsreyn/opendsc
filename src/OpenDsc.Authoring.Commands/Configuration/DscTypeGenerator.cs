// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text;
using System.Text.Json;

namespace OpenDsc.Authoring.Commands;

/// <summary>
/// Generates C# source code from DSC resource JSON schemas for runtime compilation via
/// <c>Add-Type</c>. The generated types live under the <c>DSC.*</c> namespace hierarchy
/// and provide IntelliSense-friendly property accessors for PowerShell authoring.
/// </summary>
internal static class DscTypeGenerator
{
    /// <summary>
    /// Generates a complete C# source file containing type definitions for all provided
    /// resource type infos that have an embedded JSON schema.
    /// </summary>
    /// <param name="resources">The resource type infos to generate types for.</param>
    /// <returns>A C# source string ready for compilation via <c>Add-Type</c>.</returns>
    public static string GenerateSource(IEnumerable<DscDiscoveredResource> resources)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine();

        // Track generated enum types to avoid duplicates
        var generatedEnums = new HashSet<string>(StringComparer.Ordinal);

        foreach (var resource in resources)
        {
            if (resource.Schema is null)
            {
                continue;
            }

            var typeName = resource.GeneratedTypeName;
            var lastDot = typeName.LastIndexOf('.');

            if (lastDot < 0)
            {
                continue;
            }

            var namespaceName = typeName[..lastDot];
            var className = typeName[(lastDot + 1)..];

            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");

            GenerateEnumsFromSchema(sb, resource.Schema.Value, namespaceName, generatedEnums);
            GenerateClass(sb, className, resource.Schema.Value, namespaceName);

            sb.AppendLine("}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generates C# source for a single resource type.
    /// </summary>
    /// <param name="resource">The resource type info with an embedded schema.</param>
    /// <returns>A C# source string, or <c>null</c> if the resource has no schema.</returns>
    public static string? GenerateSingleSource(DscDiscoveredResource resource)
    {
        if (resource.Schema is null)
        {
            return null;
        }

        return GenerateSource([resource]);
    }

    private static void GenerateClass(StringBuilder sb, string className, JsonElement schema, string namespaceName)
    {
        sb.AppendLine($"    public class {SanitizeIdentifier(className)}");
        sb.AppendLine("    {");

        if (schema.TryGetProperty("properties", out var properties))
        {
            foreach (var prop in properties.EnumerateObject())
            {
                var propName = SanitizeIdentifier(prop.Name);
                var csType = JsonSchemaTypeToCSharp(prop.Value, namespaceName, prop.Name);

                sb.AppendLine($"        public {csType} {propName} {{ get; set; }}");
            }
        }

        sb.AppendLine("    }");
    }

    private static void GenerateEnumsFromSchema(
        StringBuilder sb,
        JsonElement schema,
        string namespaceName,
        HashSet<string> generatedEnums)
    {
        if (!schema.TryGetProperty("properties", out var properties))
        {
            return;
        }

        foreach (var prop in properties.EnumerateObject())
        {
            if (!prop.Value.TryGetProperty("enum", out var enumValues))
            {
                continue;
            }

            // Only generate enums for string-typed properties
            var enumType = GetSchemaType(prop.Value);

            if (enumType is not null and not "string")
            {
                continue;
            }

            var enumName = SanitizeIdentifier(prop.Name) + "Option";
            var fullEnumName = $"{namespaceName}.{enumName}";

            if (!generatedEnums.Add(fullEnumName))
            {
                continue;
            }

            sb.AppendLine($"    public enum {enumName}");
            sb.AppendLine("    {");

            foreach (var value in enumValues.EnumerateArray())
            {
                var enumMember = SanitizeIdentifier(value.GetString() ?? "Unknown");
                sb.AppendLine($"        {enumMember},");
            }

            sb.AppendLine("    }");
            sb.AppendLine();
        }
    }

    /// <summary>
    /// Maps a JSON schema type definition to a C# type string.
    /// </summary>
    private static string JsonSchemaTypeToCSharp(JsonElement propSchema, string namespaceName, string propName)
    {
        // Check for enum first — generate a typed enum
        if (propSchema.TryGetProperty("enum", out _))
        {
            var enumBaseType = GetSchemaType(propSchema);

            if (enumBaseType is null or "string")
            {
                return SanitizeIdentifier(propName) + "Option?";
            }
        }

        var typeStr = GetSchemaType(propSchema);

        if (typeStr is null)
        {
            return "object";
        }

        return typeStr switch
        {
            "string" => propSchema.TryGetProperty("format", out var fmt) && fmt.GetString() is "date-time"
                ? "DateTime?"
                : "string",
            "integer" => "int?",
            "number" => "double?",
            "boolean" => "bool?",
            "object" => "Dictionary<string, object>",
            "array" => JsonSchemaArrayToCSharp(propSchema, namespaceName, propName),
            _ => "object",
        };
    }

    /// <summary>
    /// Extracts the primary (non-null) type from a JSON schema <c>type</c> property.
    /// Handles both <c>"type": "string"</c> and <c>"type": ["string", "null"]</c>.
    /// </summary>
    private static string? GetSchemaType(JsonElement propSchema)
    {
        if (!propSchema.TryGetProperty("type", out var typeEl))
        {
            return null;
        }

        if (typeEl.ValueKind == JsonValueKind.String)
        {
            return typeEl.GetString();
        }

        if (typeEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in typeEl.EnumerateArray())
            {
                var value = item.GetString();

                if (value is not null and not "null")
                {
                    return value;
                }
            }
        }

        return null;
    }

    private static string JsonSchemaArrayToCSharp(JsonElement propSchema, string namespaceName, string propName)
    {
        if (propSchema.TryGetProperty("items", out var items) &&
            items.TryGetProperty("type", out var itemType))
        {
            var itemCsType = itemType.GetString() switch
            {
                "string" => "string",
                "integer" => "int",
                "number" => "double",
                "boolean" => "bool",
                _ => "object",
            };

            return $"{itemCsType}[]";
        }

        return "object[]";
    }

    /// <summary>
    /// Ensures a string is a valid C# identifier by replacing invalid characters.
    /// </summary>
    private static string SanitizeIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return "_empty";
        }

        var sb = new StringBuilder(name.Length);

        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];

            if (i == 0 && char.IsDigit(c))
            {
                sb.Append('_');
            }

            sb.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
        }

        var result = sb.ToString();

        // Avoid C# keywords
        return result switch
        {
            "class" or "namespace" or "enum" or "struct" or "interface"
                or "public" or "private" or "static" or "string" or "int"
                or "bool" or "double" or "object" or "event" or "delegate"
                or "default" or "null" or "true" or "false" or "new" => $"@{result}",
            _ => result,
        };
    }
}
