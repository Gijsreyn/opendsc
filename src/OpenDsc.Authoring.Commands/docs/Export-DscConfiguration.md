---
document type: cmdlet
external help file: OpenDsc.Authoring-Help.xml
HelpUri: ''
Locale: en-US
Module Name: OpenDsc.Authoring
ms.date: 04/17/2026
PlatyPS schema version: 2024-05-01
title: Export-DscConfiguration
---

<!-- markdownlint-disable MD025 -->

# Export-DscConfiguration

## SYNOPSIS

Exports a DSC configuration builder to a YAML or JSON file.

## SYNTAX

```powershell
Export-DscConfiguration [-Configuration] <DscConfigurationBuilder> [-Path] <string> [-Format <DscConfigurationFormat>] [<CommonParameters>]
```

## DESCRIPTION

Serializes a `DscConfigurationBuilder` to a file. The output format is
auto-detected from the file extension (`.yaml` or `.yml` for YAML, `.json` for
JSON) unless overridden with `-Format`. Parent directories are created
automatically.

## EXAMPLES

### Example 1 - Export to YAML

```powershell
$config = New-DscConfiguration
$config | Add-DscResourceInstance -Name 'SSHD' -Type 'OpenDsc.Windows/Service' -Properties @{
    name   = 'sshd'
    status = 'Running'
}
$config | Export-DscConfiguration -Path ./deploy.dsc.yaml
```

Exports the configuration as a YAML file, auto-detected from the `.yaml`
extension.

### Example 2 - Export to JSON

```powershell
$config | Export-DscConfiguration -Path ./deploy.dsc.json
```

Exports the configuration as a JSON file.

### Example 3 - Override format detection

```powershell
$config | Export-DscConfiguration -Path ./deploy.dsc -Format Json
```

Writes JSON regardless of the file extension.

## PARAMETERS

### -Configuration

The configuration builder to export.

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

### -Path

The output file path. The format is inferred from the extension unless
`-Format` is specified. Recognized extensions: `.yaml`, `.yml` (YAML),
`.json` (JSON). Unrecognized extensions default to YAML.

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

### -Format

Explicitly sets the output format, overriding file extension detection.

```yaml
Type: OpenDsc.Authoring.Commands.DscConfigurationFormat
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
AcceptedValues:
- Yaml
- Json
HelpMessage: ''
```

## INPUTS

### OpenDsc.Authoring.Commands.DscConfigurationBuilder

The configuration builder to export.

## OUTPUTS

### None

This cmdlet writes to a file and produces no output.

## NOTES

## RELATED LINKS

- [New-DscConfiguration](New-DscConfiguration.md)
- [ConvertTo-DscConfiguration](ConvertTo-DscConfiguration.md)
