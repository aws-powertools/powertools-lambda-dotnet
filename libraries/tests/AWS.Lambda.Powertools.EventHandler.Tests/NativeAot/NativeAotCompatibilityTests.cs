using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.EventHandler.AppSyncEvents;
using AWS.Lambda.Powertools.EventHandler.Resolvers;
using AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Models;
using Xunit;

namespace AWS.Lambda.Powertools.EventHandler.Tests.NativeAot;

public class NativeAotCompatibilityTests
{
    [Theory]
    [InlineData(ResponseState.REPROMPT, "\"responseState\":\"REPROMPT\"")]
    [InlineData(ResponseState.FAILURE, "\"responseState\":\"FAILURE\"")]
    public void FunctionResponse_ResponseState_SerializesAsString_WithSourceGeneratedContext(ResponseState state,
        string expected)
    {
        var response = new FunctionResponse { ResponseState = state };

        var json = JsonSerializer.Serialize(response, NativeAotTestContext.Default.FunctionResponse);

        Assert.Contains(expected, json);
    }

    [Fact]
    public void FunctionResponse_NullResponseState_IsOmitted_WithSourceGeneratedContext()
    {
        var json = JsonSerializer.Serialize(new FunctionResponse(), NativeAotTestContext.Default.FunctionResponse);

        Assert.DoesNotContain("responseState", json);
    }

    [Theory]
    [InlineData("PUBLISH", AppSyncEventsOperation.Publish)]
    [InlineData("SUBSCRIBE", AppSyncEventsOperation.Subscribe)]
    public void Information_Operation_DeserializesFromString_WithSourceGeneratedContext(string value,
        AppSyncEventsOperation expected)
    {
        var info = JsonSerializer.Deserialize($"{{\"operation\":\"{value}\"}}", NativeAotTestContext.Default.Information);

        Assert.Equal(expected, info!.Operation);
    }

    public static TheoryData<string> DelegateBasedRegistrationMethods => new()
    {
        nameof(BedrockAgentFunctionResolver.Tool) + "(String, Delegate)",
        nameof(BedrockAgentFunctionResolver.Tool) + "(String, String, Delegate)",
        nameof(BedrockAgentFunctionResolver.Tool) + "`1(String, Delegate)",
        nameof(BedrockAgentFunctionResolver.Tool) + "`1(String, String, Delegate)"
    };

    [Theory]
    [MemberData(nameof(DelegateBasedRegistrationMethods))]
    public void DelegateBasedToolRegistration_IsMarkedAsNotAotCompatible(string signature)
    {
        var method = FindMethod(signature);

        Assert.NotNull(method.GetCustomAttribute<RequiresUnreferencedCodeAttribute>());
        Assert.NotNull(method.GetCustomAttribute<RequiresDynamicCodeAttribute>());
    }

    [Theory]
    [InlineData(typeof(Func<BedrockFunctionRequest, ILambdaContext?, BedrockFunctionResponse>))]
    [InlineData(typeof(Func<BedrockFunctionRequest, BedrockFunctionResponse>))]
    [InlineData(typeof(Func<BedrockFunctionResponse>))]
    [InlineData(typeof(Func<string>))]
    [InlineData(typeof(Func<object>))]
    public void TypedToolRegistration_IsAotCompatible(Type handlerType)
    {
        var method = typeof(BedrockAgentFunctionResolver).GetMethod(nameof(BedrockAgentFunctionResolver.Tool),
            new[] { typeof(string), handlerType, typeof(string) });

        Assert.NotNull(method);
        Assert.Null(method!.GetCustomAttribute<RequiresUnreferencedCodeAttribute>());
        Assert.Null(method.GetCustomAttribute<RequiresDynamicCodeAttribute>());
    }

    [Fact]
    public void RegisterTool_IsAotCompatible()
    {
        var method = FindMethod(nameof(BedrockResolverExtensions.RegisterTool) + "`1(BedrockAgentFunctionResolver)");

        Assert.Null(method.GetCustomAttribute<RequiresUnreferencedCodeAttribute>());
        Assert.Null(method.GetCustomAttribute<RequiresDynamicCodeAttribute>());
        // Keeps the tool methods and their metadata in trimmed and Native AOT apps.
        var annotation = method.GetGenericArguments()[0].GetCustomAttribute<DynamicallyAccessedMembersAttribute>();
        Assert.Equal(DynamicallyAccessedMemberTypes.PublicMethods, annotation!.MemberTypes);
    }

    private static MethodInfo FindMethod(string signature)
    {
        var methods = typeof(BedrockAgentFunctionResolver).GetMethods()
            .Concat(typeof(BedrockResolverExtensions).GetMethods());

        return Assert.Single(methods, m => Describe(m) == signature);
    }

    private static string Describe(MethodInfo method)
    {
        var name = method.IsGenericMethodDefinition
            ? $"{method.Name}`{method.GetGenericArguments().Length}"
            : method.Name;
        return $"{name}({string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name))})";
    }
}

[JsonSerializable(typeof(FunctionResponse))]
[JsonSerializable(typeof(Information))]
internal partial class NativeAotTestContext : JsonSerializerContext
{
}
