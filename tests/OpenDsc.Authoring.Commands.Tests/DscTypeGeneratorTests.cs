// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Authoring.Commands.Tests;

[Trait("Category", "Unit")]
public class DscTypeGeneratorTests
{
    private static DscDiscoveredResource CreateResourceWithSchema(string type, string schemaJson)
    {
        using var doc = JsonDocument.Parse(schemaJson);

        return new DscDiscoveredResource
        {
            Type = type,
            GeneratedTypeName = DscDiscoveredResource.ToTypeName(type),
            Schema = doc.RootElement.Clone(),
        };
    }

    [Fact]
    public void GenerateSource_StringProperty_GeneratesStringProperty()
    {
        var resource = CreateResourceWithSchema("Test/Resource", """
        {
            "type": "object",
            "properties": {
                "Name": { "type": "string" }
            }
        }
        """);

        var source = DscTypeGenerator.GenerateSource([resource]);

        source.Should().Contain("public string Name { get; set; }");
    }

    [Fact]
    public void GenerateSource_IntegerProperty_GeneratesNullableInt()
    {
        var resource = CreateResourceWithSchema("Test/Resource", """
        {
            "type": "object",
            "properties": {
                "Count": { "type": "integer" }
            }
        }
        """);

        var source = DscTypeGenerator.GenerateSource([resource]);

        source.Should().Contain("public int? Count { get; set; }");
    }

    [Fact]
    public void GenerateSource_BooleanProperty_GeneratesNullableBool()
    {
        var resource = CreateResourceWithSchema("Test/Resource", """
        {
            "type": "object",
            "properties": {
                "Enabled": { "type": "boolean" }
            }
        }
        """);

        var source = DscTypeGenerator.GenerateSource([resource]);

        source.Should().Contain("public bool? Enabled { get; set; }");
    }

    [Fact]
    public void GenerateSource_EnumProperty_GeneratesEnumType()
    {
        var resource = CreateResourceWithSchema("Test/Resource", """
        {
            "type": "object",
            "properties": {
                "Status": { "type": "string", "enum": ["Running", "Stopped"] }
            }
        }
        """);

        var source = DscTypeGenerator.GenerateSource([resource]);

        source.Should().Contain("public enum StatusOption");
        source.Should().Contain("Running,");
        source.Should().Contain("Stopped,");
        source.Should().Contain("public StatusOption? Status { get; set; }");
    }

    [Fact]
    public void GenerateSource_ArrayProperty_GeneratesArrayType()
    {
        var resource = CreateResourceWithSchema("Test/Resource", """
        {
            "type": "object",
            "properties": {
                "Tags": { "type": "array", "items": { "type": "string" } }
            }
        }
        """);

        var source = DscTypeGenerator.GenerateSource([resource]);

        source.Should().Contain("public string[] Tags { get; set; }");
    }

    [Fact]
    public void GenerateSource_ObjectProperty_GeneratesDictionary()
    {
        var resource = CreateResourceWithSchema("Test/Resource", """
        {
            "type": "object",
            "properties": {
                "Settings": { "type": "object" }
            }
        }
        """);

        var source = DscTypeGenerator.GenerateSource([resource]);

        source.Should().Contain("public Dictionary<string, object> Settings { get; set; }");
    }

    [Fact]
    public void GenerateSource_DateTimeProperty_GeneratesNullableDateTime()
    {
        var resource = CreateResourceWithSchema("Test/Resource", """
        {
            "type": "object",
            "properties": {
                "StartTime": { "type": "string", "format": "date-time" }
            }
        }
        """);

        var source = DscTypeGenerator.GenerateSource([resource]);

        source.Should().Contain("public DateTime? StartTime { get; set; }");
    }

    [Fact]
    public void GenerateSource_CorrectNamespace_IncludesDscPrefix()
    {
        var resource = CreateResourceWithSchema("OpenDsc.Windows/Service", """
        {
            "type": "object",
            "properties": {
                "Name": { "type": "string" }
            }
        }
        """);

        var source = DscTypeGenerator.GenerateSource([resource]);

        source.Should().Contain("namespace DSC.OpenDsc.Windows");
        source.Should().Contain("public class Service");
    }

    [Fact]
    public void GenerateSource_MultipleResources_GeneratesAllTypes()
    {
        var resource1 = CreateResourceWithSchema("OpenDsc.Windows/Service", """
        {
            "type": "object",
            "properties": { "Name": { "type": "string" } }
        }
        """);

        var resource2 = CreateResourceWithSchema("OpenDsc.FileSystem/File", """
        {
            "type": "object",
            "properties": { "Path": { "type": "string" } }
        }
        """);

        var source = DscTypeGenerator.GenerateSource([resource1, resource2]);

        source.Should().Contain("namespace DSC.OpenDsc.Windows");
        source.Should().Contain("class Service");
        source.Should().Contain("namespace DSC.OpenDsc.FileSystem");
        source.Should().Contain("class File");
    }

    [Fact]
    public void GenerateSource_ResourceWithNoSchema_Skipped()
    {
        var resource = new DscDiscoveredResource
        {
            Type = "Test/NoSchema",
            GeneratedTypeName = "DSC.Test.NoSchema",
            Schema = null,
        };

        var source = DscTypeGenerator.GenerateSource([resource]);

        source.Should().NotContain("class NoSchema");
    }

    [Fact]
    public void GenerateSingleSource_NullSchema_ReturnsNull()
    {
        var resource = new DscDiscoveredResource
        {
            Type = "Test/NoSchema",
            GeneratedTypeName = "DSC.Test.NoSchema",
            Schema = null,
        };

        var result = DscTypeGenerator.GenerateSingleSource(resource);

        result.Should().BeNull();
    }

    [Fact]
    public void GenerateSource_NumberProperty_GeneratesNullableDouble()
    {
        var resource = CreateResourceWithSchema("Test/Resource", """
        {
            "type": "object",
            "properties": {
                "Ratio": { "type": "number" }
            }
        }
        """);

        var source = DscTypeGenerator.GenerateSource([resource]);

        source.Should().Contain("public double? Ratio { get; set; }");
    }
}
