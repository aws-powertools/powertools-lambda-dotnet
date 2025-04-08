using System;
using AWS.Lambda.Powertools.Common.Core;
using Xunit;

namespace AWS.Lambda.Powertools.Common.Tests;

public class LambdaLifecycleTrackerTests : IDisposable
    {
        public LambdaLifecycleTrackerTests()
        {
            // Reset before each test to ensure clean state
            LambdaLifecycleTracker.Reset();
            Environment.SetEnvironmentVariable(Constants.AWSInitializationTypeEnv, null);
        }

        public void Dispose()
        {
            // Reset after each test
            LambdaLifecycleTracker.Reset();
            Environment.SetEnvironmentVariable(Constants.AWSInitializationTypeEnv, null);
        }

        [Fact]
        public void IsColdStart_FirstInvocation_ReturnsTrue()
        {
            // Act
            var result = LambdaLifecycleTracker.IsColdStart;

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsColdStart_SecondInvocation_ReturnsFalse()
        {
            // Arrange - first access to trigger cold start
            _ = LambdaLifecycleTracker.IsColdStart;
    
            // Clear just the AsyncLocal value to simulate new invocation in same container
            LambdaLifecycleTracker.Reset(resetContainer: false);
    
            // Act - second invocation on same container
            var result = LambdaLifecycleTracker.IsColdStart;
    
            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsColdStart_WithProvisionedConcurrency_ReturnsFalse()
        {
            // Arrange
            Environment.SetEnvironmentVariable(Constants.AWSInitializationTypeEnv, "provisioned-concurrency");

            // Act
            var result = LambdaLifecycleTracker.IsColdStart;

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsColdStart_ReturnsSameValueWithinInvocation()
        {
            // Act - access multiple times in the same invocation
            var firstAccess = LambdaLifecycleTracker.IsColdStart;
            var secondAccess = LambdaLifecycleTracker.IsColdStart;
            var thirdAccess = LambdaLifecycleTracker.IsColdStart;

            // Assert
            Assert.True(firstAccess);
            Assert.Equal(firstAccess, secondAccess);
            Assert.Equal(firstAccess, thirdAccess);
        }

        [Fact]
        public void Reset_ResetsState()
        {
            // Arrange
            _ = LambdaLifecycleTracker.IsColdStart; // First invocation
            
            // Act
            LambdaLifecycleTracker.Reset();
            var result = LambdaLifecycleTracker.IsColdStart;

            // Assert
            Assert.True(result); // Should be true again after reset
        }

        [Fact]
        public void Reset_ClearsEnvironmentSetting()
        {
            // Arrange
            Environment.SetEnvironmentVariable(Constants.AWSInitializationTypeEnv, "provisioned-concurrency");
            _ = LambdaLifecycleTracker.IsColdStart; // Load the environment variable
            
            // Act
            LambdaLifecycleTracker.Reset();
            Environment.SetEnvironmentVariable(Constants.AWSInitializationTypeEnv, null); // Clear the environment
            var result = LambdaLifecycleTracker.IsColdStart;

            // Assert
            Assert.True(result); // Should be true when env var is cleared
        }
    }