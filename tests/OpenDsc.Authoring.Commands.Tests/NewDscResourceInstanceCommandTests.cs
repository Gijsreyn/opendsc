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
public class NewDscResourceInstanceCommandTests
{
    private static Runspace CreateRunspace()
    {
        var iss = InitialSessionState.CreateDefault2();
        iss.Commands.Add(new SessionStateCmdletEntry(
            "New-DscResourceInstance",
            typeof(NewDscResourceInstanceCommand), null));

        var runspace = RunspaceFactory.CreateRunspace(iss);
        runspace.Open();

        return runspace;
    }

    [Fact]
    public void Invoke_WithProperties_CreatesInstanceInfo()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("New-DscResourceInstance")
            .AddParameter("Type", "OpenDsc.Windows/Service")
            .AddParameter("Properties", new Hashtable { ["name"] = "sshd" });

        var results = ps.Invoke<DscResourceInstanceInfo>();

        results.Should().HaveCount(1);
        results[0].Type.Should().Be("OpenDsc.Windows/Service");
        results[0].Properties["name"].Should().Be("sshd");
    }

    [Fact]
    public void Invoke_MissingBothInstanceAndProperties_WritesError()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("New-DscResourceInstance")
            .AddParameter("Type", "OpenDsc.Windows/Service");

        var results = ps.Invoke<DscResourceInstanceInfo>();

        results.Should().BeEmpty();
        ps.Streams.Error.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void Invoke_WithHashtableProperties_MultipleKeys()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("New-DscResourceInstance")
            .AddParameter("Type", "OpenDsc.Windows/Service")
            .AddParameter("Properties", new Hashtable
            {
                ["name"] = "sshd",
                ["status"] = "Running",
                ["enabled"] = true,
            });

        var result = ps.Invoke<DscResourceInstanceInfo>().Single();

        result.Properties.Should().HaveCount(3);
        result.Properties["name"].Should().Be("sshd");
        result.Properties["status"].Should().Be("Running");
        result.Properties["enabled"].Should().Be(true);
    }
}
