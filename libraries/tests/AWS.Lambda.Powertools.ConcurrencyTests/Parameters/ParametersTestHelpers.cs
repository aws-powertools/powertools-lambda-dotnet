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

namespace AWS.Lambda.Powertools.ConcurrencyTests.Parameters;

/// <summary>
/// Result class for tracking thread safety test outcomes.
/// Used to verify that concurrent operations complete without exceptions.
/// </summary>
internal class ThreadSafetyResult
{
    public string InvocationId { get; set; } = string.Empty;
    public int ThreadIndex { get; set; }
    public int OperationsAttempted { get; set; }
    public int OperationsCompleted { get; set; }
    public bool ExceptionThrown { get; set; }
    public string? ExceptionMessage { get; set; }
    public string? ExceptionType { get; set; }
}

/// <summary>
/// Result class for tracking cache operation outcomes.
/// Used to verify cache read/write operations complete correctly.
/// </summary>
internal class CacheOperationResult
{
    public string InvocationId { get; set; } = string.Empty;
    public int ThreadIndex { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public object? Value { get; set; }
    public bool Success { get; set; }
    public bool ExceptionThrown { get; set; }
    public string? ExceptionMessage { get; set; }
    public string? ExceptionType { get; set; }
}


/// <summary>
/// Result class for tracking provider singleton access outcomes.
/// Used to verify that concurrent access to provider singletons returns the same instance.
/// </summary>
internal class ProviderAccessResult
{
    public string InvocationId { get; set; } = string.Empty;
    public int ThreadIndex { get; set; }
    public string ProviderType { get; set; } = string.Empty;
    public int ProviderHashCode { get; set; }
    public bool ExceptionThrown { get; set; }
    public string? ExceptionMessage { get; set; }
    public string? ExceptionType { get; set; }
}

/// <summary>
/// Result class for tracking transformer operation outcomes.
/// Used to verify transformer retrieval and registration operations.
/// </summary>
internal class TransformerOperationResult
{
    public string InvocationId { get; set; } = string.Empty;
    public int ThreadIndex { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public string TransformerName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public bool ExceptionThrown { get; set; }
    public string? ExceptionMessage { get; set; }
    public string? ExceptionType { get; set; }
}

/// <summary>
/// Result class for tracking parameter retrieval outcomes.
/// Used to verify concurrent GetAsync and GetMultipleAsync operations.
/// </summary>
internal class ParameterRetrievalResult
{
    public string InvocationId { get; set; } = string.Empty;
    public int ThreadIndex { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public bool Success { get; set; }
    public bool ExceptionThrown { get; set; }
    public string? ExceptionMessage { get; set; }
    public string? ExceptionType { get; set; }
}

/// <summary>
/// Result class for tracking configuration operation outcomes.
/// Used to verify concurrent configuration changes.
/// </summary>
internal class ConfigurationResult
{
    public string InvocationId { get; set; } = string.Empty;
    public int ThreadIndex { get; set; }
    public string ConfigurationType { get; set; } = string.Empty;
    public TimeSpan? MaxAgeSet { get; set; }
    public bool Success { get; set; }
    public bool ExceptionThrown { get; set; }
    public string? ExceptionMessage { get; set; }
    public string? ExceptionType { get; set; }
}

/// <summary>
/// Result class for tracking async context preservation.
/// Used to verify context is maintained across await points.
/// </summary>
internal class AsyncContextResult
{
    public string InvocationId { get; set; } = string.Empty;
    public int ThreadIndex { get; set; }
    public int ThreadIdBeforeAwait { get; set; }
    public int ThreadIdAfterAwait { get; set; }
    public bool ContextPreserved { get; set; }
    public bool ExceptionThrown { get; set; }
    public string? ExceptionMessage { get; set; }
    public string? ExceptionType { get; set; }
}
