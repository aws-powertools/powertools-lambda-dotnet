#if NET8_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.IO;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Logging.Internal.Helpers;
using AWS.Lambda.Powertools.Logging.Serializers;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.Logging.Tests.Utilities;

public class PowertoolsLoggerHelpersTests : IDisposable
{
    [Fact]
    public void ObjectToDictionary_AnonymousObjectWithSimpleProperties_ReturnsDictionary()
    {
        // Arrange
        var anonymousObject = new { name = "test", age = 30 };

        // Act
        var result = PowertoolsLoggerHelpers.ObjectToDictionary(anonymousObject);

        // Assert
        Assert.IsType<Dictionary<string, object>>(result);
        var dictionary = (Dictionary<string, object>)result;
        Assert.Equal(2, dictionary.Count);
        Assert.Equal("test", dictionary["name"]);
        Assert.Equal(30, dictionary["age"]);
    }

    [Fact]
    public void ObjectToDictionary_AnonymousObjectWithNestedObject_ReturnsDictionaryWithNestedDictionary()
    {
        // Arrange
        var anonymousObject = new { name = "test", nested = new { id = 1 } };

        // Act
        var result = PowertoolsLoggerHelpers.ObjectToDictionary(anonymousObject);

        // Assert
        Assert.IsType<Dictionary<string, object>>(result);
        var dictionary = (Dictionary<string, object>)result;
        Assert.Equal(2, dictionary.Count);
        Assert.Equal("test", dictionary["name"]);
        Assert.IsType<Dictionary<string, object>>(dictionary["nested"]);
        var nestedDictionary = (Dictionary<string, object>)dictionary["nested"];
        Assert.Single(nestedDictionary);
        Assert.Equal(1, nestedDictionary["id"]);
    }

    [Fact]
    public void ObjectToDictionary_ObjectWithNamespace_ReturnsOriginalObject()
    {
        // Arrange
        var objectWithNamespace = new System.Text.StringBuilder();

        // Act
        var result = PowertoolsLoggerHelpers.ObjectToDictionary(objectWithNamespace);

        // Assert
        Assert.Same(objectWithNamespace, result);
    }

    [Fact]
    public void ObjectToDictionary_NullObject_Return_New_Dictionary()
    {
        // Act & Assert
        Assert.NotNull(() => PowertoolsLoggerHelpers.ObjectToDictionary(null));
    }

    [Fact]
    public void Should_Log_With_Anonymous()
    {
        var consoleOut = Substitute.For<StringWriter>();
        SystemWrapper.Instance.SetOut(consoleOut);

        // Act & Assert
        Logger.AppendKey("newKey", new
        {
            name = "my name"
        });

        Logger.LogInformation("test");

        consoleOut.Received(1).WriteLine(
            Arg.Is<string>(i =>
                i.Contains("\"new_key\":{\"name\":\"my name\"}"))
        );
    }

    [Fact]
    public void Should_Log_With_Complex_Anonymous()
    {
        var consoleOut = Substitute.For<StringWriter>();
        SystemWrapper.Instance.SetOut(consoleOut);

        // Act & Assert
        Logger.AppendKey("newKey", new
        {
            id = 1,
            name = "my name",
            Adresses = new
            {
                street = "street 1",
                number = 1,
                city = new
                {
                    name = "city 1",
                    state = "state 1"
                }
            }
        });

        Logger.LogInformation("test");

        consoleOut.Received(1).WriteLine(
            Arg.Is<string>(i =>
                i.Contains(
                    "\"new_key\":{\"id\":1,\"name\":\"my name\",\"adresses\":{\"street\":\"street 1\",\"number\":1,\"city\":{\"name\":\"city 1\",\"state\":\"state 1\"}"))
        );
    }

    [Fact]
    public void ObjectToDictionary_EnumValue_ReturnsOriginalEnum()
    {
        // Arrange
        var enumValue = DayOfWeek.Monday;

        // Act
        var result = PowertoolsLoggerHelpers.ObjectToDictionary(enumValue);

        // Assert
        Assert.Equal(enumValue, result);
    }

    [Fact]
    public void ObjectToDictionary_ObjectWithNullProperty_ExcludesNullProperty()
    {
        // Arrange
        var objectWithNull = new { name = (string)null, age = 30 };

        // Act
        var result = PowertoolsLoggerHelpers.ObjectToDictionary(objectWithNull);

        // Assert
        Assert.IsType<Dictionary<string, object>>(result);
        var dictionary = (Dictionary<string, object>)result;
        Assert.Single(dictionary);
        Assert.Equal(30, dictionary["age"]);
        Assert.False(dictionary.ContainsKey("name"));
    }

    [Fact]
    public void ObjectToDictionary_EmptyAnonymousObject_ReturnsEmptyDictionary()
    {
        // Arrange
        var emptyObject = new { };

        // Act
        var result = PowertoolsLoggerHelpers.ObjectToDictionary(emptyObject);

        // Assert
        Assert.IsType<Dictionary<string, object>>(result);
        var dictionary = (Dictionary<string, object>)result;
        Assert.Empty(dictionary);
    }

    [Fact]
    public void ObjectToDictionary_ObjectWithNestedEnum_ReturnsDictionaryWithEnum()
    {
        // Arrange
        var objectWithEnum = new { name = "test", day = DayOfWeek.Friday };

        // Act
        var result = PowertoolsLoggerHelpers.ObjectToDictionary(objectWithEnum);

        // Assert
        Assert.IsType<Dictionary<string, object>>(result);
        var dictionary = (Dictionary<string, object>)result;
        Assert.Equal(2, dictionary.Count);
        Assert.Equal("test", dictionary["name"]);
        Assert.Equal(DayOfWeek.Friday, dictionary["day"]);
    }

    [Fact]
    public void ObjectToDictionary_ObjectWithAllNullProperties_ReturnsEmptyDictionary()
    {
        // Arrange
        var allNullObject = new { name = (string)null, address = (string)null };

        // Act
        var result = PowertoolsLoggerHelpers.ObjectToDictionary(allNullObject);

        // Assert
        Assert.IsType<Dictionary<string, object>>(result);
        var dictionary = (Dictionary<string, object>)result;
        Assert.Empty(dictionary);
    }

    public void Dispose()
    {
        PowertoolsLoggingSerializer.ConfigureNamingPolicy(LoggerOutputCase.Default);
        PowertoolsLoggingSerializer.ClearOptions();
    }
}

#endif