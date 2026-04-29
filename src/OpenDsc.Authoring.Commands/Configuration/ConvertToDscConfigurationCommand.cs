// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

namespace OpenDsc.Authoring.Commands;

/// <summary>
/// Converts a <see cref="DscConfigurationBuilder"/> to a YAML or JSON string.
/// </summary>
/// <example>
/// <code>
/// $config = New-DscConfiguration
/// $config | Add-DscResourceInstance -Name 'SSHD' -Type 'OpenDsc.Windows/Service' -Properties @{
///     name = 'sshd'; status = 'Running'
/// }
///
/// # Default: YAML output
/// $config | ConvertTo-DscConfiguration
///
/// # JSON output
/// $config | ConvertTo-DscConfiguration -Format Json
/// </code>
/// </example>
[Cmdlet(VerbsData.ConvertTo, "DscConfiguration")]
[OutputType(typeof(string))]
public sealed class ConvertToDscConfigurationCommand : PSCmdlet
{
    /// <summary>
    /// The configuration builder to convert.
    /// </summary>
    [Parameter(Mandatory = true, ValueFromPipeline = true)]
    public DscConfigurationBuilder Configuration { get; set; } = null!;

    /// <summary>
    /// The output format. Defaults to <see cref="DscConfigurationFormat.Yaml"/>.
    /// </summary>
    [Parameter(Position = 0)]
    public DscConfigurationFormat Format { get; set; } = DscConfigurationFormat.Yaml;

    /// <inheritdoc/>
    protected override void ProcessRecord()
    {
        var document = Configuration.Build();

        var content = Format switch
        {
            DscConfigurationFormat.Json => DscConfigurationSerializer.ToJson(document),
            DscConfigurationFormat.Yaml => DscConfigurationSerializer.ToYaml(document),
            _ => DscConfigurationSerializer.ToYaml(document),
        };

        WriteObject(content);
    }
}
