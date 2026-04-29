// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

namespace OpenDsc.Authoring.Commands;

/// <summary>
/// Creates a new <see cref="DscConfigurationBuilder"/> for incrementally constructing
/// a DSC v3 configuration document.
/// </summary>
/// <example>
/// <code>
/// $config = New-DscConfiguration
/// $config | Add-DscResourceInstance -Name 'SSHD' -Type 'OpenDsc.Windows/Service' -Properties @{
///     name = 'sshd'
///     status = 'Running'
/// }
/// $config | Export-DscConfiguration -Path ./deploy.dsc.yaml
/// </code>
/// </example>
[Cmdlet(VerbsCommon.New, "DscConfiguration")]
[OutputType(typeof(DscConfigurationBuilder))]
public sealed class NewDscConfigurationCommand : PSCmdlet
{
    /// <summary>
    /// An optional JSON schema URI to set on the configuration document.
    /// Defaults to the DSC v3 bundled schema.
    /// </summary>
    [Parameter]
    public string? Schema { get; set; }

    /// <inheritdoc/>
    protected override void EndProcessing()
    {
        var builder = new DscConfigurationBuilder();

        if (Schema is not null)
        {
            builder.Schema = Schema;
        }

        WriteObject(builder);
    }
}
