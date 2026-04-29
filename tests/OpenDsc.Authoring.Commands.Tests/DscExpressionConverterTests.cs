// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Authoring.Commands.Tests;

[Trait("Category", "Unit")]
public class DscExpressionConverterTests
{
    [Fact]
    public void RawConverter_Convert_ReturnsInputUnchanged()
    {
        var converter = new RawDscExpressionConverter();

        var result = converter.Convert("[concat(systemRoot(), '\\path')]");

        result.Should().Be("[concat(systemRoot(), '\\path')]");
    }

    [Fact]
    public void RawConverter_Validate_ValidExpression_ReturnsTrue()
    {
        var converter = new RawDscExpressionConverter();

        var result = converter.Validate("[resourceId('Type/Name', 'instance')]", out var error);

        result.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void RawConverter_Validate_EmptyString_ReturnsFalse()
    {
        var converter = new RawDscExpressionConverter();

        var result = converter.Validate("", out var error);

        result.Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void RawConverter_Validate_MissingBrackets_ReturnsFalse()
    {
        var converter = new RawDscExpressionConverter();

        var result = converter.Validate("concat(a, b)", out var error);

        result.Should().BeFalse();
        error.Should().Contain("square brackets");
    }

    [Fact]
    public void RawConverter_Validate_OnlyOpeningBracket_ReturnsFalse()
    {
        var converter = new RawDscExpressionConverter();

        var result = converter.Validate("[concat(a, b)", out var error);

        result.Should().BeFalse();
    }
}
