// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

namespace OpenDsc.Authoring.Commands;

/// <summary>
/// Discovers DSC resource types via <c>dsc resource list</c> and generates .NET types
/// in the caller's PowerShell session for IntelliSense-friendly configuration authoring.
/// Generated types use the <c>DSC.*</c> namespace hierarchy (e.g. <c>[DSC.OpenDsc.Windows.Service]</c>).
/// </summary>
/// <example>
/// <code>
/// # Import all command-based resources
/// Import-DscResourceType
///
/// # Import including adapted resources
/// Import-DscResourceType -IncludeAdapted
///
/// # Filter by type pattern
/// Import-DscResourceType -Type 'OpenDsc.Windows/*'
///
/// # Force re-discovery (bypass cache)
/// Import-DscResourceType -Force
/// </code>
/// </example>
[Cmdlet(VerbsData.Import, "DscResourceType")]
[OutputType(typeof(DscDiscoveredResource))]
public sealed class ImportDscResourceTypeCommand : PSCmdlet
{
    // Session-level cache of discovered resources to avoid re-invoking dsc CLI
    private static List<DscDiscoveredResource>? s_cachedResources;
    private static bool s_cacheIncludesAdapted;

    /// <summary>
    /// A wildcard pattern to filter resource types (e.g. <c>OpenDsc.Windows/*</c>).
    /// When specified, only matching resource types are imported.
    /// </summary>
    [Parameter]
    [SupportsWildcards]
    public string[]? Type { get; set; }

    /// <summary>
    /// When set, also discovers adapted resources via <c>dsc resource list --adapter '*'</c>.
    /// </summary>
    [Parameter]
    public SwitchParameter IncludeAdapted { get; set; }

    /// <summary>
    /// A specific adapter to filter by (e.g. <c>Microsoft.Adapter/PowerShell</c>).
    /// Implies <see cref="IncludeAdapted"/>.
    /// </summary>
    [Parameter]
    public string? Adapter { get; set; }

    /// <summary>
    /// Forces re-discovery, bypassing the session cache.
    /// </summary>
    [Parameter]
    public SwitchParameter Force { get; set; }

    /// <summary>
    /// When set, returns the <see cref="DscResourceTypeInfo"/> objects without generating .NET types.
    /// </summary>
    [Parameter]
    public SwitchParameter PassThru { get; set; }

    /// <inheritdoc/>
    protected override void EndProcessing()
    {
        var needsAdapted = IncludeAdapted.IsPresent || Adapter is not null;

        // Use cache if available and sufficient
        if (!Force.IsPresent && s_cachedResources is not null &&
            (!needsAdapted || s_cacheIncludesAdapted))
        {
            WriteVerbose("Using cached resource type list.");
            ProcessResources(s_cachedResources, needsAdapted);

            return;
        }

        WriteVerbose("Discovering DSC resource types...");

        List<DscDiscoveredResource> resources;

        try
        {
            resources = DscResourceTypeDiscovery.Discover(
                includeAdapted: needsAdapted,
                adapterFilter: Adapter);
        }
        catch (InvalidOperationException ex)
        {
            WriteError(new ErrorRecord(
                ex,
                "DscDiscoveryFailed",
                ErrorCategory.ResourceUnavailable,
                null));

            return;
        }

        // Update cache
        s_cachedResources = resources;
        s_cacheIncludesAdapted = needsAdapted;

        WriteVerbose($"Discovered {resources.Count} resource type(s).");
        ProcessResources(resources, needsAdapted);
    }

    private void ProcessResources(List<DscDiscoveredResource> resources, bool includeAdapted)
    {
        var filtered = FilterResources(resources);

        if (filtered.Count == 0)
        {
            WriteWarning("No resource types matched the specified filters.");

            return;
        }

        // Generate .NET types via Add-Type
        var resourcesWithSchema = filtered.Where(r => r.Schema is not null).ToList();

        if (resourcesWithSchema.Count > 0)
        {
            GenerateTypes(resourcesWithSchema);
        }

        foreach (var resource in filtered)
        {
            if (PassThru.IsPresent)
            {
                WriteObject(resource);
            }
        }

        if (!PassThru.IsPresent)
        {
            WriteVerbose($"Imported {filtered.Count} resource type(s). Use [DSC.<type>]@{{...}} syntax for IntelliSense.");
        }
    }

    private List<DscDiscoveredResource> FilterResources(List<DscDiscoveredResource> resources)
    {
        if (Type is null || Type.Length == 0)
        {
            return resources;
        }

        var patterns = Type.Select(t => new WildcardPattern(t, WildcardOptions.IgnoreCase)).ToArray();

        return resources
            .Where(r => patterns.Any(p => p.IsMatch(r.Type)))
            .ToList();
    }

    private void GenerateTypes(List<DscDiscoveredResource> resources)
    {
        var source = DscTypeGenerator.GenerateSource(resources);

        if (string.IsNullOrWhiteSpace(source))
        {
            WriteVerbose("No schemas available for type generation.");

            return;
        }

        WriteDebug("Generated C# source for Add-Type:");
        WriteDebug(source);

        try
        {
            // Use Add-Type via PowerShell to compile the generated types into the session
            using var ps = System.Management.Automation.PowerShell.Create(RunspaceMode.CurrentRunspace);
            ps.AddCommand("Add-Type").AddParameter("TypeDefinition", source).AddParameter("Language", "CSharp");
            ps.Invoke();

            if (ps.HadErrors)
            {
                foreach (var error in ps.Streams.Error)
                {
                    WriteWarning($"Type generation warning: {error.Exception.Message}");
                }
            }

            WriteVerbose($"Generated {resources.Count} .NET type(s) in the DSC.* namespace.");
        }
        catch (Exception ex)
        {
            WriteWarning($"Failed to generate .NET types: {ex.Message}. " +
                "Resources can still be used with hashtable syntax via New-DscResourceInstance.");
        }
    }

    /// <summary>
    /// Clears the session-level resource type cache. Exposed for testing.
    /// </summary>
    internal static void ClearCache()
    {
        s_cachedResources = null;
        s_cacheIncludesAdapted = false;
    }
}
