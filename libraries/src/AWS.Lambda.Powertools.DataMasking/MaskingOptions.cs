/*
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
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

using System.Text.RegularExpressions;

namespace AWS.Lambda.Powertools.DataMasking;

/// <summary>
/// Controls how values are masked by <see cref="DataMasking.Erase(object,string[],MaskingOptions)"/>.
/// </summary>
/// <remarks>
/// By default a fixed <c>*****</c> string replaces the entire value. The options below allow
/// customizing the masking behavior while keeping the operation irreversible.
/// </remarks>
public sealed class MaskingOptions
{
    /// <summary>
    /// The default masking value applied when no custom rule is configured.
    /// </summary>
    public const string DefaultMask = "*****";

    /// <summary>
    /// The fixed string used to replace a value.
    /// Ignored when <see cref="PreserveLength"/> is <c>true</c> or when <see cref="Pattern"/> is set.
    /// Defaults to <see cref="DefaultMask"/>.
    /// </summary>
    public string MaskValue { get; set; } = DefaultMask;

    /// <summary>
    /// When <c>true</c>, the value is replaced by a mask that preserves the original character length
    /// (for example <c>"secret"</c> becomes <c>"******"</c>), using <see cref="MaskCharacter"/>.
    /// Ignored when <see cref="Pattern"/> is set.
    /// </summary>
    public bool PreserveLength { get; set; }

    /// <summary>
    /// The character used when <see cref="PreserveLength"/> is enabled. Defaults to <c>'*'</c>.
    /// </summary>
    public char MaskCharacter { get; set; } = '*';

    /// <summary>
    /// An optional regular expression applied to the string representation of the value.
    /// Matches are replaced with <see cref="Replacement"/>. When set, this takes precedence
    /// over <see cref="MaskValue"/> and <see cref="PreserveLength"/>.
    /// </summary>
    public Regex? Pattern { get; set; }

    /// <summary>
    /// The replacement applied to matches of <see cref="Pattern"/>. Supports regex substitution
    /// syntax (for example <c>"$1****"</c>). Defaults to <see cref="DefaultMask"/>.
    /// </summary>
    public string Replacement { get; set; } = DefaultMask;

    /// <summary>
    /// The default options: replace the whole value with <see cref="DefaultMask"/>.
    /// </summary>
    public static MaskingOptions Default { get; } = new();

    /// <summary>
    /// Whether <see cref="Apply"/> reads the original value. This is only the case when a
    /// <see cref="Pattern"/> is set or <see cref="PreserveLength"/> is enabled; for the default
    /// fixed-<see cref="MaskValue"/> behavior the original value is ignored and callers can skip
    /// materializing it.
    /// </summary>
    internal bool RequiresRawValue => Pattern is not null || PreserveLength;

    /// <summary>
    /// Produces the masked representation of the supplied raw string value according to these options.
    /// </summary>
    /// <param name="value">The original value's string representation.</param>
    /// <returns>The masked string.</returns>
    internal string Apply(string value)
    {
        if (Pattern is not null)
        {
            return Pattern.Replace(value, Replacement);
        }

        if (PreserveLength)
        {
            return new string(MaskCharacter, value.Length);
        }

        return MaskValue;
    }
}
