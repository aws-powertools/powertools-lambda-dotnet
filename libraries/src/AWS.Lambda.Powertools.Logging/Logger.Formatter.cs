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

namespace AWS.Lambda.Powertools.Logging;

public static partial class Logger
{
    /// <summary>
    ///     Set the log formatter.
    /// </summary>
    /// <param name="logFormatter">The log formatter.</param>
    /// <remarks>WARNING: This method should not be called when using AOT. ILogFormatter should be passed to PowertoolsSourceGeneratorSerializer constructor</remarks>
    public static void UseFormatter(ILogFormatter logFormatter)
    {
        Configure(config => {
            config.LogFormatter = logFormatter;
        });
    }

    /// <summary>
    ///     Set the log formatter to default.
    /// </summary>
    public static void UseDefaultFormatter()
    {
        Configure(config => {
            config.LogFormatter = null;
        });
    }
}
