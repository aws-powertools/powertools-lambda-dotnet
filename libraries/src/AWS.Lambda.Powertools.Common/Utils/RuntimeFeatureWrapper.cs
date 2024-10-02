using System;
using System.Runtime.CompilerServices;

namespace AWS.Lambda.Powertools.Common.Utils;

/// <summary>
/// Wrapper for RuntimeFeature
/// </summary>
public static class RuntimeFeatureWrapper
{
    private static Func<bool> _isDynamicCodeSupportedFunc = () => RuntimeFeature.IsDynamicCodeSupported;
    
    /// <summary>
    /// Check to see if IsDynamicCodeSupported
    /// </summary>
    public static bool IsDynamicCodeSupported => _isDynamicCodeSupportedFunc();

// For testing purposes
    internal static void SetIsDynamicCodeSupported(bool value)
    {
        _isDynamicCodeSupportedFunc = () => value;
    }

// To reset after tests
    internal static void Reset()
    {
        _isDynamicCodeSupportedFunc = () => RuntimeFeature.IsDynamicCodeSupported;
    }
}