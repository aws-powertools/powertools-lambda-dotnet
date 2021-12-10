using System;
using Amazon.Lambda.PowerTools.Tracing.Internal;
using AWS.Lambda.PowerTools.Core;
using Moq;
using Xunit;

namespace AWS.Lambda.PowerTools.Tracing.Tests
{
    public class SegmentScopeTest
    {
        [Fact]
        public void SegmentScope_OnCreate_BeginSubsegment()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var nameSpace = Guid.NewGuid().ToString();
            var configurations = new Mock<IPowerToolsConfigurations>();
            var recorder = new Mock<IXRayRecorder>();

            // Act
            var segmentScope = new SegmentScope(configurations.Object, recorder.Object, nameSpace, methodName, null);
            
            // Assert
            recorder.Verify(v =>
                v.BeginSubsegment(
                    It.Is<string>(i => i == methodName),
                    It.IsAny<DateTime?>()
                ), Times.Once);
        }
        
        [Fact]
        public void SegmentScope_WhenNamespaceHasValue_SetNamespaceWithValue()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var nameSpace = Guid.NewGuid().ToString();
            var configurations = new Mock<IPowerToolsConfigurations>();
            var recorder = new Mock<IXRayRecorder>();

            // Act
            var segmentScope = new SegmentScope(configurations.Object, recorder.Object, nameSpace, methodName, null);
            
            // Assert
            recorder.Verify(v =>
                v.BeginSubsegment(
                    It.Is<string>(i => i == methodName),
                    It.IsAny<DateTime?>()
                ), Times.Once);
            
            recorder.Verify(v =>
                v.SetNamespace(
                    It.Is<string>(i => i == nameSpace)
                ), Times.Once);
        }
        
        [Fact]
        public void SegmentScope_WhenNamespaceDoesNotHaveValue_SetNamespaceWithServiceName()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var serviceName = Guid.NewGuid().ToString();
            var configurations = new Mock<IPowerToolsConfigurations>();
            configurations.Setup(c =>
                c.ServiceName
            ).Returns(serviceName);
            var recorder = new Mock<IXRayRecorder>();

            // Act
            var segmentScope = new SegmentScope(configurations.Object, recorder.Object, null, methodName, null);
            
            // Assert
            recorder.Verify(v =>
                v.BeginSubsegment(
                    It.Is<string>(i => i == methodName),
                    It.IsAny<DateTime?>()
                ), Times.Once);
            
            recorder.Verify(v =>
                v.SetNamespace(
                    It.Is<string>(i => i == serviceName)
                ), Times.Once);
        }
        
        [Fact]
        public void SegmentScope_OnDispose_EndsSubsegment()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var nameSpace = Guid.NewGuid().ToString();
            var configurations = new Mock<IPowerToolsConfigurations>();
            var recorder = new Mock<IXRayRecorder>();

            // Act
            using (new SegmentScope(configurations.Object, recorder.Object, nameSpace, methodName, null))
            {
                
            }
            
            // Assert
            recorder.Verify(v =>
                v.BeginSubsegment(
                    It.Is<string>(i => i == methodName),
                    It.IsAny<DateTime?>()
                ), Times.Once);
            
            recorder.Verify(v =>
                v.SetNamespace(
                    It.Is<string>(i => i == nameSpace)
                ), Times.Once);

            recorder.Verify(v => v.EndSubsegment(), Times.Once);
        }
    }
}