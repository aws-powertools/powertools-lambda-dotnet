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
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AWS.Lambda.Powertools.BatchProcessing;
using AWS.Lambda.Powertools.Logging;
using HelloWorld.Data;

namespace HelloWorld.DynamoDb;

/// <summary>
/// Data model representing the DynamoDB stream record structure.
/// </summary>
public class DynamoDbProductStreamRecord
{
    public string? EventName { get; set; }
    public DynamoDbNewImage? NewImage { get; set; }
    public string? SequenceNumber { get; set; }
}

public class DynamoDbNewImage
{
    public DynamoDbAttributeValue? Product { get; set; }
}

public class DynamoDbAttributeValue
{
    public string? S { get; set; }
}

/// <summary>
/// Typed record handler for DynamoDB stream records.
/// Receives the deserialized DynamoDB stream record and extracts the Product from NewImage.
/// </summary>
public class TypedDynamoDbProductHandler : ITypedRecordHandler<DynamoDbProductStreamRecord>
{
    public Task<RecordHandlerResult> HandleAsync(DynamoDbProductStreamRecord record, CancellationToken cancellationToken)
    {
        /*
         * Your business logic with strongly-typed data.
         * The batch processor automatically deserializes the DynamoDB stream record.
         * We then extract and deserialize the Product from the NewImage attribute.
         */
        Logger.LogInformation($"[Typed] Handling DynamoDB record with event: {record.EventName}");
        
        // Extract the Product JSON from the NewImage attribute
        var productJson = record.NewImage?.Product?.S;
        if (string.IsNullOrEmpty(productJson))
        {
            throw new ArgumentException("Product attribute is missing or empty");
        }

        // Check for failure marker
        if (productJson == "failure")
        {
            throw new ArgumentException("Error on failure product");
        }
        
        var product = JsonSerializer.Deserialize<Product>(productJson);
        
        Logger.LogInformation($"[Typed] Handling product with id: {product!.Id}, name: {product.Name}");

        if (product.Id == 4)
        {
            throw new ArgumentException("Error on id 4");
        }
        
        return Task.FromResult(RecordHandlerResult.None);
    }
}
