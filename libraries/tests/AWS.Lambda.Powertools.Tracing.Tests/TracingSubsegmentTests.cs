using AWS.Lambda.Powertools.Tracing.Internal;
using Xunit;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using System;

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

    [Fact]
    public void WithSubsegment_WithEntity_ThrowsArgumentNullException_WhenNameIsNull()
    {
        // Arrange
        var parent = new Segment("parent", TraceId.NewId());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            Tracing.WithSubsegment(null, null, parent, _ => { }));
    }

    [Fact]
    public void WithSubsegment_WithEntity_ThrowsArgumentNullException_WhenNameIsEmpty()
    {
        // Arrange
        var parent = new Segment("parent", TraceId.NewId());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            Tracing.WithSubsegment(null, "", parent, _ => { }));
    }

    [Fact]
    public void WithSubsegment_WithEntity_ThrowsArgumentNullException_WhenEntityIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            Tracing.WithSubsegment(null, "test", null, _ => { }));
    }

    [Fact]
    public void WithSubsegment_WithEntity_CreatesSubsegmentWithCorrectName()
    {
        // Arrange
        var parent = new Segment("parent", TraceId.NewId());
        TracingSubsegment capturedSubsegment = null;

        // Act
        Tracing.WithSubsegment("test-namespace", "test-name", parent, subsegment =>
        {
            capturedSubsegment = subsegment;
        });

        // Assert
        Assert.NotNull(capturedSubsegment);
        Assert.Equal("## test-name", capturedSubsegment.Name);
        Assert.Equal("test-namespace", capturedSubsegment.Namespace);
    }

    [Fact]
    public void WithSubsegment_WithEntity_SetsSubsegmentProperties()
    {
        // Arrange
        var parent = new Segment("parent", TraceId.NewId());
        TracingSubsegment capturedSubsegment = null;

        // Act
        Tracing.WithSubsegment("test-namespace", "test-name", parent, subsegment =>
        {
            capturedSubsegment = subsegment;
        });

        // Assert
        Assert.NotNull(capturedSubsegment);
        Assert.Equal(parent.Sampled, capturedSubsegment.Sampled);
        Assert.False(capturedSubsegment.IsInProgress);
        Assert.True(capturedSubsegment.StartTime > 0);
        Assert.True(capturedSubsegment.EndTime > 0);
    }

    [Fact]
    public void WithSubsegment_WithEntity_AddsSubsegmentToParent()
    {
        // Arrange
        var parent = new Segment("parent", TraceId.NewId());
        var initialSubsegmentCount = parent.Subsegments?.Count ?? 0;

        // Act
        Tracing.WithSubsegment("test-namespace", "test-name", parent, _ => { });

        // Assert
        Assert.True(parent.IsSubsegmentsAdded);
        Assert.Equal(initialSubsegmentCount + 1, parent.Subsegments.Count);
    }

    [Fact]
    public void WithSubsegment_WithEntity_InvokesActionWithSubsegment()
    {
        // Arrange
        var parent = new Segment("parent", TraceId.NewId());
        bool actionInvoked = false;
        TracingSubsegment passedSubsegment = null;

        // Act
        Tracing.WithSubsegment("test-namespace", "test-name", parent, subsegment =>
        {
            actionInvoked = true;
            passedSubsegment = subsegment;
        });

        // Assert
        Assert.True(actionInvoked);
        Assert.NotNull(passedSubsegment);
        Assert.IsType<TracingSubsegment>(passedSubsegment);
    }

    [Fact]
    public void WithSubsegment_WithEntity_HandlesNullAction()
    {
        // Arrange
        var parent = new Segment("parent", TraceId.NewId());

        // Act & Assert - Should not throw
        Tracing.WithSubsegment("test-namespace", "test-name", parent, null);
    }

    [Fact]
    public void WithSubsegment_WithEntity_UsesDefaultNamespaceWhenNull()
    {
        // Arrange
        var parent = new Segment("parent", TraceId.NewId());
        TracingSubsegment capturedSubsegment = null;

        // Act
        Tracing.WithSubsegment(null, "test-name", parent, subsegment =>
        {
            capturedSubsegment = subsegment;
        });

        // Assert
        Assert.NotNull(capturedSubsegment);
        Assert.NotNull(capturedSubsegment.Namespace);
    }

    [Fact]
    public void WithSubsegment_WithEntity_HandlesExceptionInAction()
    {
        // Arrange
        var parent = new Segment("parent", TraceId.NewId());
        var expectedException = new InvalidOperationException("Test exception");

        // Act & Assert
        var actualException = Assert.Throws<InvalidOperationException>(() =>
        {
            Tracing.WithSubsegment("test-namespace", "test-name", parent, subsegment =>
            {
                throw expectedException;
            });
        });

        Assert.Equal(expectedException, actualException);
        // Verify subsegment was still properly cleaned up
        Assert.True(parent.IsSubsegmentsAdded);
    }
}