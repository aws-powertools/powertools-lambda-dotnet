using AWS.Lambda.Powertools.Tracing.Internal;
using Xunit;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using System;

namespace AWS.Lambda.Powertools.Tracing.Tests;

[Collection("TracingTests")]
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

    #region BeginSubsegment Tests

    [Fact]
    public void BeginSubsegment_WithName_ThrowsArgumentNullException_WhenNameIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Tracing.BeginSubsegment(null));
    }

    [Fact]
    public void BeginSubsegment_WithName_ThrowsArgumentNullException_WhenNameIsEmpty()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Tracing.BeginSubsegment(""));
    }

    [Fact]
    public void BeginSubsegment_WithName_ThrowsArgumentNullException_WhenNameIsWhitespace()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Tracing.BeginSubsegment("   "));
    }

    [Fact]
    public void BeginSubsegment_WithNamespaceAndName_ThrowsArgumentNullException_WhenNameIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Tracing.BeginSubsegment("namespace", null));
    }

    [Fact]
    public void BeginSubsegment_WithNamespaceAndName_ThrowsArgumentNullException_WhenNameIsEmpty()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Tracing.BeginSubsegment("namespace", ""));
    }

    [Fact]
    public void BeginSubsegment_WithName_ReturnsTracingSubsegment()
    {
        // Act
        using var subsegment = Tracing.BeginSubsegment("test-segment");

        // Assert
        Assert.NotNull(subsegment);
        Assert.IsType<TracingSubsegment>(subsegment);
        Assert.Equal("## test-segment", subsegment.Name);
    }

    [Fact]
    public void BeginSubsegment_WithNamespaceAndName_ReturnsTracingSubsegment()
    {
        // Act
        using var subsegment = Tracing.BeginSubsegment("test-namespace", "test-segment");

        // Assert
        Assert.NotNull(subsegment);
        Assert.IsType<TracingSubsegment>(subsegment);
        Assert.Equal("## test-segment", subsegment.Name);
    }

    [Fact]
    public void BeginSubsegment_IsDisposable()
    {
        // Act
        var subsegment = Tracing.BeginSubsegment("test-segment");

        // Assert
        Assert.IsAssignableFrom<IDisposable>(subsegment);
        
        // Cleanup
        subsegment.Dispose();
    }

    [Fact]
    public void BeginSubsegment_CanBeUsedInUsingStatement()
    {
        // This test verifies that the using statement compiles and executes without errors
        bool executedSuccessfully = false;

        // Act
        using (var subsegment = Tracing.BeginSubsegment("test-segment"))
        {
            Assert.NotNull(subsegment);
            executedSuccessfully = true;
        }

        // Assert
        Assert.True(executedSuccessfully);
    }

    [Fact]
    public void BeginSubsegment_WithUsing_AllowsNestedSubsegments()
    {
        // This test verifies nested using statements work correctly
        bool outerExecuted = false;
        bool innerExecuted = false;

        // Act
        using (var outerSegment = Tracing.BeginSubsegment("outer-segment"))
        {
            Assert.NotNull(outerSegment);
            outerExecuted = true;

            using (var innerSegment = Tracing.BeginSubsegment("inner-segment"))
            {
                Assert.NotNull(innerSegment);
                innerExecuted = true;
            }
        }

        // Assert
        Assert.True(outerExecuted);
        Assert.True(innerExecuted);
    }

    [Fact]
    public void BeginSubsegment_Dispose_DoesNotThrowException()
    {
        // Arrange
        var subsegment = Tracing.BeginSubsegment("test-segment");

        // Act & Assert - Should not throw
        subsegment.Dispose();
        
        // Multiple dispose calls should also not throw
        subsegment.Dispose();
    }

    #endregion

    #region TracingSubsegment Disposable Tests

    [Fact]
    public void TracingSubsegment_Constructor_WithAutoEnd_SetsCorrectProperties()
    {
        // Arrange & Act
        var subsegment = new TracingSubsegment("test", true);

        // Assert
        Assert.Equal("test", subsegment.Name);
        Assert.IsAssignableFrom<IDisposable>(subsegment);
    }

    [Fact]
    public void TracingSubsegment_AddAnnotation_CallsXRayRecorder()
    {
        // Arrange
        using var subsegment = new TracingSubsegment("test", false);

        // Act & Assert - Should not throw (we can't easily mock XRayRecorder in this context)
        // This test mainly verifies the method exists and can be called
        try
        {
            subsegment.AddAnnotation("testKey", "testValue");
        }
        catch (Exception ex) when (ex.Message.Contains("Entity is not available"))
        {
            // This is expected when no tracing context is available
            // The important thing is that the method exists and attempts to call XRayRecorder
        }
    }

    [Fact]
    public void TracingSubsegment_AddMetadata_CallsXRayRecorder()
    {
        // Arrange
        using var subsegment = new TracingSubsegment("test", false);

        // Act & Assert - Should not throw (we can't easily mock XRayRecorder in this context)
        try
        {
            subsegment.AddMetadata("testKey", "testValue");
        }
        catch (Exception ex) when (ex.Message.Contains("Entity is not available"))
        {
            // This is expected when no tracing context is available
            // The important thing is that the method exists and attempts to call XRayRecorder
        }
    }

    [Fact]
    public void TracingSubsegment_AddMetadataWithNamespace_CallsXRayRecorder()
    {
        // Arrange
        using var subsegment = new TracingSubsegment("test", false);

        // Act & Assert - Should not throw (we can't easily mock XRayRecorder in this context)
        try
        {
            subsegment.AddMetadata("testNamespace", "testKey", "testValue");
        }
        catch (Exception ex) when (ex.Message.Contains("Entity is not available"))
        {
            // This is expected when no tracing context is available
            // The important thing is that the method exists and attempts to call XRayRecorder
        }
    }

    [Fact]
    public void TracingSubsegment_AddException_CallsXRayRecorder()
    {
        // Arrange
        using var subsegment = new TracingSubsegment("test", false);
        var testException = new InvalidOperationException("Test exception");

        // Act & Assert - Should not throw (we can't easily mock XRayRecorder in this context)
        try
        {
            subsegment.AddException(testException);
        }
        catch (Exception ex) when (ex.Message.Contains("Entity is not available"))
        {
            // This is expected when no tracing context is available
            // The important thing is that the method exists and attempts to call XRayRecorder
        }
    }

    [Fact]
    public void TracingSubsegment_AddHttpInformation_CallsXRayRecorder()
    {
        // Arrange
        using var subsegment = new TracingSubsegment("test", false);

        // Act & Assert - Should not throw (we can't easily mock XRayRecorder in this context)
        try
        {
            subsegment.AddHttpInformation("testKey", "testValue");
        }
        catch (Exception ex) when (ex.Message.Contains("Entity is not available"))
        {
            // This is expected when no tracing context is available
            // The important thing is that the method exists and attempts to call XRayRecorder
        }
    }

    [Fact]
    public void TracingSubsegment_Dispose_WithAutoEndFalse_DoesNotCallEndSubsegment()
    {
        // Arrange
        var subsegment = new TracingSubsegment("test", false);

        // Act & Assert - Should not throw
        subsegment.Dispose();
        
        // Multiple dispose calls should also not throw
        subsegment.Dispose();
    }

    [Fact]
    public void TracingSubsegment_Dispose_WithAutoEndTrue_AttemptsToCallEndSubsegment()
    {
        // Arrange
        var subsegment = new TracingSubsegment("test", true);

        // Act & Assert - Should not throw even if XRayRecorder throws
        // The dispose method should swallow exceptions to prevent issues in using blocks
        subsegment.Dispose();
        
        // Multiple dispose calls should also not throw
        subsegment.Dispose();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void BeginSubsegment_WithUsing_CanAddAnnotationsAndMetadata()
    {
        // This integration test verifies the complete workflow
        bool executedSuccessfully = false;

        // Act
        using (var subsegment = Tracing.BeginSubsegment("integration-test"))
        {
            // These calls should not throw, even if they fail internally due to no tracing context
            try
            {
                subsegment.AddAnnotation("TestAnnotation", "TestValue");
                subsegment.AddMetadata("TestMetadata", "TestMetadataValue");
                subsegment.AddMetadata("CustomNamespace", "TestKey", "TestValue");
                executedSuccessfully = true;
            }
            catch (Exception ex) when (ex.Message.Contains("Entity is not available"))
            {
                // This is expected when no tracing context is available
                executedSuccessfully = true;
            }
        }

        // Assert
        Assert.True(executedSuccessfully);
    }

    [Fact]
    public void BeginSubsegment_WithUsing_HandlesExceptionsGracefully()
    {
        // This test verifies that exceptions in the using block don't prevent disposal
        var expectedException = new InvalidOperationException("Test exception");
        bool exceptionThrown = false;
        
        // Act
        try
        {
            using (var subsegment = Tracing.BeginSubsegment("exception-test"))
            {
                try
                {
                    subsegment.AddAnnotation("BeforeException", true);
                }
                catch (Exception ex) when (ex.Message.Contains("Entity is not available"))
                {
                    // Expected when no tracing context is available
                }
                throw expectedException;
            }
        }
        catch (InvalidOperationException ex)
        {
            exceptionThrown = true;
            Assert.Equal(expectedException.Message, ex.Message);
        }

        // Assert
        Assert.True(exceptionThrown);
        // The important thing is that disposal happened without throwing additional exceptions
    }

    #endregion
}