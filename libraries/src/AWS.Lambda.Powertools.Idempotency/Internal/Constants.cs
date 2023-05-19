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

namespace AWS.Lambda.Powertools.Idempotency.Internal;

/// <summary>
///     Class Constants
/// </summary>
internal class Constants {
    /// <summary>
    /// Constant for LAMBDA_FUNCTION_NAME_ENV environment variable
    /// </summary>
    internal const string LambdaFunctionNameEnv = "AWS_LAMBDA_FUNCTION_NAME";
    /// <summary>
    /// Constant for AWS_REGION_ENV environment variable
    /// </summary>
    internal const string AwsRegionEnv = "AWS_REGION";
}
