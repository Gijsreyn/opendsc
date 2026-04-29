// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Authoring.Commands.Tests;

[Trait("Category", "Unit")]
public class DscConfigurationBuilderTests
{
    [Fact]
    public void Build_EmptyBuilder_ReturnsDocumentWithNoResources()
    {
        var builder = new DscConfigurationBuilder();

        var doc = builder.Build();

        doc.Resources.Should().BeEmpty();
    }

    [Fact]
    public void Build_DefaultSchema_HasDscV3SchemaUri()
    {
        var builder = new DscConfigurationBuilder();

        var doc = builder.Build();

        doc.Schema.Should().Be("https://aka.ms/dsc/schemas/v3/bundled/config/document.json");
    }

    [Fact]
    public void AddResource_SingleResource_AppearsInBuild()
    {
        var builder = new DscConfigurationBuilder();
        builder.AddResource("SSHD", "OpenDsc.Windows/Service", new Dictionary<string, object?>
        {
            ["name"] = "sshd",
            ["status"] = "Running",
        });

        var doc = builder.Build();

        doc.Resources.Should().HaveCount(1);
        doc.Resources[0].Name.Should().Be("SSHD");
        doc.Resources[0].Type.Should().Be("OpenDsc.Windows/Service");
    }

    [Fact]
    public void AddResource_Properties_ConvertedToJsonNodes()
    {
        var builder = new DscConfigurationBuilder();
        builder.AddResource("Test", "OpenDsc.Windows/Service", new Dictionary<string, object?>
        {
            ["name"] = "sshd",
            ["enabled"] = true,
            ["count"] = 42,
        });

        var doc = builder.Build();
        var props = doc.Resources[0].Properties;

        props["name"]!.ToString().Should().Be("sshd");
        props["enabled"]!.GetValue<bool>().Should().BeTrue();
        props["count"]!.GetValue<int>().Should().Be(42);
    }

    [Fact]
    public void AddResource_NullProperty_StoredAsNull()
    {
        var builder = new DscConfigurationBuilder();
        builder.AddResource("Test", "Test/Resource", new Dictionary<string, object?>
        {
            ["name"] = "test",
            ["optional"] = null,
        });

        var doc = builder.Build();

        doc.Resources[0].Properties["optional"].Should().BeNull();
    }

    [Fact]
    public void AddResource_MultipleResources_AllPreservedInOrder()
    {
        var builder = new DscConfigurationBuilder();
        builder.AddResource("First", "Type/A", new Dictionary<string, object?> { ["id"] = "1" });
        builder.AddResource("Second", "Type/B", new Dictionary<string, object?> { ["id"] = "2" });
        builder.AddResource("Third", "Type/C", new Dictionary<string, object?> { ["id"] = "3" });

        var doc = builder.Build();

        doc.Resources.Should().HaveCount(3);
        doc.Resources[0].Name.Should().Be("First");
        doc.Resources[1].Name.Should().Be("Second");
        doc.Resources[2].Name.Should().Be("Third");
    }

    [Fact]
    public void AddResource_DependsOnPlainName_ResolvesToResourceIdExpression()
    {
        var builder = new DscConfigurationBuilder();
        builder.AddResource("Install PS7", "WinGet/Package", new Dictionary<string, object?>
        {
            ["id"] = "Microsoft.PowerShell",
        });
        builder.AddResource("Configure SSHD", "OpenDsc.Windows/Service", new Dictionary<string, object?>
        {
            ["name"] = "sshd",
        }, dependsOn: ["Install PS7"]);

        var doc = builder.Build();

        doc.Resources[1].DependsOn.Should().HaveCount(1);
        doc.Resources[1].DependsOn![0].Should().Be("[resourceId('WinGet/Package', 'Install PS7')]");
    }

