using System;
using System.Text.RegularExpressions;

namespace AWS.Lambda.Powertools.Tracing.Internal;

/// <summary>
///    Helper class
/// </summary>
public static class Helpers
{
    /// <summary>
    /// Sanitize a string by removing any characters that are not alphanumeric, whitespace, or one of the following: _ . : / % & # = + - @
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static string SanitizeString(string input)
    {
        // Define a regular expression pattern to match allowed characters
        var pattern = @"[^a-zA-Z0-9\s_\.\:/%&#=+\-@]";

        // Replace any character that does not match the pattern with an empty string, with a timeout
        return Regex.Replace(input, pattern, string.Empty, RegexOptions.None, TimeSpan.FromMilliseconds(100));
    }
}