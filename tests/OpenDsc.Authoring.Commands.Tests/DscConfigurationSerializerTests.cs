// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Authoring.Commands.Tests;

[Trait("Category", "Unit")]
public class DscConfigurationSerializerTests
{
    private static DscConfigurationBuilder CreateBuilder()
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
    public void ToYaml_ContainsSchemaKey()
    {
        var doc = CreateBuilder().Build();
        var yaml = DscConfigurationSerializer.ToYaml(doc);

        yaml.Should().Contain("$schema:");
    }

    [Fact]
    public void ToYaml_ContainsResources()
    {
        var doc = CreateBuilder().Build();
        var yaml = DscConfigurationSerializer.ToYaml(doc);

        yaml.Should().Contain("resources:");
        yaml.Should().Contain("name: SSHD");
        yaml.Should().Contain("type: OpenDsc.Windows/Service");
    }

    [Fact]
    public void ToYaml_ContainsProperties()
    {
        var doc = CreateBuilder().Build();
        var yaml = DscConfigurationSerializer.ToYaml(doc);

        yaml.Should().Contain("name: sshd");
        yaml.Should().Contain("status: Running");
    }

    [Fact]
    public void ToJson_ReturnsValidJson()
    {
        var doc = CreateBuilder().Build();
        var json = DscConfigurationSerializer.ToJson(doc);

        var act = () => System.Text.Json.JsonDocument.Parse(json);
        act.Should().NotThrow();
    }

    [Fact]
    public void ToJson_ContainsSchemaProperty()
    {
        var doc = CreateBuilder().Build();
        var json = DscConfigurationSerializer.ToJson(doc);

        using var jdoc = System.Text.Json.JsonDocument.Parse(json);
        jdoc.RootElement.GetProperty("$schema").GetString()
            .Should().Be("https://aka.ms/dsc/schemas/v3/bundled/config/document.json");
    }

    [Fact]
    public void ToJson_ContainsResources()
    {
        var doc = CreateBuilder().Build();
        var json = DscConfigurationSerializer.ToJson(doc);

        using var jdoc = System.Text.Json.JsonDocument.Parse(json);
        jdoc.RootElement.GetProperty("resources").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public void ToYaml_DependsOn_IncludedWhenPresent()
    {
        var builder = new DscConfigurationBuilder();
        builder.AddResource("A", "Type/A", new Dictionary<string, object?> { ["id"] = "1" });
        builder.AddResource("B", "Type/B", new Dictionary<string, object?> { ["id"] = "2" },
            dependsOn: ["A"]);

        var doc = builder.Build();
        var yaml = DscConfigurationSerializer.ToYaml(doc);

        yaml.Should().Contain("dependsOn:");
    }

    [Fact]
    public void ToJson_DependsOn_IncludedWhenPresent()
    {
        var builder = new DscConfigurationBuilder();
        builder.AddResource("A", "Type/A", new Dictionary<string, object?> { ["id"] = "1" });
        builder.AddResource("B", "Type/B", new Dictionary<string, object?> { ["id"] = "2" },
            dependsOn: ["A"]);

        var doc = builder.Build();
        var json = DscConfigurationSerializer.ToJson(doc);

        using var jdoc = System.Text.Json.JsonDocument.Parse(json);
        var second = jdoc.RootElement.GetProperty("resources")[1];
        second.TryGetProperty("dependsOn", out var deps).Should().BeTrue();
        deps.GetArrayLength().Should().Be(1);
    }

    [Fact]
    public void ToYaml_BooleanProperty_SerializedCorrectly()
    {
        var builder = new DscConfigurationBuilder();
        builder.AddResource("Test", "Type/A", new Dictionary<string, object?>
        {
            ["enabled"] = true,
        });

        var doc = builder.Build();
        var yaml = DscConfigurationSerializer.ToYaml(doc);

        yaml.Should().Contain("enabled: true");
    }

    [Fact]
    public void ToYaml_IntegerProperty_SerializedCorrectly()
    {
        var builder = new DscConfigurationBuilder();
        builder.AddResource("Test", "Type/A", new Dictionary<string, object?>
        {
            ["port"] = 8080,
        });

        var doc = builder.Build();
        var yaml = DscConfigurationSerializer.ToYaml(doc);

        yaml.Should().Contain("port: 8080");
    }

    [Fact]
    public void ToJson_MultipleResources_AllPresent()
    {
        var builder = new DscConfigurationBuilder();
        builder.AddResource("First", "Type/A", new Dictionary<string, object?> { ["id"] = "1" });
        builder.AddResource("Second", "Type/B", new Dictionary<string, object?> { ["id"] = "2" });
        builder.AddResource("Third", "Type/C", new Dictionary<string, object?> { ["id"] = "3" });

        var doc = builder.Build();
        var json = DscConfigurationSerializer.ToJson(doc);

        using var jdoc = System.Text.Json.JsonDocument.Parse(json);
        jdoc.RootElement.GetProperty("resources").GetArrayLength().Should().Be(3);
    }
}
