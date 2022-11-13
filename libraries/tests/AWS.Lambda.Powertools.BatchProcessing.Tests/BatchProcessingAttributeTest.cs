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
using AWS.Lambda.Powertools.BatchProcessing.Internal;
using AWS.Lambda.Powertools.Common;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace AWS.Lambda.Powertools.BatchProcessing.Tests
{
    [Collection("Sequential")]
    public class BatchProcessingAttributeTestContext
    {
        [Fact]
        public void OnEntry_Test()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();

            BatchProcessingAspectHandler.ResetForTest();
            var handler = new BatchProcessingAspectHandler();
            var eventArgs = new AspectEventArgs
            {
                Name = methodName,
                Args = new object[] { }
            };

            // Act
            handler.OnEntry(eventArgs);

            // Assert

        }
    }
}
