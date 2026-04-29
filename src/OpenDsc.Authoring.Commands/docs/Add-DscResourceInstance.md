---
document type: cmdlet
external help file: OpenDsc.Authoring-Help.xml
HelpUri: ''
Locale: en-US
Module Name: OpenDsc.Authoring
ms.date: 04/17/2026
PlatyPS schema version: 2024-05-01
title: Add-DscResourceInstance
---

<!-- markdownlint-disable MD025 -->

# Add-DscResourceInstance

## SYNOPSIS

Adds a resource instance to a DSC configuration builder.

## SYNTAX

```powershell
Add-DscResourceInstance [-Configuration] <DscConfigurationBuilder> [-Name] <string> [-Type <string>] [-Properties <hashtable>] [-Instance <DscResourceInstanceInfo>] [-TypedInstance <psobject>] [-DependsOn <string[]>] [-PassThru] [<CommonParameters>]
```

## DESCRIPTION

Adds a DSC resource entry to a `DscConfigurationBuilder`. There are three ways
to specify the resource:

1. **Inline** — Provide `-Type` and `-Properties` directly.
2. **Pre-built instance** — Pass a `DscResourceInstanceInfo` from
   `New-DscResourceInstance` via `-Instance`.
3. **Typed object** — Provide a `DSC.*` typed object via `-TypedInstance` along
   with `-Type`.

Dependencies can be declared with `-DependsOn`. Plain names are automatically
wrapped in `[resourceId('<type>', '<name>')]` expressions. Strings starting with
`[` are passed through as-is for DSC expression support.

## EXAMPLES

### Example 1 - Add a resource with inline properties

```powershell
$config = New-DscConfiguration
$config | Add-DscResourceInstance -Name 'SSHD' -Type 'OpenDsc.Windows/Service' -Properties @{
    name   = 'sshd'
    status = 'Running'
}
```

### Example 2 - Add a pre-built resource instance

```powershell
$instance = New-DscResourceInstance -Type 'OpenDsc.Windows/Service' -Properties @{
    name   = 'sshd'
    status = 'Running'
}
$config = New-DscConfiguration
$config | Add-DscResourceInstance -Name 'SSHD' -Instance $instance
```

### Example 3 - Add a resource with dependencies

```powershell
$config = New-DscConfiguration
$config | Add-DscResourceInstance -Name 'OpenSSH' -Type 'OpenDsc.Windows/OptionalFeature' -Properties @{
    name = 'OpenSSH.Server'
}
$config | Add-DscResourceInstance -Name 'SSHD' -Type 'OpenDsc.Windows/Service' -Properties @{
    name   = 'sshd'
    status = 'Running'
} -DependsOn 'OpenSSH'
```

The dependency `'OpenSSH'` is resolved to
`[resourceId('OpenDsc.Windows/OptionalFeature', 'OpenSSH')]`.

### Example 4 - Chain additions with PassThru

```powershell
New-DscConfiguration |
    Add-DscResourceInstance -Name 'A' -Type 'Type/A' -Properties @{ id = '1' } -PassThru |
    Add-DscResourceInstance -Name 'B' -Type 'Type/B' -Properties @{ id = '2' } -PassThru |
    Export-DscConfiguration -Path ./config.dsc.yaml
```

Uses `-PassThru` to chain multiple additions and export in one pipeline.

### Example 5 - Use a typed DSC object

```powershell
Import-DscResourceType -Type 'OpenDsc.Windows/Service'
$svc = [DSC.OpenDsc.Windows.Service]@{ Name = 'sshd'; Status = 'Running' }
$config = New-DscConfiguration
$config | Add-DscResourceInstance -Name 'SSHD' -Type 'OpenDsc.Windows/Service' -TypedInstance $svc
```

## PARAMETERS

### -Configuration

The configuration builder to add the resource to.

```yaml
Type: OpenDsc.Authoring.Commands.DscConfigurationBuilder
DefaultValue: ''
SupportsWildcards: false
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: true
  ValueFromPipeline: true
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Name

A unique display name for this resource instance within the configuration.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Type

The fully qualified DSC resource type (e.g., `OpenDsc.Windows/Service`).
Required when using `-Properties` or `-TypedInstance`.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Properties

A hashtable of desired-state property name-value pairs.

```yaml
Type: System.Collections.Hashtable
DefaultValue: ''
SupportsWildcards: false
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Instance

A pre-built `DscResourceInstanceInfo` object from `New-DscResourceInstance`.

```yaml
Type: OpenDsc.Authoring.Commands.DscResourceInstanceInfo
DefaultValue: ''
SupportsWildcards: false
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -TypedInstance

A `DSC.*` typed object. Requires `-Type` to be specified. Properties are
extracted via reflection with camelCase conversion.

```yaml
Type: System.Management.Automation.PSObject
DefaultValue: ''
SupportsWildcards: false
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -DependsOn

Names of other resources this instance depends on. Plain names are resolved to
`[resourceId()]` expressions. Strings starting with `[` are passed through.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -PassThru

Return the configuration builder to enable pipeline chaining.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: 'False'
SupportsWildcards: false
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

## INPUTS

### OpenDsc.Authoring.Commands.DscConfigurationBuilder

The configuration builder to add the resource to.

## OUTPUTS

### OpenDsc.Authoring.Commands.DscConfigurationBuilder

When `-PassThru` is specified, returns the builder for pipeline chaining.

### None

By default, no output is produced.

## NOTES

Exactly one of the following must be provided: `-Type` + `-Properties`,
`-Instance`, or `-Type` + `-TypedInstance`.

## RELATED LINKS

- [New-DscConfiguration](New-DscConfiguration.md)
- [New-DscResourceInstance](New-DscResourceInstance.md)
- [Export-DscConfiguration](Export-DscConfiguration.md)
- [ConvertTo-DscConfiguration](ConvertTo-DscConfiguration.md)
