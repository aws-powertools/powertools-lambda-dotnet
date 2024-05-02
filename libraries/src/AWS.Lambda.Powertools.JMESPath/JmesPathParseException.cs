/*
  * Copyright JsonCons.Net authors. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 *
 *  http://aws.amazon.com/apache2.0
 *
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */

using System;

namespace AWS.Lambda.Powertools.JMESPath;

/// <summary>
/// Defines a custom exception object that is thrown when JMESPath parsing fails.
/// </summary>    

public sealed class JmesPathParseException : Exception
{
    /// <summary>
    /// The line in the JMESPath string where a parse error was detected.
    /// </summary>
    private int LineNumber {get;}

    /// <summary>
    /// The column in the JMESPath string where a parse error was detected.
    /// </summary>
    private int ColumnNumber {get;}

    internal JmesPathParseException(string message, int line, int column)
        : base(message)
    {
        LineNumber = line;
        ColumnNumber = column;
    }

    /// <summary>
    /// Returns an error message that describes the current exception.
    /// </summary>
    /// <returns>A string representation of the current exception.</returns>
    public override string ToString ()
    {
        return $"{Message} at line {LineNumber} and column {ColumnNumber}";
    }
}