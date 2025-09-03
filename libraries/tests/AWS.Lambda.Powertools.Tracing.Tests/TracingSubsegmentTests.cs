using AWS.Lambda.Powertools.Tracing.Internal;
using Xunit;
using Amazon.XRay.Recorder.Core.Internal.Entities;

namespace AWS.Lambda.Powertools.Tracing.Tests;

[Collection("Sequential")]
public class TracingSubsegmentTests
{

    [Fact]
    public void TracingSubsegment_Constructor_Should_Set_Name()
    {
        // Arrange
        var name = "test-segment";

        // Act
        var subsegment = new TracingSubsegment(name);

        // Assert
        Assert.Equal("test-segment", subsegment.Name);
        Assert.True(Entity.IsIdValid(subsegment.Id));
        Assert.Null(subsegment.TraceId);
        Assert.Null(subsegment.ParentId);
        Assert.False(subsegment.IsSubsegmentsAdded);
    }

    [Fact]
    public void Test_Add_Ref_And_Release_With_TracingSubsegment()
    {
        // Arrange
        var parent = new Segment("parent", TraceId.NewId());
        var child = new TracingSubsegment("child");

        // Act
        parent.AddSubsegment(child);

        // Assert
        Assert.Equal(2, parent.Reference);
        Assert.Equal(1, child.Reference);

        child.Release();
        Assert.Equal(1, parent.Reference);
        Assert.Equal(0, child.Reference);
        Assert.False(parent.IsEmittable());
        Assert.False(child.IsEmittable());

        parent.Release();
        Assert.Equal(0, parent.Reference);
        Assert.True(parent.IsEmittable());
        Assert.True(child.IsEmittable());
    }

    [Fact]
    public void IsEmittable_Returns_False_Without_Parent()
    {
        var subsegment = new TracingSubsegment("segment");
        Assert.False(subsegment.IsEmittable());
    }

    [Fact]
    public void TracingSubsegment_Is_Assignable_BaseClass()
    {
        // Arrange
        var subsegment = new TracingSubsegment("segment");

        // Act & Assert
        Assert.IsAssignableFrom<Subsegment>(subsegment);
    }

    [Fact]
    public void Tracing_WithSubsegment_Invokes_Delegate_With_TracingSubsegment()
    {
        // Arrange
        bool delegateInvoked = false;

        void TracingSubsegmentDelegate(TracingSubsegment subsegment)
        {
            delegateInvoked = true;
        }

        // Act
        Tracing.WithSubsegment("namespace", "test", TracingSubsegmentDelegate);

        // Assert
        Assert.True(delegateInvoked);
    }
}