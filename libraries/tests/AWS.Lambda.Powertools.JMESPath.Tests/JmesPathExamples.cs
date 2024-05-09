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

using System.Text.Json;
using Xunit.Abstractions;

namespace AWS.Lambda.Powertools.JMESPath.Tests;

public class JmesPathExamples
{
    private readonly ITestOutputHelper _output;
    private readonly JsonSerializerOptions _serializerOptions = new() { WriteIndented = false };

    public JmesPathExamples(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Select_With_Powertools_Json_Function()
    {
        var jsonString = """
                         {
                             "body": "{\"customerId\":\"dd4649e6-2484-4993-acb8-0f9123103394\"}",
                             "deeply_nested": [
                                 {
                                     "some_data": [
                                         1,
                                         2,
                                         3
                                     ]
                                 }
                             ]
                         }
                         """;

        using var doc = JsonDocument.Parse(jsonString);

        var transformer = JsonTransformer.Parse("powertools_json(body).customerId");
        using var result = transformer.Transform(doc.RootElement);

        _output.WriteLine(result.RootElement.GetRawText());

        Assert.Equal("dd4649e6-2484-4993-acb8-0f9123103394", result.RootElement.GetString());
    }

    [Fact]
    public void Select_With_Powertools_Base64_Function()
    {
        var jsonString = """
                         {
                             "body": "eyJjdXN0b21lcklkIjoiZGQ0NjQ5ZTYtMjQ4NC00OTkzLWFjYjgtMGY5MTIzMTAzMzk0In0=",
                             "deeply_nested": [
                                 {
                                     "some_data": [
                                         1,
                                         2,
                                         3
                                     ]
                                 }
                             ]
                         }
                         """;

        using var doc = JsonDocument.Parse(jsonString);

        var transformer = JsonTransformer.Parse("powertools_base64(body).customerId");
        using var result = transformer.Transform(doc.RootElement);

        _output.WriteLine(result.RootElement.GetRawText());

        Assert.Equal("dd4649e6-2484-4993-acb8-0f9123103394", result.RootElement.GetString());
    }

    [Fact]
    public void Select_With_Powertools_Base64_Gzip_Function()
    {
        var jsonString = """
                         {
                             "body": "H4sIAAAAAAAAA6tWSi4tLsnPTS3yTFGyUkpJMTEzsUw10zUysTDRNbG0NNZNTE6y0DVIszQ0MjY0MDa2NFGqBQCMzDWgNQAAAA==",
                             "deeply_nested": [
                                 {
                                     "some_data": [
                                         1,
                                         2,
                                         3
                                     ]
                                 }
                             ]
                         }
                         """;

        using var doc = JsonDocument.Parse(jsonString);

        var transformer = JsonTransformer.Parse("powertools_base64_gzip(body).customerId");
        using var result = transformer.Transform(doc.RootElement);

        _output.WriteLine(result.RootElement.GetRawText());

        Assert.Equal("dd4649e6-2484-4993-acb8-0f9123103394", result.RootElement.GetString());
    }

    [Fact]
    public void FiltersAndMultiselectLists()
    {
        //Arrange
        
        var jsonString = """
                         {
                           "people": [
                             {
                               "age": 20,
                               "other": "foo",
                               "name": "Bob"
                             },
                             {
                               "age": 25,
                               "other": "bar",
                               "name": "Fred"
                             },
                             {
                               "age": 30,
                               "other": "baz",
                               "name": "George"
                             }
                           ]
                         }
                         """;

        using var doc = JsonDocument.Parse(jsonString);
        
        var expectedJson = """[["Fred",25],["George",30]]""";
        
        //Act

        var transformer = JsonTransformer.Parse("people[?age > `20`].[name, age]");

        using var result = transformer.Transform(doc.RootElement);
        
        var actualJson = JsonSerializer.Serialize(result.RootElement, _serializerOptions);
        
        //Assert
        
        _output.WriteLine(actualJson);
        Assert.Equal(expectedJson, actualJson);
    }
    
    // Source: https://jmespath.org/examples.html#filters-and-multiselect-hashes
    [Fact]
    public void FiltersAndMultiselectHashes()
    {
        //Arrange
        
        var jsonString = """

                         {
                           "people": [
                             {
                               "age": 20,
                               "other": "foo",
                               "name": "Bob"
                             },
                             {
                               "age": 25,
                               "other": "bar",
                               "name": "Fred"
                             },
                             {
                               "age": 30,
                               "other": "baz",
                               "name": "George"
                             }
                           ]
                         }
                                 
                         """;

        using var doc = JsonDocument.Parse(jsonString);
        var expectedJson = """[{"name":"Fred","age":25},{"name":"George","age":30}]""";
        
        // Act
        
        var transformer = JsonTransformer.Parse("people[?age > `20`].{name: name, age: age}");

        using var result = transformer.Transform(doc.RootElement);
        var actualJson = JsonSerializer.Serialize(result.RootElement, _serializerOptions);
        
        //Assert
        
        _output.WriteLine(actualJson);
        Assert.Equal(expectedJson, actualJson);
    }

    // Source: https://jmespath.org/examples.html#working-with-nested-data
    [Fact]
    public void WorkingWithNestedData()
    {
        // Arrange
        
        var jsonString = """

                         {
                           "reservations": [
                             {
                               "instances": [
                                 {"type": "small",
                                  "state": {"name": "running"},
                                  "tags": [{"Key": "Name",
                                            "Values": ["Web"]},
                                           {"Key": "version",
                                            "Values": ["1"]}]},
                                 {"type": "large",
                                  "state": {"name": "stopped"},
                                  "tags": [{"Key": "Name",
                                            "Values": ["Web"]},
                                           {"Key": "version",
                                            "Values": ["1"]}]}
                               ]
                             }, {
                               "instances": [
                                 {"type": "medium",
                                  "state": {"name": "terminated"},
                                  "tags": [{"Key": "Name",
                                            "Values": ["Web"]},
                                           {"Key": "version",
                                            "Values": ["1"]}]},
                                 {"type": "xlarge",
                                  "state": {"name": "running"},
                                  "tags": [{"Key": "Name",
                                            "Values": ["DB"]},
                                           {"Key": "version",
                                            "Values": ["1"]}]}
                               ]
                             }
                           ]
                         }
                                 
                         """;

        using var doc = JsonDocument.Parse(jsonString);
        var expectedJson = """[["Web","small","running"],["Web","large","stopped"],["Web","medium","terminated"],["DB","xlarge","running"]]""";
        
        // Act
        
        var transformer =
            JsonTransformer.Parse("reservations[].instances[].[tags[?Key=='Name'].Values[] | [0], type, state.name]");

        using var result = transformer.Transform(doc.RootElement);
        var actualJson = JsonSerializer.Serialize(result.RootElement, _serializerOptions);
        
        //Assert
        
        _output.WriteLine(actualJson);
        Assert.Equal(expectedJson, actualJson);
    }

    // Source: https://jmespath.org/examples.html#filtering-and-selecting-nested-data
    [Fact]
    public void FilteringAndSelectingNestedData()
    {
        //Arrange
        
        var jsonString = """

                         {
                           "people": [
                             {
                               "general": {
                                 "id": 100,
                                 "age": 20,
                                 "other": "foo",
                                 "name": "Bob"
                               },
                               "history": {
                                 "first_login": "2014-01-01",
                                 "last_login": "2014-01-02"
                               }
                             },
                             {
                               "general": {
                                 "id": 101,
                                 "age": 30,
                                 "other": "bar",
                                 "name": "Bill"
                               },
                               "history": {
                                 "first_login": "2014-05-01",
                                 "last_login": "2014-05-02"
                               }
                             }
                           ]
                         }
                                 
                         """;

        using var doc = JsonDocument.Parse(jsonString);
        var expectedJson = """{"id":100,"age":20,"other":"foo","name":"Bob"}""";
        
        // Act
        
        var transformer = JsonTransformer.Parse("people[?general.id==`100`].general | [0]");
        using var result = transformer.Transform(doc.RootElement);
        var actualJson = JsonSerializer.Serialize(result.RootElement, _serializerOptions);
        
        //Assert
        
        _output.WriteLine(actualJson);
        Assert.Equal(expectedJson, actualJson);
    }

    // Source: https://jmespath.org/examples.html#using-functions
    [Fact]
    public void UsingFunctions()
    {
        // Arrange
        
        var jsonString = """

                         {
                           "Contents": [
                             {
                               "Date": "2014-12-21T05:18:08.000Z",
                               "Key": "logs/bb",
                               "Size": 303
                             },
                             {
                               "Date": "2014-12-20T05:19:10.000Z",
                               "Key": "logs/aa",
                               "Size": 308
                             },
                             {
                               "Date": "2014-12-20T05:19:12.000Z",
                               "Key": "logs/qux",
                               "Size": 297
                             },
                             {
                               "Date": "2014-11-20T05:22:23.000Z",
                               "Key": "logs/baz",
                               "Size": 329
                             },
                             {
                               "Date": "2014-12-20T05:25:24.000Z",
                               "Key": "logs/bar",
                               "Size": 604
                             },
                             {
                               "Date": "2014-12-20T05:27:12.000Z",
                               "Key": "logs/foo",
                               "Size": 647
                             }
                           ]
                         }
                                 
                         """;

        using var doc = JsonDocument.Parse(jsonString);
        var expectedJson = """[{"Key":"logs/baz","Size":329},{"Key":"logs/aa","Size":308},{"Key":"logs/qux","Size":297},{"Key":"logs/bar","Size":604},{"Key":"logs/foo","Size":647},{"Key":"logs/bb","Size":303}]""";
        
        // Act
        
        var transformer = JsonTransformer.Parse("sort_by(Contents, &Date)[*].{Key: Key, Size: Size}");
        using var result = transformer.Transform(doc.RootElement);
        var actualJson = JsonSerializer.Serialize(result.RootElement, _serializerOptions);
        
        //Assert
        
        _output.WriteLine(actualJson);
        Assert.Equal(expectedJson, actualJson);
    }
    
    [Fact]
    public void SortBySize()
    {
        // Arrange
        
        var jsonString = """

                         {
                           "Contents": [
                             {
                               "Date": "2014-12-21T05:18:08.000Z",
                               "Key": "logs/bb",
                               "Size": 303
                             },
                             {
                               "Date": "2014-12-20T05:19:10.000Z",
                               "Key": "logs/aa",
                               "Size": 308
                             },
                             {
                               "Date": "2014-12-20T05:19:12.000Z",
                               "Key": "logs/qux",
                               "Size": 297
                             },
                             {
                               "Date": "2014-11-20T05:22:23.000Z",
                               "Key": "logs/baz",
                               "Size": 329
                             },
                             {
                               "Date": "2014-12-20T05:25:24.000Z",
                               "Key": "logs/bar",
                               "Size": 604
                             },
                             {
                               "Date": "2014-12-20T05:27:12.000Z",
                               "Key": "logs/foo",
                               "Size": 647
                             }
                           ]
                         }
                                 
                         """;

        using var doc = JsonDocument.Parse(jsonString);
        var expectedJson = """[{"Size":297},{"Size":303},{"Size":308},{"Size":329},{"Size":604},{"Size":647}]""";
        
        // Act
        
        var transformer = JsonTransformer.Parse("sort_by(Contents, &Size)[*].{Size: Size}");
        using var result = transformer.Transform(doc.RootElement);
        var actualJson = JsonSerializer.Serialize(result.RootElement, _serializerOptions);
        
        //Assert
        
        _output.WriteLine(actualJson);
        Assert.Equal(expectedJson, actualJson);
    }

    [Fact]
    public void KeyOfInterest()
    {
        var jsonString = """

                         {
                             "Data":[
                                 {
                                     "KeyOfInterest":true,
                                     "AnotherKey":true
                                 },
                                 {
                                     "KeyOfInterest":false,
                                     "AnotherKey":true
                                 },
                                 {
                                     "KeyOfInterest":true,
                                     "AnotherKey":true
                                 }
                             ]
                         }
                                 
                         """;

        using var doc = JsonDocument.Parse(jsonString);
        
        var expectedJson1 = "[true,false,true]";
        var expectedJson2 = """[{"Key of Interest":true,"Another Key":true},{"Key of Interest":false,"Another Key":true},{"Key of Interest":true,"Another Key":true}]""";

        // Act
        
        var result1 = JsonTransformer.Transform(doc.RootElement,
            "Data[*].KeyOfInterest");
        var result2 = JsonTransformer.Transform(doc.RootElement,
            "Data[*].{\"Key of Interest\" : KeyOfInterest, \"Another Key\": AnotherKey}");
        
        var actualJson1 = JsonSerializer.Serialize(result1);
        var actualJson2 = JsonSerializer.Serialize(result2, _serializerOptions);
        
        // Assert
        
        _output.WriteLine(JsonSerializer.Serialize(result1));
        _output.WriteLine(JsonSerializer.Serialize(result2, _serializerOptions));
        
        Assert.Equal(expectedJson1, actualJson1);
        Assert.Equal(expectedJson2, actualJson2);
    }
}