
using System;
using AWS.Lambda.Powertools.Common;

namespace AWS.Lambda.Powertools.Tracing.Internal;

internal static class TracingAspectFactory
{
    /// <summary>
    /// Get an instance of the TracingAspect class.
    /// </summary>
    /// <param name="type">The type of the class to be logged.</param>
    /// <returns>An instance of the TracingAspect class.</returns>
    public static object GetInstance(Type type)
    {
        return new TracingAspect(PowertoolsConfigurations.Instance, XRayRecorder.Instance);
    }
}