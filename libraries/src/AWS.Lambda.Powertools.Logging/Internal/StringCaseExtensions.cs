using System;
using System.Linq;
using System.Text;

namespace AWS.Lambda.Powertools.Logging.Internal;

/// <summary>
/// Extension methods for string case conversion.
/// </summary>
internal static class StringCaseExtensions
{
    /// <summary>
    /// Converts a string to camelCase.
    /// </summary>
    /// <param name="value">The string to convert.</param>
    /// <returns>A camelCase formatted string.</returns>
    public static string ToCamel(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        // Convert to PascalCase first to handle potential snake_case or kebab-case
        string pascalCase = ToPascal(value);
            
        // Convert first char to lowercase
        return char.ToLowerInvariant(pascalCase[0]) + pascalCase.Substring(1);
    }

    /// <summary>
    /// Converts a string to PascalCase.
    /// </summary>
    /// <param name="input">The string to convert.</param>
    /// <returns>A PascalCase formatted string.</returns>
    public static string ToPascal(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var words = input.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
        var result = new StringBuilder();

        foreach (var word in words)
        {
            if (word.Length > 0)
            {
                // Capitalize the first character of each word
                result.Append(char.ToUpperInvariant(word[0]));

                // Handle the rest of the characters
                if (word.Length > 1)
                {
                    // If the word is all uppercase, convert the rest to lowercase
                    if (word.All(char.IsUpper))
                    {
                        result.Append(word.Substring(1).ToLowerInvariant());
                    }
                    else
                    {
                        // Otherwise, keep the original casing
                        result.Append(word.Substring(1));
                    }
                }
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Converts a string to snake_case.
    /// </summary>
    /// <param name="input">The string to convert.</param>
    /// <returns>A snake_case formatted string.</returns>
    public static string ToSnake(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = new StringBuilder(input.Length + 10);
        bool lastCharWasUnderscore = false;
        bool lastCharWasUpper = false;

        for (int i = 0; i < input.Length; i++)
        {
            char currentChar = input[i];

            if (currentChar == '_')
            {
                result.Append('_');
                lastCharWasUnderscore = true;
                lastCharWasUpper = false;
            }
            else if (char.IsUpper(currentChar))
            {
                if (i > 0 && !lastCharWasUnderscore &&
                    (!lastCharWasUpper || (i + 1 < input.Length && char.IsLower(input[i + 1]))))
                {
                    result.Append('_');
                }

                result.Append(char.ToLowerInvariant(currentChar));
                lastCharWasUnderscore = false;
                lastCharWasUpper = true;
            }
            else
            {
                result.Append(char.ToLowerInvariant(currentChar));
                lastCharWasUnderscore = false;
                lastCharWasUpper = false;
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Converts a string to the specified case format.
    /// </summary>
    /// <param name="value">The string to convert.</param>
    /// <param name="outputCase">The target case format.</param>
    /// <returns>A formatted string in the specified case.</returns>
    public static string ToCase(this string value, LoggerOutputCase outputCase)
    {
        return outputCase switch
        {
            LoggerOutputCase.CamelCase => value.ToCamel(),
            LoggerOutputCase.PascalCase => value.ToPascal(),
            LoggerOutputCase.SnakeCase => value.ToSnake(),
            _ => value.ToSnake() // Default/unchanged
        };
    }
}