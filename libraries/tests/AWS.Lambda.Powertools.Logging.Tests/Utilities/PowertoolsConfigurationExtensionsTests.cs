using System;
using Xunit;
using AWS.Lambda.Powertools.Logging.Internal;

namespace AWS.Lambda.Powertools.Logging.Tests.Utilities;

public class PowertoolsConfigurationExtensionsTests : IDisposable
{
    [Theory]
    [InlineData(LoggerOutputCase.CamelCase, "TestString", "testString")]
    [InlineData(LoggerOutputCase.PascalCase, "testString", "TestString")]
    [InlineData(LoggerOutputCase.SnakeCase, "TestString", "test_string")]
    [InlineData(LoggerOutputCase.SnakeCase, "testString", "test_string")] // Default case
    public void ConvertToOutputCase_ShouldConvertCorrectly(LoggerOutputCase outputCase, string input, string expected)
    {
        // Act
        var result = input.ToCase(outputCase);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("TestString", "test_string")]
    [InlineData("testString", "test_string")]
    [InlineData("Test_String", "test_string")]
    [InlineData("TEST_STRING", "test_string")]
    [InlineData("test", "test")]
    [InlineData("TestStringABC", "test_string_abc")]
    [InlineData("TestStringABCTest", "test_string_abc_test")]
    [InlineData("Test__String", "test__string")]
    [InlineData("TEST", "test")]
    [InlineData("ABCTestDEF", "abc_test_def")]
    [InlineData("ABC_TEST_DEF", "abc_test_def")]
    [InlineData("abcTestDef", "abc_test_def")]
    [InlineData("abc_test_def", "abc_test_def")]
    [InlineData("Abc_Test_Def", "abc_test_def")]
    [InlineData("ABC", "abc")]
    [InlineData("A_B_C", "a_b_c")]
    [InlineData("ABCDEFG", "abcdefg")]
    [InlineData("ABCDefGHI", "abc_def_ghi")]
    [InlineData("ABCTestDEFGhi", "abc_test_def_ghi")]
    [InlineData("Test___String", "test___string")]
    public void ToSnakeCase_ShouldConvertCorrectly(string input, string expected)
    {
        // Act
        var result = input.ToSnake();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("testString", "TestString")]
    [InlineData("TestString", "TestString")]
    [InlineData("test", "Test")]
    [InlineData("test_string", "TestString")]
    [InlineData("test_string_abc", "TestStringAbc")]
    [InlineData("test_stringABC", "TestStringABC")]
    [InlineData("test__string", "TestString")]
    [InlineData("TEST_STRING", "TestString")]
    [InlineData("t", "T")]
    [InlineData("", "")]
    [InlineData("abc_def_ghi", "AbcDefGhi")]
    [InlineData("ABC_DEF_GHI", "AbcDefGhi")]
    [InlineData("abc123_def456", "Abc123Def456")]
    [InlineData("_test_string", "TestString")]
    [InlineData("test_string_", "TestString")]
    [InlineData("__test__string__", "TestString")]
    [InlineData("TEST__STRING", "TestString")]
    [InlineData("testString123", "TestString123")]
    [InlineData("test_string_123", "TestString123")]
    [InlineData("123_test_string", "123TestString")]
    [InlineData("test_1_string", "Test1String")]
    public void ToPascalCase_ShouldConvertCorrectly(string input, string expected)
    {
        // Act
        var result = input.ToPascal();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("test_string", "testString")]
    [InlineData("testString", "testString")]
    [InlineData("TestString", "testString")]
    [InlineData("test_string_abc", "testStringAbc")]
    [InlineData("test_stringABC", "testStringABC")]
    [InlineData("test__string", "testString")]
    [InlineData("TEST_STRING", "testString")]
    [InlineData("test", "test")]
    [InlineData("T", "t")]
    [InlineData("", "")]
    [InlineData("abc_def_ghi", "abcDefGhi")]
    [InlineData("ABC_DEF_GHI", "abcDefGhi")]
    [InlineData("abc123_def456", "abc123Def456")]
    [InlineData("_test_string", "testString")]
    [InlineData("test_string_", "testString")]
    [InlineData("__test__string__", "testString")]
    [InlineData("TEST__STRING", "testString")]
    [InlineData("testString123", "testString123")]
    [InlineData("test_string_123", "testString123")]
    [InlineData("123_test_string", "123TestString")]
    [InlineData("test_1_string", "test1String")]
    [InlineData("Test_string", "testString")]
    [InlineData("Test_String", "testString")]
    [InlineData("Test_String_Abc", "testStringAbc")]
    [InlineData("alreadyCamelCase", "alreadyCamelCase")]
    [InlineData("ALLCAPS", "allcaps")]
    [InlineData("ALL_CAPS", "allCaps")]
    [InlineData("single", "single")]
    public void ToCamelCase_ShouldConvertCorrectly(string input, string expected)
    {
        // Act
        var result = input.ToCamel();

        // Assert
        Assert.Equal(expected, result);
    }

    public void Dispose()
    {
        LoggingAspect.ResetForTest();
    }
}

// Helper class to invoke private static methods
public static class PrivateMethod
{
    public static T InvokeStatic<T>(Type type, string methodName, params object[] parameters)
    {
        var method = type.GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return (T)method!.Invoke(null, parameters);
    }
}