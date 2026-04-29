<#
    .SYNOPSIS
        Demonstrates the OpenDsc.Authoring configuration cmdlets.

    .DESCRIPTION
        Builds the module, loads it, and walks through creating a DSC v3
        configuration document using the new authoring cmdlets.

    .NOTES
        Run from the repository root:  .\demo.ps1
#>

#Requires -Version 7.6

$ErrorActionPreference = 'Stop'

# ── Build and load the module ────────────────────────────────────────────────

Write-Host '▶ Building OpenDsc.Authoring.Commands...' -ForegroundColor Cyan
dotnet build src/OpenDsc.Authoring.Commands/OpenDsc.Authoring.Commands.csproj -c Debug -v quiet

$modulePath = "$PSScriptRoot/src/OpenDsc.Authoring.Commands/bin/Debug/net10.0/OpenDsc.Authoring.psd1"
Import-Module $modulePath -Force
Write-Host "  Module loaded from: $modulePath`n" -ForegroundColor DarkGray

# ── 1. Create a new configuration ───────────────────────────────────────────

Write-Host '1️⃣  New-DscConfiguration' -ForegroundColor Yellow
$config = New-DscConfiguration
$config
Write-Host ''

# ── 2. Add resources with inline properties ──────────────────────────────────

Write-Host '2️⃣  Add-DscResourceInstance (inline properties)' -ForegroundColor Yellow

$config | Add-DscResourceInstance -Name 'OpenSSH' `
    -Type 'OpenDsc.Windows/OptionalFeature' `
    -Properties @{
    name = 'OpenSSH.Server'
}

$config | Add-DscResourceInstance -Name 'SSHD' `
    -Type 'OpenDsc.Windows/Service' `
    -Properties @{
    name   = 'sshd'
    status = 'Running'
} `
    -DependsOn 'OpenSSH'

Write-Host "  Resources added: $($config.Resources.Count)"
Write-Host ''

# ── 3. Create a resource instance separately ─────────────────────────────────

Write-Host '3️⃣  New-DscResourceInstance + Add-DscResourceInstance' -ForegroundColor Yellow

$instance = New-DscResourceInstance -Type 'OpenDsc.FileSystem/Directory' -Properties @{
    path   = '/etc/ssh'
    _exist = $true
}

Write-Host "  Instance type: $($instance.Type)"
Write-Host "  Instance keys: $($instance.Properties.Keys -join ', ')"

$config | Add-DscResourceInstance -Name 'SSHDir' -Instance $instance
Write-Host "  Total resources: $($config.Resources.Count)"
Write-Host ''

# ── 4. Convert to YAML (default) ────────────────────────────────────────────

Write-Host '4️⃣  ConvertTo-DscConfiguration (YAML)' -ForegroundColor Yellow
$yaml = $config | ConvertTo-DscConfiguration
Write-Host $yaml

# ── 5. Convert to JSON ──────────────────────────────────────────────────────

Write-Host '5️⃣  ConvertTo-DscConfiguration -Format Json' -ForegroundColor Yellow
$json = $config | ConvertTo-DscConfiguration -Format Json
Write-Host $json
Write-Host ''

# ── 6. Export to files ───────────────────────────────────────────────────────

Write-Host '6️⃣  Export-DscConfiguration' -ForegroundColor Yellow

$yamlPath = Join-Path $PSScriptRoot 'artifacts/demo-config.dsc.yaml'
$jsonPath = Join-Path $PSScriptRoot 'artifacts/demo-config.dsc.json'

$config | Export-DscConfiguration -Path $yamlPath
Write-Host "  Written: $yamlPath"

$config | Export-DscConfiguration -Path $jsonPath
Write-Host "  Written: $jsonPath"
Write-Host ''

# ── 7. Pipeline chaining with -PassThru ─────────────────────────────────────

Write-Host '7️⃣  Pipeline chaining with -PassThru' -ForegroundColor Yellow

$chained = New-DscConfiguration |
Add-DscResourceInstance -Name 'TZ' -Type 'Microsoft.Windows/Registry' `
    -Properties @{ keyPath = 'HKLM:\SYSTEM\CurrentControlSet\Control\TimeZoneInformation' } -PassThru |
Add-DscResourceInstance -Name 'NTP' -Type 'OpenDsc.Windows/Service' `
    -Properties @{ name = 'w32time'; status = 'Running' } -DependsOn 'TZ' -PassThru |
ConvertTo-DscConfiguration

Write-Host $chained

# ── 8. Using generated types with Import-DscResourceType ────────────────────

Write-Host '8️⃣  Import-DscResourceType + typed objects' -ForegroundColor Yellow
Write-Host '  (requires dsc CLI on PATH — skipped if unavailable)' -ForegroundColor DarkGray

if (Get-Command dsc -ErrorAction SilentlyContinue)
{
    # Discover resources and generate DSC.* types in the session
    Import-DscResourceType -Type 'OpenDsc.Windows/*'

    $instance = [DSC.OpenDsc.Windows.Service]@{
        name      = 'sshd'
        status    = 'Running'
        startType = 'Automatic'
    }

    # Use the typed object with New-DscResourceInstance
    $typedInstance = $instance | New-DscResourceInstance

    # Or pass the typed object directly to Add-DscResourceInstance
    $typedConfig = New-DscConfiguration

    $typedConfig | ConvertTo-DscConfiguration

    # Add an optional feature that SSHD depends on
    $typedConfig | Add-DscResourceInstance -Name 'OpenSSH' `
        -Type 'OpenDsc.Windows/OptionalFeature' `
        -Properties @{ name = 'OpenSSH.Server' }

    # Add the service with a dependency
    $typedConfig | Add-DscResourceInstance -Name 'SSHD' -Instance $typedInstance `
        -DependsOn 'OpenSSH'

    $typedConfig | ConvertTo-DscConfiguration
}

