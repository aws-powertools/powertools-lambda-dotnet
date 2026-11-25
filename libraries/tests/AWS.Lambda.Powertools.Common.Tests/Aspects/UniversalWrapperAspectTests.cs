using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AWS.Lambda.Powertools.Common.Utils;
using Xunit;

namespace AWS.Lambda.Powertools.Common.Tests.Aspects;

[Collection("RuntimeFeatureWrapper")]
public class UniversalWrapperAspectTests : IDisposable
{
    private readonly UniversalWrapperAspect _aspect = new();
    private readonly RecordingWrapperAttribute _wrapper = new();

    public void Dispose()
    {
        RuntimeFeatureWrapper.Reset();
    }

    public static IEnumerable<object[]> ValueTypeReturns()
    {
        yield return new object[] { typeof(int), "Int32" };
        yield return new object[] { typeof(Task<int>), "Task<Int32>" };
        yield return new object[] { typeof(Task), "Task" };
    }

    [Theory]
    [MemberData(nameof(ValueTypeReturns))]
    public void Handle_WhenDynamicCodeIsNotSupported_AndMethodReturnsValueType_ThrowsNotSupportedWithGuidance(
        Type returnType, string expectedTypeName)
    {
        RuntimeFeatureWrapper.SetIsDynamicCodeSupported(false);
        // A distinct delegate per case, because handlers are cached per target method.
        Func<object[], object> target = returnType == typeof(int)
            ? _ => 1
            : returnType == typeof(Task<int>)
                ? _ => Task.FromResult(1)
                : _ => Task.CompletedTask;

        var ex = Assert.Throws<NotSupportedException>(() => { Handle(target, returnType); });

        Assert.Contains($"'{nameof(UniversalWrapperAspectTests)}.{nameof(SampleMethod)}'", ex.Message);
        Assert.Contains($"'{expectedTypeName}'", ex.Message);
        Assert.Contains(nameof(RecordingWrapperAttribute), ex.Message);
        Assert.Contains("Native AOT", ex.Message);
        Assert.False(_wrapper.Called);
    }

    [Fact]
    public void Handle_WhenDynamicCodeIsNotSupported_AndMethodReturnsReferenceType_InvokesWrapper()
    {
        RuntimeFeatureWrapper.SetIsDynamicCodeSupported(false);

        var result = Handle(_ => "result", typeof(string));

        Assert.Equal("result", result);
        Assert.True(_wrapper.Called);
    }

    [Fact]
    public async Task Handle_WhenDynamicCodeIsNotSupported_AndMethodReturnsTaskOfReferenceType_InvokesWrapper()
    {
        RuntimeFeatureWrapper.SetIsDynamicCodeSupported(false);

        var result = Handle(_ => Task.FromResult("result"), typeof(Task<string>));

        Assert.Equal("result", await Assert.IsAssignableFrom<Task<string>>(result));
        Assert.True(_wrapper.Called);
    }

    [Fact]
    public void Handle_WhenDynamicCodeIsNotSupported_AndMethodReturnsVoid_InvokesWrapper()
    {
        RuntimeFeatureWrapper.SetIsDynamicCodeSupported(false);
        var invoked = false;

        Handle(_ =>
        {
            invoked = true;
            return null;
        }, typeof(void));

        Assert.True(invoked);
        Assert.True(_wrapper.Called);
    }

    [Fact]
    public void Handle_WhenDynamicCodeIsSupported_AndMethodReturnsValueType_InvokesWrapper()
    {
        RuntimeFeatureWrapper.SetIsDynamicCodeSupported(true);

        var result = Handle(_ => 42, typeof(int));

        Assert.Equal(42, result);
        Assert.True(_wrapper.Called);
    }

    [Fact]
    public async Task Handle_WhenDynamicCodeIsSupported_AndMethodReturnsTaskOfValueType_InvokesWrapper()
    {
        RuntimeFeatureWrapper.SetIsDynamicCodeSupported(true);

        var result = Handle(_ => Task.FromResult(42), typeof(Task<int>));

        Assert.Equal(42, await Assert.IsAssignableFrom<Task<int>>(result));
        Assert.True(_wrapper.Called);
    }

    [Fact]
    public async Task Handle_WhenDynamicCodeIsSupported_AndMethodReturnsTask_InvokesWrapper()
    {
        RuntimeFeatureWrapper.SetIsDynamicCodeSupported(true);
        var invoked = false;

        var result = Handle(_ =>
        {
            invoked = true;
            return Task.CompletedTask;
        }, typeof(Task));

        await Assert.IsAssignableFrom<Task>(result);
        Assert.True(invoked);
        Assert.True(_wrapper.Called);
    }

    private object Handle(Func<object[], object> target, Type returnType)
    {
        var method = typeof(UniversalWrapperAspectTests).GetMethod(nameof(SampleMethod),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return _aspect.Handle(this, GetType(), method, target, nameof(SampleMethod), Array.Empty<object>(),
            returnType, new Attribute[] { _wrapper });
    }

    private static void SampleMethod()
    {
    }

    private sealed class RecordingWrapperAttribute : UniversalWrapperAttribute
    {
        public bool Called { get; private set; }

        protected internal override T WrapSync<T>(Func<object[], T> target, object[] args, AspectEventArgs eventArgs)
        {
            Called = true;
            return base.WrapSync(target, args, eventArgs);
        }

        protected internal override Task<T> WrapAsync<T>(Func<object[], Task<T>> target, object[] args,
            AspectEventArgs eventArgs)
        {
            Called = true;
            return base.WrapAsync(target, args, eventArgs);
        }
    }
}
