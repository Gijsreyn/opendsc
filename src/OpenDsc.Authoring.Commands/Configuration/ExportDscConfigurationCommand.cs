// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

namespace OpenDsc.Authoring.Commands;

/// <summary>
/// The output format for configuration document serialization.
/// </summary>
public enum DscConfigurationFormat
{
    /// <summary>YAML format (default for <c>ConvertTo-DscConfiguration</c>).</summary>
    Yaml,

    /// <summary>JSON format.</summary>
    Json,
}

/// <summary>
/// Exports a <see cref="DscConfigurationBuilder"/> to a YAML or JSON file.
/// The format is auto-detected from the file extension unless <c>-Format</c> is specified.
/// </summary>
/// <example>
/// <code>
/// $config = New-DscConfiguration
/// $config | Add-DscResourceInstance -Name 'SSHD' -Type 'OpenDsc.Windows/Service' -Properties @{
///     name = 'sshd'; status = 'Running'
/// }
///
/// # Auto-detect format from extension
/// $config | Export-DscConfiguration -Path ./deploy.dsc.yaml
/// $config | Export-DscConfiguration -Path ./deploy.dsc.json
///
/// # Explicit format
/// $config | Export-DscConfiguration -Path ./deploy.dsc -Format Json
/// </code>
/// </example>
[Cmdlet(VerbsData.Export, "DscConfiguration")]
public sealed class ExportDscConfigurationCommand : PSCmdlet
{
    /// <summary>
    /// The configuration builder to export.
    /// </summary>
    [Parameter(Mandatory = true, ValueFromPipeline = true)]
    public DscConfigurationBuilder Configuration { get; set; } = null!;

    /// <summary>
    /// The output file path. The format is inferred from the extension
    /// (<c>.yaml</c>/<c>.yml</c> → YAML, <c>.json</c> → JSON) unless overridden by <see cref="Format"/>.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    [ValidateNotNullOrEmpty]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Explicitly sets the output format, overriding file extension detection.
    /// </summary>
    [Parameter]
    public DscConfigurationFormat? Format { get; set; }

    /// <inheritdoc/>
    protected override void ProcessRecord()
    {
        var resolvedPaths = GetUnresolvedProviderPathFromPSPath(Path);
        var document = Configuration.Build();
        var format = Format ?? InferFormat(resolvedPaths);

        var content = format switch
        {
            DscConfigurationFormat.Json => DscConfigurationSerializer.ToJson(document),
            DscConfigurationFormat.Yaml => DscConfigurationSerializer.ToYaml(document),
            _ => DscConfigurationSerializer.ToYaml(document),
        };

        try
        {
            // Ensure parent directory exists
            var directory = System.IO.Path.GetDirectoryName(resolvedPaths);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(resolvedPaths, content);
            WriteVerbose($"Configuration exported to '{resolvedPaths}' as {format}.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            WriteError(new ErrorRecord(
                ex,
                "ExportFailed",
                ErrorCategory.WriteError,
                resolvedPaths));
        }
    }

    private static DscConfigurationFormat InferFormat(string path)
    {
        var ext = System.IO.Path.GetExtension(path);

        return ext.ToLowerInvariant() switch
        {
            ".json" => DscConfigurationFormat.Json,
            ".yaml" or ".yml" => DscConfigurationFormat.Yaml,
            _ => DscConfigurationFormat.Yaml,
        };
    }
}
