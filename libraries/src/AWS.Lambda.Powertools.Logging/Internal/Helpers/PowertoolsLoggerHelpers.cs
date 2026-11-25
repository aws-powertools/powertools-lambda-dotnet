using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Common.Utils;

namespace AWS.Lambda.Powertools.Logging.Internal.Helpers;

/// <summary>
/// Class PowertoolsLoggerHelpers.
/// </summary>
internal static class PowertoolsLoggerHelpers
{
    /// <summary>
    ///     Warning written once when an anonymous type, or another type declared without a namespace, is logged
    ///     under Native AOT.
    /// </summary>
    internal const string NamespacelessTypeUnderAotWarning =
        "Powertools Logging can't read the properties of anonymous types, or of other types declared without a " +
        "namespace, when running as Native AOT, so their properties are not logged. Use a Dictionary<string, object>, " +
        "or a class declared in a namespace and registered in your JsonSerializerContext, instead.";

    /// <summary>
    /// Converts an object to a dictionary.
    /// </summary>
    /// <param name="anonymousObject">The object to convert.</param>
    /// <param name="warningOutput">Where to write one-time warnings. Defaults to the console.</param>
    /// <returns>
    /// If the object has a namespace, returns the object as-is.
    /// Otherwise, returns a dictionary representation of the object's properties. Under Native AOT, where those
    /// properties can't be read, returns an empty dictionary and writes <see cref="NamespacelessTypeUnderAotWarning" /> once.
    /// </returns>
    internal static object ObjectToDictionary(object anonymousObject, IConsoleWrapper warningOutput = null)
    {
        if (anonymousObject == null)
        {
            return new Dictionary<string, object>();
        }

        var type = anonymousObject.GetType();

        if (!NeedsPropertyReflection(type))
        {
            return anonymousObject;
        }

        // JsonSerializer.IsReflectionEnabledByDefault is a feature switch that is false in trimmed and Native AOT
        // apps, where the property metadata ReadProperties needs isn't available. The trimmer and ILC know it's
        // false there, so they don't report the guarded [RequiresUnreferencedCode] call.
        // RuntimeFeatureWrapper lets tests simulate Native AOT.
        if (JsonSerializer.IsReflectionEnabledByDefault && RuntimeFeatureWrapper.IsDynamicCodeSupported)
        {
#pragma warning disable IL2026 // Guarded by JsonSerializer.IsReflectionEnabledByDefault; needed for the net8.0 analyzer.
            return ReadProperties(anonymousObject, type, warningOutput);
#pragma warning restore IL2026
        }

        WarnNamespacelessTypeUnderAot(warningOutput);
        return new Dictionary<string, object>();
    }

    /// <summary>
    ///     Warning written once when <c>BeginScope</c> is called with an object state under Native AOT.
    /// </summary>
    internal const string ObjectScopeStateUnderAotWarning =
        "Powertools Logging can't read the properties of an object passed to BeginScope when running as Native AOT, " +
        "so no scope keys are added for it. Pass a Dictionary<string, object> or " +
        "IEnumerable<KeyValuePair<string, object>> to BeginScope instead.";

    /// <summary>
    ///     Warning written once per formatter type when a custom log formatter returns an anonymous type, or another
    ///     type declared without a namespace, under Native AOT.
    /// </summary>
    internal static string FormatterReturnedNamespacelessTypeUnderAotWarning(Type formatterType)
    {
        return $"Powertools Logging: the custom log formatter {formatterType.FullName} returned an anonymous type, " +
               "or another type declared without a namespace, which can't be read when running as Native AOT. " +
               "Using the standard log output instead. Return a class declared in a namespace and registered in " +
               "your JsonSerializerContext.";
    }

    /// <summary>
    ///     Whether Logging has to read the type's properties with reflection: anonymous types, and other
    ///     types declared without a namespace. Other types are passed to the JSON serializer as they are.
    /// </summary>
    internal static bool NeedsPropertyReflection(Type type)
    {
        return type.Namespace == null && !type.IsEnum;
    }

    /// <summary>
    ///     Writes <see cref="NamespacelessTypeUnderAotWarning" /> once per execution environment.
    /// </summary>
    internal static void WarnNamespacelessTypeUnderAot(IConsoleWrapper warningOutput)
    {
        if (OneTimeWarnings.ShouldWrite(NamespacelessTypeUnderAotWarning))
        {
            (warningOutput ?? new ConsoleWrapper()).WriteLine(NamespacelessTypeUnderAotWarning);
        }
    }

    [RequiresUnreferencedCode("Reads the properties of types that can't be statically analyzed.")]
    private static Dictionary<string, object> ReadProperties(object anonymousObject, Type type,
        IConsoleWrapper warningOutput)
    {
        return type.GetProperties()
            .Where(prop => prop.GetValue(anonymousObject, null) != null)
            .ToDictionary(
                prop => prop.Name,
                prop => {
                    var value = prop.GetValue(anonymousObject, null);
                    if (value == null)
                        return string.Empty;
                
                    if (value.GetType().IsEnum)
                        return value;
                
                    return ObjectToDictionary(value, warningOutput);
                }
            );
    }
}