// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;
using System.Management.Automation.Runspaces;

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Authoring.Commands.Tests;

[Trait("Category", "Unit")]
public class NewDscConfigurationCommandTests
{
    private static Runspace CreateRunspace()
    {
        var iss = InitialSessionState.CreateDefault2();
        iss.Commands.Add(new SessionStateCmdletEntry(
            "New-DscConfiguration",
            typeof(NewDscConfigurationCommand), null));

        var runspace = RunspaceFactory.CreateRunspace(iss);
        runspace.Open();

        return runspace;
    }

    [Fact]
    public void Invoke_ReturnsConfigurationBuilder()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("New-DscConfiguration");

        var results = ps.Invoke<DscConfigurationBuilder>();

        results.Should().HaveCount(1);
        results[0].Should().NotBeNull();
    }

    [Fact]
    public void Invoke_DefaultSchema_IsDscV3()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("New-DscConfiguration");

        var builder = ps.Invoke<DscConfigurationBuilder>().Single();

        builder.Schema.Should().Be("https://aka.ms/dsc/schemas/v3/bundled/config/document.json");
    }

    [Fact]
    public void Invoke_CustomSchema_Applied()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("New-DscConfiguration")
            .AddParameter("Schema", "https://custom.schema/v1");

        var builder = ps.Invoke<DscConfigurationBuilder>().Single();

        builder.Schema.Should().Be("https://custom.schema/v1");
    }

    [Fact]
    public void Invoke_EmptyResources_ByDefault()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("New-DscConfiguration");

        var builder = ps.Invoke<DscConfigurationBuilder>().Single();

        builder.Resources.Should().BeEmpty();
    }
}
