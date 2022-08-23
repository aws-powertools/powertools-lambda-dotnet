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

//TODO: Replace with core PowerTools logging
namespace AWS.Lambda.Powertools.Idempotency.Output;

public interface ILog
{
    /// <summary>
    /// Writes an informational message to the log.
    /// </summary>
    /// <param name="format">The format.</param>
    /// <param name="args">The args.</param>
    void WriteInformation(string format, params object[] args);

    /// <summary>
    /// Writes an error message to the log.
    /// </summary>
    /// <param name="format">The format.</param>
    /// <param name="args">The args.</param>
    void WriteError(string format, params object[] args);

    /// <summary>
    /// Writes a warning message to the log.
    /// </summary>
    /// <param name="format">The format.</param>
    /// <param name="args">The args.</param>
    void WriteWarning(string format, params object[] args);
    
    /// <summary>
    /// Writes a debug message to the log.
    /// </summary>
    /// <param name="format">The format.</param>
    /// <param name="args">The args.</param>
    void WriteDebug(string format, params object[] args);
}