    [Fact]
    public void AddResource_DependsOnRawExpression_PassedThrough()
    {
        var builder = new DscConfigurationBuilder();
        builder.AddResource("First", "Type/A", new Dictionary<string, object?> { ["id"] = "1" });
        builder.AddResource("Second", "Type/B", new Dictionary<string, object?> { ["id"] = "2" },
            dependsOn: ["[resourceId('Type/A', 'First')]"]);

        var doc = builder.Build();

        doc.Resources[1].DependsOn![0].Should().Be("[resourceId('Type/A', 'First')]");
    }

    [Fact]
    public void AddResource_DependsOnMixed_ResolvesNamesAndPassesRawExpressions()
    {
        var builder = new DscConfigurationBuilder();
        builder.AddResource("First", "Type/A", new Dictionary<string, object?> { ["id"] = "1" });
        builder.AddResource("Second", "Type/B", new Dictionary<string, object?> { ["id"] = "2" });
        builder.AddResource("Third", "Type/C", new Dictionary<string, object?> { ["id"] = "3" },
            dependsOn: ["First", "[resourceId('Type/B', 'Second')]"]);

        var doc = builder.Build();

        doc.Resources[2].DependsOn.Should().HaveCount(2);
        doc.Resources[2].DependsOn![0].Should().Be("[resourceId('Type/A', 'First')]");
        doc.Resources[2].DependsOn![1].Should().Be("[resourceId('Type/B', 'Second')]");
    }

    [Fact]
    public void AddResource_DependsOnUnknownName_ThrowsArgumentException()
    {
        var builder = new DscConfigurationBuilder();

        var act = () => builder.AddResource("Second", "Type/B",
            new Dictionary<string, object?> { ["id"] = "2" },
            dependsOn: ["NonExistent"]);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Cannot resolve dependency 'NonExistent'*");
    }

    [Fact]
    public void AddResource_NullName_ThrowsArgumentException()
    {
        var builder = new DscConfigurationBuilder();

        var act = () => builder.AddResource(null!, "Type/A",
            new Dictionary<string, object?> { ["id"] = "1" });

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddResource_EmptyType_ThrowsArgumentException()
    {
        var builder = new DscConfigurationBuilder();

        var act = () => builder.AddResource("Test", "",
            new Dictionary<string, object?> { ["id"] = "1" });

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddResource_ArrayProperty_ConvertedToJsonArray()
    {
        var builder = new DscConfigurationBuilder();
        builder.AddResource("Test", "Type/A", new Dictionary<string, object?>
        {
            ["tags"] = new[] { "web", "production" },
        });

        var doc = builder.Build();
        var tags = doc.Resources[0].Properties["tags"];

        tags.Should().NotBeNull();
        tags!.AsArray().Should().HaveCount(2);
    }

    [Fact]
    public void AddResource_NestedDictionary_ConvertedToJsonObject()
    {
        var builder = new DscConfigurationBuilder();
        builder.AddResource("Test", "Type/A", new Dictionary<string, object?>
        {
            ["settings"] = new Dictionary<string, object?>
            {
                ["key"] = "value",
                ["count"] = 5,
            },
        });

        var doc = builder.Build();
        var settings = doc.Resources[0].Properties["settings"];

        settings.Should().NotBeNull();
        settings!["key"]!.ToString().Should().Be("value");
        settings!["count"]!.GetValue<int>().Should().Be(5);
    }

    [Fact]
    public void AddResource_NoDependsOn_DependsOnIsNull()
    {
        var builder = new DscConfigurationBuilder();
        builder.AddResource("Test", "Type/A", new Dictionary<string, object?> { ["id"] = "1" });

        var doc = builder.Build();

        doc.Resources[0].DependsOn.Should().BeNull();
    }

    [Fact]
    public void Schema_CustomValue_AppliedToBuild()
    {
        var builder = new DscConfigurationBuilder
        {
            Schema = "https://custom.schema/v1"
        };

        var doc = builder.Build();

        doc.Schema.Should().Be("https://custom.schema/v1");
    }
}
