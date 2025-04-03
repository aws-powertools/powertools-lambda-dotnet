/* * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. *  * Licensed under the Apache License, Version 2.0 (the "License"). * You may not use this file except in compliance with the License. * A copy of the License is located at *  *  http://aws.amazon.com/apache2.0 * .cs
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

using AWS.Lambda.Powertools.Logging.Internal;

namespace AWS.Lambda.Powertools.Logging;

public static partial class Logger
{
    /// <summary>
    /// Flush any buffered logs
    /// </summary>
    public static void FlushBuffer()
    {
        // Use the buffer manager directly
        LogBufferManager.FlushCurrentBuffer();
    }

    /// <summary>
    /// Clear any buffered logs without writing them
    /// </summary>
    public static void ClearBuffer()
    {
        // Use the buffer manager directly
        LogBufferManager.ClearCurrentBuffer();
    }
}