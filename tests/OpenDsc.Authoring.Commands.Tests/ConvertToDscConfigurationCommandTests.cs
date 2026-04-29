// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text.Json;

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Authoring.Commands.Tests;

[Trait("Category", "Unit")]
public class ConvertToDscConfigurationCommandTests
{
    private static Runspace CreateRunspace()
    {
        var iss = InitialSessionState.CreateDefault2();
        iss.Commands.Add(new SessionStateCmdletEntry(
            "ConvertTo-DscConfiguration",
            typeof(ConvertToDscConfigurationCommand), null));

        var runspace = RunspaceFactory.CreateRunspace(iss);
        runspace.Open();

        return runspace;
    }

    private static DscConfigurationBuilder CreateSampleBuilder()
    {
        var builder = new DscConfigurationBuilder();
        builder.AddResource("SSHD", "OpenDsc.Windows/Service", new Dictionary<string, object?>
        {
            ["name"] = "sshd",
            ["status"] = "Running",
        });

        return builder;
    }

    [Fact]
    public void Invoke_DefaultFormat_ReturnsYaml()
    {
        var builder = CreateSampleBuilder();

        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("ConvertTo-DscConfiguration")
            .AddParameter("Configuration", builder);

        var results = ps.Invoke<string>();

        results.Should().HaveCount(1);
        var yaml = results[0];
        yaml.Should().Contain("resources:");
        yaml.Should().Contain("name: sshd");
        yaml.Should().Contain("type: OpenDsc.Windows/Service");
    }

    [Fact]
    public void Invoke_JsonFormat_ReturnsValidJson()
    {
        var builder = CreateSampleBuilder();

        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("ConvertTo-DscConfiguration")
            .AddParameter("Configuration", builder)
            .AddParameter("Format", DscConfigurationFormat.Json);

        var results = ps.Invoke<string>();

        results.Should().HaveCount(1);
        var json = results[0];

        var act = () => JsonDocument.Parse(json);
        act.Should().NotThrow();
    }

    [Fact]
    public void Invoke_JsonFormat_ContainsSchemaAndResources()
    {
        var builder = CreateSampleBuilder();

        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("ConvertTo-DscConfiguration")
            .AddParameter("Configuration", builder)
            .AddParameter("Format", DscConfigurationFormat.Json);

        var json = ps.Invoke<string>().Single();
        using var doc = JsonDocument.Parse(json);

        doc.RootElement.GetProperty("$schema").GetString()
            .Should().Be("https://aka.ms/dsc/schemas/v3/bundled/config/document.json");
        doc.RootElement.GetProperty("resources").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public void Invoke_YamlFormat_ContainsSchemaKey()
    {
        var builder = CreateSampleBuilder();

        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("ConvertTo-DscConfiguration")
            .AddParameter("Configuration", builder)
            .AddParameter("Format", DscConfigurationFormat.Yaml);

        var yaml = ps.Invoke<string>().Single();

        yaml.Should().Contain("$schema:");
    }

    [Fact]
    public void Invoke_MultipleResources_AllSerialized()
    {
        var builder = new DscConfigurationBuilder();
        builder.AddResource("First", "Type/A", new Dictionary<string, object?> { ["id"] = "1" });
        builder.AddResource("Second", "Type/B", new Dictionary<string, object?> { ["id"] = "2" });

        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("ConvertTo-DscConfiguration")
            .AddParameter("Configuration", builder)
            .AddParameter("Format", DscConfigurationFormat.Json);

        var json = ps.Invoke<string>().Single();
        using var doc = JsonDocument.Parse(json);

        doc.RootElement.GetProperty("resources").GetArrayLength().Should().Be(2);
    }

    [Fact]
    public void Invoke_WithDependsOn_SerializedInOutput()
    {
        var builder = new DscConfigurationBuilder();
        builder.AddResource("First", "Type/A", new Dictionary<string, object?> { ["id"] = "1" });
        builder.AddResource("Second", "Type/B", new Dictionary<string, object?> { ["id"] = "2" },
            dependsOn: ["First"]);

        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("ConvertTo-DscConfiguration")
            .AddParameter("Configuration", builder)
            .AddParameter("Format", DscConfigurationFormat.Json);

        var json = ps.Invoke<string>().Single();
        using var doc = JsonDocument.Parse(json);
        var secondResource = doc.RootElement.GetProperty("resources")[1];

        secondResource.TryGetProperty("dependsOn", out var deps).Should().BeTrue();
        deps.GetArrayLength().Should().Be(1);
    }

    [Fact]
    public void Invoke_NoDependsOn_OmittedFromOutput()
    {
        var builder = new DscConfigurationBuilder();
        builder.AddResource("First", "Type/A", new Dictionary<string, object?> { ["id"] = "1" });

        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("ConvertTo-DscConfiguration")
            .AddParameter("Configuration", builder)
            .AddParameter("Format", DscConfigurationFormat.Json);

        var json = ps.Invoke<string>().Single();
        using var doc = JsonDocument.Parse(json);
        var firstResource = doc.RootElement.GetProperty("resources")[0];

        firstResource.TryGetProperty("dependsOn", out _).Should().BeFalse();
    }
}
