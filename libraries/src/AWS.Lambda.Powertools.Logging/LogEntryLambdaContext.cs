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

using System;
using Amazon.Lambda.Core;

namespace AWS.Lambda.Powertools.Logging;

/// <summary>
/// Powertools Log Entry Lambda Context
/// </summary>
public class LogEntryLambdaContext : ILambdaContext
{
    /// <summary>
    /// The AWS request ID associated with the request.
    /// This is the same ID returned to the client that called invoke().
    /// This ID is reused for retries on the same request.
    /// </summary>
    public string AwsRequestId { get; internal set; }

    /// <inheritdoc />
    public IClientContext ClientContext { get; }

    /// <summary>
    /// Name of the Lambda function that is running.
    /// </summary>
    public string FunctionName { get; internal set; }
        
    /// <summary>
    /// The Lambda function version that is executing.
    /// If an alias is used to invoke the function, then this will be
    /// the version the alias points to.
    /// </summary>
    public string FunctionVersion { get; internal set; }

    /// <inheritdoc />
    public ICognitoIdentity Identity { get; }

    /// <inheritdoc />
    public string LogStreamName { get; }

    /// <summary>
    /// The ARN used to invoke this function.
    /// It can be function ARN or alias ARN.
    /// An unqualified ARN executes the $LATEST version and aliases execute
    /// the function version they are pointing to.
    /// </summary>
    public int MemoryLimitInMB { get; internal set; }

    /// <inheritdoc />
    public TimeSpan RemainingTime { get; }

    /// <summary>
    /// The CloudWatch log group name associated with the invoked function.
    /// It can be null if the IAM user provided does not have permission for
    /// CloudWatch actions.
    /// </summary>
    public string InvokedFunctionArn { get; internal set; }

    /// <summary>
    /// Lambda Logger
    /// </summary>
    public ILambdaLogger Logger { get; }

    /// <inheritdoc />
    public string LogGroupName { get; }
}