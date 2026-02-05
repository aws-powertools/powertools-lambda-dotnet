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

using AWS.Lambda.Powertools.Metadata.Exceptions;
using AWS.Lambda.Powertools.Metadata.Internal;
using FluentAssertions;
using Xunit;

namespace AWS.Lambda.Powertools.Metadata.Tests.Internal;

public class MetadataFetcherTests
{
    [Fact]
    public void Fetch_ThrowsWhenTokenMissing()
    {
        // Arrange
        Environment.SetEnvironmentVariable("AWS_LAMBDA_METADATA_TOKEN", null);
        Environment.SetEnvironmentVariable("AWS_LAMBDA_METADATA_API", "localhost:8080");

        try
        {
            var fetcher = new MetadataFetcher();
            var act = () => fetcher.Fetch();
            act.Should().Throw<LambdaMetadataException>()
                .WithMessage("*AWS_LAMBDA_METADATA_TOKEN*");
        }
        finally
        {
            Environment.SetEnvironmentVariable("AWS_LAMBDA_METADATA_API", null);
        }
    }

    [Fact]
    public void Fetch_ThrowsWhenApiMissing()
    {
        // Arrange
        Environment.SetEnvironmentVariable("AWS_LAMBDA_METADATA_TOKEN", "test-token");
        Environment.SetEnvironmentVariable("AWS_LAMBDA_METADATA_API", null);

        try
        {
            var fetcher = new MetadataFetcher();
            var act = () => fetcher.Fetch();
            act.Should().Throw<LambdaMetadataException>()
                .WithMessage("*AWS_LAMBDA_METADATA_API*");
        }
        finally
        {
            Environment.SetEnvironmentVariable("AWS_LAMBDA_METADATA_TOKEN", null);
        }
    }
}
