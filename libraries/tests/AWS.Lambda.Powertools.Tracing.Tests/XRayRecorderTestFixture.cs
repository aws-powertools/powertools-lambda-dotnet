using System;
using AWS.Lambda.Powertools.Tracing.Internal;
using Xunit;

namespace AWS.Lambda.Powertools.Tracing.Tests;

/// <summary>
/// Test fixture to ensure proper cleanup of XRayRecorder singleton state between tests
/// </summary>
public class XRayRecorderTestFixture : IDisposable
{
    public void Dispose()
    {
        // Reset the singleton instance after each test to prevent test pollution
        XRayRecorder.ResetInstance();
    }
}

/// <summary>
/// Collection definition for tests that need isolated XRayRecorder instances
/// </summary>
[CollectionDefinition("XRayRecorderTests")]
public class XRayRecorderTestCollection : ICollectionFixture<XRayRecorderTestFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}