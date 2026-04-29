// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Authoring.Commands.Tests;

[Trait("Category", "Unit")]
public class DscDiscoveredResourceTests
{
    [Theory]
    [InlineData("OpenDsc.Windows/Service", "DSC.OpenDsc.Windows.Service")]
    [InlineData("Microsoft.WinGet.DSC/WinGetPackage", "DSC.Microsoft.WinGet.DSC.WinGetPackage")]
    [InlineData("OpenDsc.FileSystem/File", "DSC.OpenDsc.FileSystem.File")]
    [InlineData("Microsoft.DSC/Debug.Echo", "DSC.Microsoft.DSC.Debug.Echo")]
    [InlineData("Simple/Resource", "DSC.Simple.Resource")]
    public void ToTypeName_ConvertsCorrectly(string dscType, string expected)
    {
        var result = DscDiscoveredResource.ToTypeName(dscType);

        result.Should().Be(expected);
    }
}
