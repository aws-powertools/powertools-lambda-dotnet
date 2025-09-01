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

namespace AWS.Lambda.Powertools.BatchProcessing;

/// <summary>
/// The <see cref="ITypedRecordHandlerProvider{T}"/> interface for creating strongly-typed record handlers.
/// </summary>
/// <typeparam name="T">Type of the deserialized data from the batch record.</typeparam>
public interface ITypedRecordHandlerProvider<in T>
{
    /// <summary>
    /// Creates a typed record handler.
    /// </summary>
    /// <returns>The created typed record handler.</returns>
    ITypedRecordHandler<T> Create();
}