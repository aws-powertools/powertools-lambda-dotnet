using System;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using AWS.Lambda.Powertools.Common;

namespace AWS.Lambda.Powertools.Tracing.Internal;

/// <summary>
///     Class TracingSubsegment.
///     It's a wrapper for Subsegment from Amazon.XRay.Recorder.Core.Internal.
/// </summary>
/// <seealso cref="Subsegment" />
public class TracingSubsegment : Subsegment, IDisposable
{
    private bool _disposed = false;
    private readonly bool _shouldAutoEnd;

    /// <summary>
    /// Wrapper constructor
    /// </summary>
    /// <param name="name"></param> 
    public TracingSubsegment(string name) : base(name) 
    { 
        _shouldAutoEnd = false;
    }

    /// <summary>
    /// Constructor for disposable subsegments
    /// </summary>
    /// <param name="name">The name of the subsegment</param>
    /// <param name="shouldAutoEnd">Whether this subsegment should auto-end when disposed</param>
    internal TracingSubsegment(string name, bool shouldAutoEnd) : base(name) 
    { 
        _shouldAutoEnd = shouldAutoEnd;
    }

    /// <summary>
    /// Adds an annotation to the subsegment
    /// </summary>
    /// <param name="key">The annotation key</param>
    /// <param name="value">The annotation value</param>
    public new void AddAnnotation(string key, object value)
    {
        XRayRecorder.Instance.AddAnnotation(key, value);
    }

    /// <summary>
    /// Adds metadata to the subsegment
    /// </summary>
    /// <param name="key">The metadata key</param>
    /// <param name="value">The metadata value</param>
    public new void AddMetadata(string key, object value)
    {
        XRayRecorder.Instance.AddMetadata(Namespace ?? PowertoolsConfigurations.Instance.Service, key, value);
    }

    /// <summary>
    /// Adds metadata to the subsegment with a specific namespace
    /// </summary>
    /// <param name="nameSpace">The namespace</param>
    /// <param name="key">The metadata key</param>
    /// <param name="value">The metadata value</param>
    public new void AddMetadata(string nameSpace, string key, object value)
    {
        XRayRecorder.Instance.AddMetadata(nameSpace, key, value);
    }

    /// <summary>
    /// Adds an exception to the subsegment
    /// </summary>
    /// <param name="exception">The exception to add</param>
    public new void AddException(Exception exception)
    {
        XRayRecorder.Instance.AddException(exception);
    }

    /// <summary>
    /// Adds HTTP information to the subsegment
    /// </summary>
    /// <param name="key">The HTTP information key</param>
    /// <param name="value">The HTTP information value</param>
    public void AddHttpInformation(string key, object value)
    {
        XRayRecorder.Instance.AddHttpInformation(key, value);
    }

    /// <summary>
    /// Disposes the subsegment and ends it if configured to do so
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected dispose method
    /// </summary>
    /// <param name="disposing">Whether we're disposing</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing && _shouldAutoEnd)
        {
            try
            {
                XRayRecorder.Instance.EndSubsegment();
            }
            catch
            {
                // Swallow exceptions during disposal to prevent issues in using blocks
            }
            _disposed = true;
        }
    }
}