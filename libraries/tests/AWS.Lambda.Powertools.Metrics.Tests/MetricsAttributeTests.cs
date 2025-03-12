using Xunit;

namespace AWS.Lambda.Powertools.Metrics.Tests;

[Collection("Sequential")]
public class MetricsAttributeTests
{
    [Fact]
    public void MetricsAttribute_WhenCaptureColdStartSet_ShouldSetFlag()
    {
        // Arrange & Act
        var attribute = new MetricsAttribute
        {
            CaptureColdStart = true
        };

        // Assert
        Assert.True(attribute.CaptureColdStart);
        Assert.True(attribute.IsCaptureColdStartSet);
    }

    [Fact]
    public void MetricsAttribute_WhenCaptureColdStartNotSet_ShouldNotSetFlag()
    {
        // Arrange & Act
        var attribute = new MetricsAttribute();

        // Assert
        Assert.False(attribute.CaptureColdStart);
        Assert.False(attribute.IsCaptureColdStartSet);
    }

    [Fact]
    public void MetricsAttribute_WhenRaiseOnEmptyMetricsSet_ShouldSetFlag()
    {
        // Arrange & Act
        var attribute = new MetricsAttribute
        {
            RaiseOnEmptyMetrics = true
        };

        // Assert
        Assert.True(attribute.RaiseOnEmptyMetrics);
        Assert.True(attribute.IsRaiseOnEmptyMetricsSet);
    }

    [Fact]
    public void MetricsAttribute_WhenRaiseOnEmptyMetricsNotSet_ShouldNotSetFlag()
    {
        // Arrange & Act
        var attribute = new MetricsAttribute();

        // Assert
        Assert.False(attribute.RaiseOnEmptyMetrics);
        Assert.False(attribute.IsRaiseOnEmptyMetricsSet);
    }

    [Fact]
    public void MetricsAttribute_ShouldSetNamespaceAndService()
    {
        // Arrange & Act
        var attribute = new MetricsAttribute
        {
            Namespace = "TestNamespace",
            Service = "TestService"
        };

        // Assert
        Assert.Equal("TestNamespace", attribute.Namespace);
        Assert.Equal("TestService", attribute.Service);
    }
}