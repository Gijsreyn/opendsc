// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Authoring.Commands.Tests;

[Trait("Category", "Unit")]
public class AddDscResourceInstanceCommandTests
{
    private static Runspace CreateRunspace()
    {
        var iss = InitialSessionState.CreateDefault2();
        iss.Commands.Add(new SessionStateCmdletEntry(
            "Add-DscResourceInstance",
            typeof(AddDscResourceInstanceCommand), null));

        var runspace = RunspaceFactory.CreateRunspace(iss);
        runspace.Open();

        return runspace;
    }

    [Fact]
    public void Invoke_WithTypeAndProperties_AddsResource()
    {
        var builder = new DscConfigurationBuilder();

        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("Add-DscResourceInstance")
            .AddParameter("Configuration", builder)
            .AddParameter("Name", "SSHD")
            .AddParameter("Type", "OpenDsc.Windows/Service")
            .AddParameter("Properties", new Hashtable { ["name"] = "sshd", ["status"] = "Running" })
            .AddParameter("PassThru", true);

        var results = ps.Invoke<DscConfigurationBuilder>();

        results.Should().HaveCount(1);
        builder.Resources.Should().HaveCount(1);
        builder.Resources[0].Name.Should().Be("SSHD");
        builder.Resources[0].Type.Should().Be("OpenDsc.Windows/Service");
    }

    [Fact]
    public void Invoke_WithDscResourceInstanceInfo_AddsResource()
    {
        var builder = new DscConfigurationBuilder();
        var instance = new DscResourceInstanceInfo
        {
            Type = "OpenDsc.Windows/Service",
            Properties = new Dictionary<string, object?>
            {
                ["name"] = "sshd",
                ["status"] = "Running",
            },
        };

        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("Add-DscResourceInstance")
            .AddParameter("Configuration", builder)
            .AddParameter("Name", "SSHD")
            .AddParameter("Instance", instance);

        ps.Invoke();

        builder.Resources.Should().HaveCount(1);
    }

    [Fact]
    public void Invoke_WithDependsOn_ResolvesNames()
    {
        var builder = new DscConfigurationBuilder();
        builder.AddResource("Install PS7", "WinGet/Package",
            new Dictionary<string, object?> { ["id"] = "Microsoft.PowerShell" });

        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("Add-DscResourceInstance")
            .AddParameter("Configuration", builder)
            .AddParameter("Name", "Configure SSHD")
            .AddParameter("Type", "OpenDsc.Windows/Service")
            .AddParameter("Properties", new Hashtable { ["name"] = "sshd" })
            .AddParameter("DependsOn", new[] { "Install PS7" });

        ps.Invoke();

        builder.Resources.Should().HaveCount(2);
        builder.Resources[1].DependsOn.Should().HaveCount(1);
        builder.Resources[1].DependsOn![0].Should().Contain("WinGet/Package");
    }

    [Fact]
    public void Invoke_WithPassThru_ReturnsBuilder()
    {
        var builder = new DscConfigurationBuilder();

        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("Add-DscResourceInstance")
            .AddParameter("Configuration", builder)
            .AddParameter("Name", "SSHD")
            .AddParameter("Type", "OpenDsc.Windows/Service")
            .AddParameter("Properties", new Hashtable { ["name"] = "sshd" })
            .AddParameter("PassThru", true);

        var results = ps.Invoke<DscConfigurationBuilder>();

        results.Should().HaveCount(1);
        results[0].Should().BeSameAs(builder);
    }

    [Fact]
    public void Invoke_WithoutPassThru_NoOutput()
    {
        var builder = new DscConfigurationBuilder();

        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("Add-DscResourceInstance")
            .AddParameter("Configuration", builder)
            .AddParameter("Name", "SSHD")
            .AddParameter("Type", "OpenDsc.Windows/Service")
            .AddParameter("Properties", new Hashtable { ["name"] = "sshd" });

        var results = ps.Invoke();

        results.Should().BeEmpty();
    }

    [Fact]
    public void Invoke_MissingTypeAndProperties_WritesError()
    {
        var builder = new DscConfigurationBuilder();

        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("Add-DscResourceInstance")
            .AddParameter("Configuration", builder)
            .AddParameter("Name", "SSHD");

        ps.Invoke();

        ps.Streams.Error.Should().HaveCountGreaterThan(0);
    }
}
