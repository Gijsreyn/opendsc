// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Diagnostics;
using System.Text.Json;

namespace OpenDsc.Authoring.Commands;

/// <summary>
/// Discovers and caches DSC resource types by invoking the <c>dsc resource list</c> CLI command.
/// Parsed resource metadata includes type names, versions, capabilities, and embedded JSON schemas
/// used for generating .NET types and validating resource instances.
/// </summary>
internal static class DscResourceTypeDiscovery
{
    /// <summary>
    /// Invokes <c>dsc resource list</c> and parses the JSONL output into resource type infos.
    /// </summary>
    /// <param name="includeAdapted">
    /// When <c>true</c>, also runs <c>dsc resource list --adapter '*'</c> to include adapted resources.
    /// </param>
    /// <param name="adapterFilter">
    /// When set, runs <c>dsc resource list --adapter 'value'</c> to include only resources from that adapter.
    /// </param>
    /// <returns>A list of discovered resource type infos.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the DSC CLI is not found or returns an error.</exception>
    public static List<DscDiscoveredResource> Discover(bool includeAdapted = false, string? adapterFilter = null)
    {
        var results = new Dictionary<string, DscDiscoveredResource>(StringComparer.OrdinalIgnoreCase);

        // Always discover command-based resources
        var commandResources = InvokeDscResourceList(adapterArgument: null);

        foreach (var resource in commandResources)
        {
            results[resource.Type] = resource;
        }

        // Include adapted resources if requested
        if (includeAdapted || adapterFilter is not null)
        {
            var adapter = adapterFilter ?? "*";
            var adaptedResources = InvokeDscResourceList(adapterArgument: adapter);

            foreach (var resource in adaptedResources)
            {
                results.TryAdd(resource.Type, resource);
            }
        }

        return [.. results.Values];
    }

    /// <summary>
    /// Parses pre-provided JSONL lines (one JSON object per line) into resource type infos.
    /// Useful for testing or when the output has already been captured.
    /// </summary>
    /// <param name="jsonLines">Lines of JSON, each representing a resource.</param>
    /// <returns>A list of parsed resource type infos.</returns>
    public static List<DscDiscoveredResource> ParseJsonLines(IEnumerable<string> jsonLines)
    {
        var results = new List<DscDiscoveredResource>();

        foreach (var line in jsonLines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var info = ParseResourceJson(line);

            if (info is not null)
            {
                results.Add(info);
            }
        }

        return results;
    }

    private static List<DscDiscoveredResource> InvokeDscResourceList(string? adapterArgument)
    {
        var args = "resource list --output-format json";

        if (adapterArgument is not null)
        {
            args += $" --adapter '{adapterArgument}'";
        }

        var psi = new ProcessStartInfo
        {
            FileName = "dsc",
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start the 'dsc' process. Ensure DSC v3 is installed and 'dsc' is on the PATH.");

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"'dsc resource list' exited with code {process.ExitCode}. Error: {stderr}");
        }

        var lines = stdout.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);

        return ParseJsonLines(lines);
    }

    private static DscDiscoveredResource? ParseResourceJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var type = root.GetProperty("type").GetString() ?? string.Empty;

            if (string.IsNullOrEmpty(type))
            {
                return null;
            }

            var info = new DscDiscoveredResource
            {
                Type = type,
                GeneratedTypeName = DscDiscoveredResource.ToTypeName(type),
            };

            if (root.TryGetProperty("version", out var versionEl))
            {
                info.Version = versionEl.GetString() ?? string.Empty;
            }

            if (root.TryGetProperty("description", out var descEl))
            {
                info.Description = descEl.GetString() ?? string.Empty;
            }

            if (root.TryGetProperty("capabilities", out var capEl) &&
                capEl.ValueKind == JsonValueKind.Array)
            {
                info.Capabilities = capEl.EnumerateArray()
                    .Select(e => e.GetString() ?? string.Empty)
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToArray();
            }

            if (root.TryGetProperty("requireAdapter", out var adapterEl))
            {
                info.RequireAdapter = adapterEl.GetString() ?? string.Empty;
            }

            // Clone the schema element so it outlives the JsonDocument.
            // The schema can appear at root.schema.embedded or inside
            // manifest.schema.embedded depending on the resource.
            info.Schema = TryGetEmbeddedSchema(root, "schema")
                       ?? TryGetEmbeddedSchema(root, "manifest", "schema");

            return info;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static JsonElement? TryGetEmbeddedSchema(JsonElement root, params string[] path)
    {
        var current = root;

        foreach (var segment in path)
        {
            if (!current.TryGetProperty(segment, out var next) ||
                next.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            current = next;
        }

        if (current.TryGetProperty("embedded", out var embedded) &&
            embedded.ValueKind == JsonValueKind.Object)
        {
            return embedded.Clone();
        }

        return null;
    }
}
