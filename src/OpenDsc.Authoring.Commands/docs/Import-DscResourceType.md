---
document type: cmdlet
external help file: OpenDsc.Authoring-Help.xml
HelpUri: ''
Locale: en-US
Module Name: OpenDsc.Authoring
ms.date: 04/17/2026
PlatyPS schema version: 2024-05-01
title: Import-DscResourceType
---

<!-- markdownlint-disable MD025 -->

# Import-DscResourceType

## SYNOPSIS

Discovers DSC resource types and generates .NET types in the current session.

## SYNTAX

```powershell
Import-DscResourceType [-Type <string[]>] [-IncludeAdapted] [-Adapter <string>] [-Force] [-PassThru] [<CommonParameters>]
```

## DESCRIPTION

Runs `dsc resource list` to discover available DSC v3 resources and compiles
.NET types into the current PowerShell session under the `DSC.*` namespace.
These types provide IntelliSense and tab-completion when authoring
configurations with `New-DscResourceInstance`.

Results are cached for the session. Use `-Force` to re-discover and regenerate
types. Use `-PassThru` to return the discovered resource metadata instead of
generating types.

## EXAMPLES

### Example 1 - Import all native resource types

```powershell
Import-DscResourceType
```

Discovers all non-adapted DSC resources and generates `DSC.*` types in the
session.

### Example 2 - Import resources matching a wildcard

```powershell
Import-DscResourceType -Type 'OpenDsc.Windows/*'
```

Imports only resources whose type matches the wildcard pattern.

### Example 3 - Include adapted resources

```powershell
Import-DscResourceType -IncludeAdapted
```

Discovers both native and adapter-wrapped resources.

### Example 4 - List available resources without generating types

```powershell
Import-DscResourceType -PassThru
```

Returns `DscDiscoveredResource` objects for inspection without compiling types.

### Example 5 - Force re-discovery

```powershell
Import-DscResourceType -Force
```

Bypasses the session cache and re-runs `dsc resource list`.

## PARAMETERS

### -Type

One or more wildcard patterns to filter which resource types are imported.
When omitted, all discovered resources are imported.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: true
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

### -IncludeAdapted

Include resources exposed through adapters (e.g., PowerShell DSC v1 resources).

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

### -Adapter

Filters discovered resources to those provided by a specific adapter. Implies
`-IncludeAdapted`.

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

### -Force

Bypass the session-level cache and re-discover resources from the DSC CLI.

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

### -PassThru

Return `DscDiscoveredResource` objects instead of generating .NET types.

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

### None

This cmdlet does not accept pipeline input.

## OUTPUTS

### OpenDsc.Authoring.Commands.DscDiscoveredResource

When `-PassThru` is specified, returns metadata about each discovered resource.

### None

By default, generates types in the session and produces no output.

## NOTES

The generated types use the naming convention `DSC.<Type>` where slashes are
replaced with dots. For example, `OpenDsc.Windows/Service` becomes
`DSC.OpenDsc.Windows.Service`.

## RELATED LINKS

- [New-DscResourceInstance](New-DscResourceInstance.md)
- [New-DscConfiguration](New-DscConfiguration.md)
