using System;
using AWS.Lambda.Powertools.Common.Tests;
using AWS.Lambda.Powertools.Common.Utils;
using AWS.Lambda.Powertools.Logging.Internal;
using AWS.Lambda.Powertools.Logging.Internal.Helpers;
using Microsoft.Extensions.Logging;
using Xunit;

namespace AWS.Lambda.Powertools.Logging.Tests.NativeAot;

/// <summary>
/// Under Native AOT the property metadata of anonymous types (and other types declared without a namespace)
/// is removed, so Logging can't read them. These tests simulate Native AOT with
/// <see cref="RuntimeFeatureWrapper.SetIsDynamicCodeSupported"/>.
/// </summary>
[Collection("Sequential")]
public class NamespacelessTypesUnderAotTests : IDisposable
{
    private readonly TestLoggerOutput _output = new();

    public NamespacelessTypesUnderAotTests()
    {
        ResetAllState();
        Logger.Configure(options => options.LogOutput = _output);
    }

    [Fact]
    public void AppendKey_WithAnonymousType_UnderAot_WarnsOnce()
    {
        RuntimeFeatureWrapper.SetIsDynamicCodeSupported(false);

        Logger.AppendKey("first", new { A = 1 });
        Logger.AppendKey("second", new { B = 2 });
        Logger.LogInformation("entry");

        var output = _output.ToString();
        Assert.Equal(1, CountOccurrences(output, PowertoolsLoggerHelpers.NamespacelessTypeUnderAotWarning));
        Assert.Contains("\"first\":{}", output);
        Assert.Contains("\"second\":{}", output);
    }

    [Fact]
    public void LogInformation_WithAnonymousObjectMessage_UnderAot_WarnsOnce()
    {
        RuntimeFeatureWrapper.SetIsDynamicCodeSupported(false);

        Logger.LogInformation(new { A = 1 });
        Logger.LogInformation(new { B = 2 });

        var output = _output.ToString();
        Assert.Equal(1, CountOccurrences(output, PowertoolsLoggerHelpers.NamespacelessTypeUnderAotWarning));
        Assert.Equal(2, CountOccurrences(output, "\"message\":{}"));
    }

    [Fact]
    public void CustomFormatter_ReturningAnonymousType_UnderAot_FallsBackToStandardOutputAndWarnsOnce()
    {
        RuntimeFeatureWrapper.SetIsDynamicCodeSupported(false);
        Logger.Configure(options =>
        {
            options.LogOutput = _output;
            options.LogFormatter = new AnonymousTypeFormatter();
        });

        Logger.LogInformation("first entry");
        Logger.LogInformation("second entry");

        var output = _output.ToString();
        var warning = PowertoolsLoggerHelpers.FormatterReturnedNamespacelessTypeUnderAotWarning(typeof(AnonymousTypeFormatter));
        Assert.Equal(1, CountOccurrences(output, warning));
        Assert.Contains(typeof(AnonymousTypeFormatter).FullName!, warning);
        Assert.Contains("\"message\":\"first entry\"", output);
        Assert.Contains("\"message\":\"second entry\"", output);
        Assert.Contains("\"level\":\"Information\"", output);
        Assert.DoesNotContain("custom_format", output);
    }

    [Fact]
    public void CustomFormatter_ReturningAnonymousType_WithDynamicCode_UsesFormatterOutput()
    {
        RuntimeFeatureWrapper.SetIsDynamicCodeSupported(true);
        Logger.Configure(options =>
        {
            options.LogOutput = _output;
            options.LogFormatter = new AnonymousTypeFormatter();
        });

        Logger.LogInformation("entry");

        var output = _output.ToString();
        Assert.Contains("\"custom_format\":\"entry\"", output);
        Assert.DoesNotContain("Native AOT", output);
    }

    [Fact]
    public void BeginScope_WithAnonymousType_UnderAot_WarnsOnce()
    {
        RuntimeFeatureWrapper.SetIsDynamicCodeSupported(false);
        var logger = LoggerFactoryHelper.CreateAndConfigureFactory(new PowertoolsLoggerConfiguration
        {
            LogOutput = _output
        }).CreatePowertoolsLogger();

        using (logger.BeginScope(new { ScopeKey = 1 }))
            logger.LogInformation("first entry");
        using (logger.BeginScope(new { OtherKey = 2 }))
            logger.LogInformation("second entry");

        var output = _output.ToString();
        Assert.Equal(1, CountOccurrences(output, PowertoolsLoggerHelpers.ObjectScopeStateUnderAotWarning));
        Assert.Contains("second entry", output);
        Assert.DoesNotContain("scope_key", output);
    }

    [Fact]
    public void BeginScope_WithAnonymousType_WithDynamicCode_AddsScopeKeys()
    {
        RuntimeFeatureWrapper.SetIsDynamicCodeSupported(true);
        var logger = LoggerFactoryHelper.CreateAndConfigureFactory(new PowertoolsLoggerConfiguration
        {
            LogOutput = _output
        }).CreatePowertoolsLogger();

        using (logger.BeginScope(new { ScopeKey = 1 }))
            logger.LogInformation("entry");

        var output = _output.ToString();
        Assert.Contains("\"scope_key\":1", output);
        Assert.DoesNotContain("Native AOT", output);
    }

    private sealed class AnonymousTypeFormatter : ILogFormatter
    {
        public object FormatLogEntry(LogEntry logEntry) => new { CustomFormat = logEntry.Message };
    }

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    public void Dispose()
    {
        ResetAllState();
    }

    private static void ResetAllState()
    {
        RuntimeFeatureWrapper.Reset();
        LoggingAspect.ResetForTest();
        Logger.Reset();
        PowertoolsLoggingBuilderExtensions.ResetAllProviders();
    }
}
