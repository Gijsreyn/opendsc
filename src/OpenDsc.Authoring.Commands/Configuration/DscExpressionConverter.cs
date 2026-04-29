// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Authoring.Commands;

/// <summary>
/// An extensible interface for converting DSC expression representations between
/// PowerShell constructs and DSC v3 expression strings. Implementations can be
/// plugged in to support scriptblock-to-expression transpilation.
/// </summary>
/// <example>
/// <code>
/// // Raw string passthrough (default behavior)
/// IExpressionConverter converter = new RawExpressionConverter();
/// string result = converter.Convert("[concat(systemRoot(), '\\path')]");
/// // Returns: "[concat(systemRoot(), '\\path')]"
/// </code>
/// </example>
public interface IDscExpressionConverter
{
    /// <summary>
    /// Converts an expression input to a DSC v3 expression string.
    /// </summary>
    /// <param name="input">The expression input (raw string or transpilable representation).</param>
    /// <returns>A valid DSC v3 expression string.</returns>
    string Convert(string input);

    /// <summary>
    /// Validates whether the input is a valid DSC expression.
    /// </summary>
    /// <param name="input">The expression string to validate.</param>
    /// <param name="error">An error message if validation fails.</param>
    /// <returns><c>true</c> if valid; otherwise, <c>false</c>.</returns>
    bool Validate(string input, out string? error);
}

/// <summary>
/// Default expression converter that passes raw DSC expression strings through unchanged.
/// This is the initial implementation; future versions can add scriptblock transpilation
/// by implementing <see cref="IDscExpressionConverter"/>.
/// </summary>
public sealed class RawDscExpressionConverter : IDscExpressionConverter
{
    /// <summary>
    /// Returns the input string unchanged.
    /// </summary>
    /// <param name="input">A raw DSC expression string.</param>
    /// <returns>The input string as-is.</returns>
    public string Convert(string input) => input;

    /// <summary>
    /// Validates that the expression starts with <c>[</c> and ends with <c>]</c>.
    /// </summary>
    /// <param name="input">The expression string to validate.</param>
    /// <param name="error">An error message if validation fails.</param>
    /// <returns><c>true</c> if the expression has valid brackets; otherwise, <c>false</c>.</returns>
    public bool Validate(string input, out string? error)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            error = "Expression cannot be null or empty.";

            return false;
        }

        if (!input.StartsWith('[') || !input.EndsWith(']'))
        {
            error = "DSC expressions must be enclosed in square brackets, e.g. \"[concat(...)]\".";

            return false;
        }

        error = null;

        return true;
    }
}
