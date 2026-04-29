---
document type: cmdlet
external help file: OpenDsc.Authoring-Help.xml
HelpUri: ''
Locale: en-US
Module Name: OpenDsc.Authoring
ms.date: 04/17/2026
PlatyPS schema version: 2024-05-01
title: ConvertTo-DscConfiguration
---

<!-- markdownlint-disable MD025 -->

# ConvertTo-DscConfiguration

## SYNOPSIS

Converts a DSC configuration builder to a YAML or JSON string.

## SYNTAX

```powershell
ConvertTo-DscConfiguration [-Configuration] <DscConfigurationBuilder> [[-Format] <DscConfigurationFormat>] [<CommonParameters>]
```

## DESCRIPTION

Serializes a `DscConfigurationBuilder` to a YAML or JSON string. Unlike
`Export-DscConfiguration`, this cmdlet returns the content as a string instead
of writing to a file. The default format is YAML.

## EXAMPLES

### Example 1 - Convert to YAML (default)

```powershell
$config = New-DscConfiguration
$config | Add-DscResourceInstance -Name 'SSHD' -Type 'OpenDsc.Windows/Service' -Properties @{
    name   = 'sshd'
    status = 'Running'
}
$config | ConvertTo-DscConfiguration
```

Returns the configuration as a YAML string.

### Example 2 - Convert to JSON

```powershell
$config | ConvertTo-DscConfiguration -Format Json
```

Returns the configuration as a formatted JSON string.

### Example 3 - Pipe to Set-Content

```powershell
$config | ConvertTo-DscConfiguration -Format Json | Set-Content -Path ./config.dsc.json
```

Converts to JSON and saves to a file using standard PowerShell cmdlets.

## PARAMETERS

### -Configuration

The configuration builder to convert.

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

### -Format

The output format. Defaults to `Yaml`.

```yaml
Type: OpenDsc.Authoring.Commands.DscConfigurationFormat
DefaultValue: Yaml
SupportsWildcards: false
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues:
- Yaml
- Json
HelpMessage: ''
```

## INPUTS

### OpenDsc.Authoring.Commands.DscConfigurationBuilder

The configuration builder to convert.

## OUTPUTS

### System.String

The serialized configuration document as a YAML or JSON string.

## NOTES

## RELATED LINKS

- [New-DscConfiguration](New-DscConfiguration.md)
- [Export-DscConfiguration](Export-DscConfiguration.md)
