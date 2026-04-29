// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Collections;
using System.Management.Automation;

namespace OpenDsc.Authoring.Commands;

/// <summary>
/// Adds a DSC resource instance to a <see cref="DscConfigurationBuilder"/>. Accepts either
/// a typed <see cref="DscResourceInstanceInfo"/> from the pipeline, or inline <c>-Type</c>
/// and <c>-Properties</c> parameters. Supports <c>-DependsOn</c> with automatic name resolution.
/// </summary>
/// <example>
/// <code>
/// $config = New-DscConfiguration
///
/// # Add from inline parameters
/// $config | Add-DscResourceInstance -Name 'SSHD' -Type 'OpenDsc.Windows/Service' -Properties @{
///     name = 'sshd'
///     status = 'Running'
/// }
///
/// # Add from pipeline with DscResourceInstanceInfo
/// New-DscResourceInstance -Type 'OpenDsc.Windows/Service' -Properties @{ name = 'sshd' } |
///     Add-DscResourceInstance -Configuration $config -Name 'SSHD'
///
/// # Add with dependency
/// $config | Add-DscResourceInstance -Name 'Configure SSHD' -Type 'OpenDsc.Windows/Service' `
///     -Properties @{ name = 'sshd'; status = 'Running' } -DependsOn 'Install PS7'
/// </code>
/// </example>
[Cmdlet(VerbsCommon.Add, "DscResourceInstance")]
[OutputType(typeof(DscConfigurationBuilder))]
public sealed class AddDscResourceInstanceCommand : PSCmdlet
{
    /// <summary>
    /// The configuration builder to add the resource to. Accepts pipeline input.
    /// </summary>
    [Parameter(Mandatory = true, ValueFromPipeline = true)]
    public DscConfigurationBuilder Configuration { get; set; } = null!;

    /// <summary>
    /// The unique display name for this resource instance within the configuration.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    [ValidateNotNullOrEmpty]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The fully qualified DSC resource type (e.g. <c>OpenDsc.Windows/Service</c>).
    /// Required when not using <see cref="Instance"/>.
    /// </summary>
    [Parameter]
    [ValidateNotNullOrEmpty]
    public string? Type { get; set; }

    /// <summary>
    /// A hashtable of desired-state properties.
    /// </summary>
    [Parameter]
    public Hashtable? Properties { get; set; }

    /// <summary>
    /// A pre-built <see cref="DscResourceInstanceInfo"/> from <c>New-DscResourceInstance</c>.
    /// </summary>
    [Parameter]
    public DscResourceInstanceInfo? Instance { get; set; }

    /// <summary>
    /// A typed object from a generated <c>DSC.*</c> type. When provided with <c>-Type</c>,
    /// the object's properties are extracted automatically.
    /// </summary>
    [Parameter]
    public PSObject? TypedInstance { get; set; }

    /// <summary>
    /// Dependencies on other resource instances. Plain names are resolved to
    /// <c>[resourceId()]</c> expressions. Strings starting with <c>[</c> pass through as raw expressions.
    /// </summary>
    [Parameter]
    public string[]? DependsOn { get; set; }

    /// <summary>
    /// Returns the <see cref="DscConfigurationBuilder"/> to the pipeline for chaining.
    /// </summary>
    [Parameter]
    public SwitchParameter PassThru { get; set; }

    /// <inheritdoc/>
    protected override void ProcessRecord()
    {
        string? resolvedType;
        Dictionary<string, object?> resolvedProps;

        if (Instance is not null)
        {
            resolvedType = Type ?? Instance.Type;
            resolvedProps = Instance.Properties;
        }
        else if (TypedInstance is not null)
        {
            resolvedType = Type ?? InferTypeFromObject(TypedInstance);

            if (resolvedType is null)
            {
                WriteError(new ErrorRecord(
                    new ArgumentException(
                        "Cannot infer the resource type from -TypedInstance. " +
                        "Specify -Type explicitly, or use a generated DSC.* typed object."),
                    "CannotInferResourceType",
                    ErrorCategory.InvalidArgument,
                    null));

                return;
            }

            resolvedProps = ExtractFromTypedObject(TypedInstance);
        }
        else if (Type is not null && Properties is not null)
        {
            resolvedType = Type;
            resolvedProps = ConvertHashtable(Properties);
        }
        else
        {
            WriteError(new ErrorRecord(
                new ArgumentException(
                    "Specify either -Instance (from New-DscResourceInstance), " +
                    "-TypedInstance with -Type, or -Type with -Properties."),
                "MissingResourceInput",
                ErrorCategory.InvalidArgument,
                null));

            return;
        }

        try
        {
            Configuration.AddResource(Name, resolvedType, resolvedProps, DependsOn);
        }
        catch (ArgumentException ex)
        {
            WriteError(new ErrorRecord(
                ex,
                "AddResourceFailed",
                ErrorCategory.InvalidArgument,
                Name));

            return;
        }

        if (PassThru.IsPresent)
        {
            WriteObject(Configuration);
        }
    }

    private static Dictionary<string, object?> ConvertHashtable(Hashtable ht)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (DictionaryEntry entry in ht)
        {
            dict[entry.Key.ToString() ?? string.Empty] = ConvertValue(entry.Value);
        }

        return dict;
    }

    private static Dictionary<string, object?> ExtractFromTypedObject(PSObject psObj)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var baseObj = psObj.BaseObject;
        var type = baseObj.GetType();

        foreach (var prop in type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
        {
            if (!prop.CanRead)
            {
                continue;
            }

            var value = prop.GetValue(baseObj);

            if (value is null)
            {
                continue;
            }

            var name = char.ToLowerInvariant(prop.Name[0]) + prop.Name[1..];
            dict[name] = ConvertValue(value);
        }

        return dict;
    }

    private static object? ConvertValue(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value.GetType().IsEnum)
        {
            return value.ToString();
        }

        return value;
    }

    private static string? InferTypeFromObject(PSObject psObj)
    {
        var typeName = psObj.BaseObject.GetType().FullName;

        if (typeName is null || !typeName.StartsWith("DSC.", StringComparison.Ordinal))
        {
            return null;
        }

        return DscDiscoveredResource.FromTypeName(typeName);
    }
}
