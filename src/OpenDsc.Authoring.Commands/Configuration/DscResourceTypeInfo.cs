// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;

namespace OpenDsc.Authoring.Commands;

/// <summary>
/// Metadata about a single DSC resource type discovered via <c>dsc resource list</c>.
/// </summary>
public sealed class DscDiscoveredResource
{
    /// <summary>
    /// The fully qualified resource type (e.g. <c>OpenDsc.Windows/Service</c>).
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The semantic version of the resource.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// A human-readable description of the resource.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The capabilities the resource supports (e.g. <c>get</c>, <c>set</c>, <c>test</c>).
    /// </summary>
    public string[] Capabilities { get; set; } = [];

    /// <summary>
    /// The adapter required to invoke this resource, or empty for command-based resources.
    /// </summary>
    public string RequireAdapter { get; set; } = string.Empty;

    /// <summary>
    /// The embedded JSON schema describing the resource properties, if available.
    /// </summary>
    public JsonElement? Schema { get; set; }

    /// <summary>
    /// The .NET type name generated for this resource (e.g. <c>DSC.OpenDsc.Windows.Service</c>).
    /// </summary>
    public string GeneratedTypeName { get; set; } = string.Empty;

    /// <summary>
    /// Converts the DSC type format (<c>Owner.Group/Name</c>) to a .NET namespace-safe format
    /// (<c>DSC.Owner.Group.Name</c>).
    /// </summary>
    /// <param name="dscType">The DSC resource type string.</param>
    /// <returns>A valid .NET fully qualified type name under the <c>DSC</c> root namespace.</returns>
    /// <example>
    /// <code>
    /// ToTypeName("OpenDsc.Windows/Service")   // "DSC.OpenDsc.Windows.Service"
    /// ToTypeName("Microsoft.DSC/Debug.Echo")  // "DSC.Microsoft.DSC.Debug.Echo"
    /// </code>
    /// </example>
    public static string ToTypeName(string dscType)
    {
        // Replace '/' with '.' to convert DSC type to .NET namespace
        var dotted = dscType.Replace('/', '.');

        return $"DSC.{dotted}";
    }

    /// <summary>
    /// Converts a generated .NET type name back to the DSC resource type format.
    /// The last segment after ignoring the <c>DSC.</c> prefix becomes the resource name
    /// separated by <c>/</c> from the namespace.
    /// </summary>
    /// <param name="typeName">A .NET type name such as <c>DSC.OpenDsc.Windows.Service</c>.</param>
    /// <returns>The DSC resource type (e.g. <c>OpenDsc.Windows/Service</c>), or <c>null</c> if the name is invalid.</returns>
    /// <example>
    /// <code>
    /// FromTypeName("DSC.OpenDsc.Windows.Service")  // "OpenDsc.Windows/Service"
    /// </code>
    /// </example>
    public static string? FromTypeName(string typeName)
    {
        if (!typeName.StartsWith("DSC.", StringComparison.Ordinal))
        {
            return null;
        }

        // Strip "DSC." prefix
        var stripped = typeName[4..];

        // The last dot separates namespace from resource name, which maps to '/'
        var lastDot = stripped.LastIndexOf('.');

        if (lastDot < 0)
        {
            return null;
        }

        var ns = stripped[..lastDot];
        var name = stripped[(lastDot + 1)..];

        return $"{ns}/{name}";
    }
}
