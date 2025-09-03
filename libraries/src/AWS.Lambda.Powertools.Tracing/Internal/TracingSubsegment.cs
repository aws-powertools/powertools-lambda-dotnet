using Amazon.XRay.Recorder.Core.Internal.Entities;

namespace AWS.Lambda.Powertools.Tracing.Internal;

/// <summary>
///     Class TracingSubsegment.
///     It's a wrapper for Subsegment from Amazon.XRay.Recorder.Core.Internal.
/// </summary>
/// <seealso cref="Subsegment" />
public class TracingSubsegment : Subsegment
{
    /// <summary>
    /// Wrapper constructor
    /// </summary>
    /// <param name="name"></param> 
    public TracingSubsegment(string name) : base(name) { }
}