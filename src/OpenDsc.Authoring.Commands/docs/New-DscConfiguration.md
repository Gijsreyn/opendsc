---
document type: cmdlet
external help file: OpenDsc.Authoring-Help.xml
HelpUri: ''
Locale: en-US
Module Name: OpenDsc.Authoring
ms.date: 04/17/2026
PlatyPS schema version: 2024-05-01
title: New-DscConfiguration
---

<!-- markdownlint-disable MD025 -->

# New-DscConfiguration

## SYNOPSIS

Creates a new DSC configuration builder.

## SYNTAX

```powershell
New-DscConfiguration [-Schema <string>] [<CommonParameters>]
```

## DESCRIPTION

Creates a new `DscConfigurationBuilder` that can be used to incrementally
construct a DSC v3 configuration document. Add resource instances with
`Add-DscResourceInstance`, then export the result with
`Export-DscConfiguration` or `ConvertTo-DscConfiguration`.

## EXAMPLES

### Example 1 - Create a configuration and add a resource

```powershell
$config = New-DscConfiguration
$config | Add-DscResourceInstance -Name 'SSHD' -Type 'OpenDsc.Windows/Service' -Properties @{
    name   = 'sshd'
    status = 'Running'
}
$config | Export-DscConfiguration -Path ./deploy.dsc.yaml
```

Creates a configuration, adds a Windows service resource, and exports to YAML.

### Example 2 - Create a configuration with a custom schema

```powershell
$config = New-DscConfiguration -Schema 'https://example.com/schema.json'
```

Creates a builder with a custom JSON schema URI.

## PARAMETERS

### -Schema

The JSON schema URI for the configuration document. Defaults to the official
DSC v3 bundled config document schema.

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

## INPUTS

### None

This cmdlet does not accept pipeline input.

## OUTPUTS

### OpenDsc.Authoring.Commands.DscConfigurationBuilder

A mutable builder for constructing DSC v3 configuration documents.

## NOTES

## RELATED LINKS

- [Add-DscResourceInstance](Add-DscResourceInstance.md)
- [Export-DscConfiguration](Export-DscConfiguration.md)
- [ConvertTo-DscConfiguration](ConvertTo-DscConfiguration.md)
