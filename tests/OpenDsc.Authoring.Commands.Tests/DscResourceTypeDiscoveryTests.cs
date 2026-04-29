// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Authoring.Commands.Tests;

[Trait("Category", "Unit")]
public class DscResourceTypeDiscoveryTests
{
    [Fact]
    public void ParseJsonLines_SingleResource_ParsesCorrectly()
    {
        var lines = new[]
        {
            """{"type":"OpenDsc.Windows/Service","version":"0.1.0","description":"Windows service control","capabilities":["get","set","delete"],"requireAdapter":"","schema":{"embedded":{"type":"object","properties":{"Name":{"type":"string"}}}}}""",
        };

        var results = DscResourceTypeDiscovery.ParseJsonLines(lines);

        results.Should().HaveCount(1);
        results[0].Type.Should().Be("OpenDsc.Windows/Service");
        results[0].Version.Should().Be("0.1.0");
        results[0].Description.Should().Be("Windows service control");
        results[0].Capabilities.Should().BeEquivalentTo(["get", "set", "delete"]);
    }

    [Fact]
    public void ParseJsonLines_MultipleResources_ParsesAll()
    {
        var lines = new[]
        {
            """{"type":"Type/A","version":"1.0.0","capabilities":["get"]}""",
            """{"type":"Type/B","version":"2.0.0","capabilities":["get","set"]}""",
        };

        var results = DscResourceTypeDiscovery.ParseJsonLines(lines);

        results.Should().HaveCount(2);
    }

    [Fact]
    public void ParseJsonLines_EmptyLines_Skipped()
    {
        var lines = new[]
        {
            """{"type":"Type/A","version":"1.0.0","capabilities":["get"]}""",
            "",
            "   ",
            """{"type":"Type/B","version":"2.0.0","capabilities":["get"]}""",
        };

        var results = DscResourceTypeDiscovery.ParseJsonLines(lines);

        results.Should().HaveCount(2);
    }

    [Fact]
    public void ParseJsonLines_InvalidJson_Skipped()
    {
        var lines = new[]
        {
            """{"type":"Type/A","version":"1.0.0","capabilities":["get"]}""",
            "this is not json",
            """{"type":"Type/B","version":"2.0.0","capabilities":["get"]}""",
        };

        var results = DscResourceTypeDiscovery.ParseJsonLines(lines);

        results.Should().HaveCount(2);
    }

    [Fact]
    public void ParseJsonLines_ResourceWithSchema_SchemaExtracted()
    {
        var lines = new[]
        {
            """{"type":"Test/Resource","version":"1.0.0","capabilities":["get"],"schema":{"embedded":{"type":"object","properties":{"Name":{"type":"string"}}}}}""",
        };

        var results = DscResourceTypeDiscovery.ParseJsonLines(lines);

        results[0].Schema.Should().NotBeNull();
    }

    [Fact]
    public void ParseJsonLines_ResourceWithoutSchema_SchemaIsNull()
    {
        var lines = new[]
        {
            """{"type":"Test/Resource","version":"1.0.0","capabilities":["get"]}""",
        };

        var results = DscResourceTypeDiscovery.ParseJsonLines(lines);

        results[0].Schema.Should().BeNull();
    }

    [Fact]
    public void ParseJsonLines_GeneratesTypeName()
    {
        var lines = new[]
        {
            """{"type":"OpenDsc.Windows/Service","version":"1.0.0","capabilities":["get"]}""",
        };

        var results = DscResourceTypeDiscovery.ParseJsonLines(lines);

        results[0].GeneratedTypeName.Should().Be("DSC.OpenDsc.Windows.Service");
    }

    [Fact]
    public void ParseJsonLines_AdaptedResource_ParsesRequireAdapter()
    {
        var lines = new[]
        {
            """{"type":"MyModule/MyResource","version":"1.0.0","capabilities":["get"],"requireAdapter":"Microsoft.Adapter/PowerShell"}""",
        };

        var results = DscResourceTypeDiscovery.ParseJsonLines(lines);

        results[0].RequireAdapter.Should().Be("Microsoft.Adapter/PowerShell");
    }
}
