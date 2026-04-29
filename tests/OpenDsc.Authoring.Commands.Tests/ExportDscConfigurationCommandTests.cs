// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;
using System.Management.Automation.Runspaces;

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Authoring.Commands.Tests;

[Trait("Category", "Unit")]
public class ExportDscConfigurationCommandTests : IDisposable
{
    private readonly string _tempDir;

    public ExportDscConfigurationCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"opendsc-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    private static Runspace CreateRunspace()
    {
        var iss = InitialSessionState.CreateDefault2();
        iss.Commands.Add(new SessionStateCmdletEntry(
            "Export-DscConfiguration",
            typeof(ExportDscConfigurationCommand), null));

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
    public void Invoke_YamlExtension_WritesYamlFile()
    {
        var builder = CreateSampleBuilder();
        var path = Path.Combine(_tempDir, "config.dsc.yaml");

        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("Export-DscConfiguration")
            .AddParameter("Configuration", builder)
            .AddParameter("Path", path);

        ps.Invoke();
        ps.HadErrors.Should().BeFalse();

        File.Exists(path).Should().BeTrue();
        var content = File.ReadAllText(path);
        content.Should().Contain("resources:");
        content.Should().Contain("name: sshd");
    }

    [Fact]
    public void Invoke_JsonExtension_WritesJsonFile()
    {
        var builder = CreateSampleBuilder();
        var path = Path.Combine(_tempDir, "config.dsc.json");

        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("Export-DscConfiguration")
            .AddParameter("Configuration", builder)
            .AddParameter("Path", path);

        ps.Invoke();
        ps.HadErrors.Should().BeFalse();

        File.Exists(path).Should().BeTrue();
        var content = File.ReadAllText(path);
        var act = () => System.Text.Json.JsonDocument.Parse(content);
        act.Should().NotThrow();
    }

    [Fact]
    public void Invoke_ExplicitFormatOverridesExtension()
    {
        var builder = CreateSampleBuilder();
        var path = Path.Combine(_tempDir, "config.dsc.yaml");

        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("Export-DscConfiguration")
            .AddParameter("Configuration", builder)
            .AddParameter("Path", path)
            .AddParameter("Format", DscConfigurationFormat.Json);

        ps.Invoke();
        ps.HadErrors.Should().BeFalse();

        var content = File.ReadAllText(path);
        var act = () => System.Text.Json.JsonDocument.Parse(content);
        act.Should().NotThrow();
    }

    [Fact]
    public void Invoke_YmlExtension_TreatedAsYaml()
    {
        var builder = CreateSampleBuilder();
        var path = Path.Combine(_tempDir, "config.dsc.yml");

        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("Export-DscConfiguration")
            .AddParameter("Configuration", builder)
            .AddParameter("Path", path);

        ps.Invoke();
        ps.HadErrors.Should().BeFalse();

        var content = File.ReadAllText(path);
        content.Should().Contain("resources:");
    }

    [Fact]
    public void Invoke_UnknownExtension_DefaultsToYaml()
    {
        var builder = CreateSampleBuilder();
        var path = Path.Combine(_tempDir, "config.dsc.txt");

        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("Export-DscConfiguration")
            .AddParameter("Configuration", builder)
            .AddParameter("Path", path);

        ps.Invoke();
        ps.HadErrors.Should().BeFalse();

        var content = File.ReadAllText(path);
        content.Should().Contain("resources:");
    }

    [Fact]
    public void Invoke_CreatesParentDirectory()
    {
        var builder = CreateSampleBuilder();
        var subDir = Path.Combine(_tempDir, "subdir", "nested");
        var path = Path.Combine(subDir, "config.dsc.yaml");

        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("Export-DscConfiguration")
            .AddParameter("Configuration", builder)
            .AddParameter("Path", path);

        ps.Invoke();
        ps.HadErrors.Should().BeFalse();

        File.Exists(path).Should().BeTrue();
    }
}
