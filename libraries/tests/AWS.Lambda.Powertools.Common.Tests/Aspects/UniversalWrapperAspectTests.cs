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

    public static IEnumerable<object[]> SupportedValueTypeReturns()
    {
        // A distinct delegate per case, because handlers are cached per target method.
        yield return new object[] { typeof(int), (Func<object[], object>)(_ => 42), 42 };
        yield return new object[] { typeof(long), (Func<object[], object>)(_ => 42L << 33), 42L << 33 };
        yield return new object[] { typeof(bool), (Func<object[], object>)(_ => true), true };
        yield return new object[] { typeof(double), (Func<object[], object>)(_ => 1.5), 1.5 };
        yield return new object[] { typeof(decimal), (Func<object[], object>)(_ => 12.34m), 12.34m };
        yield return new object[]
        {
            typeof(Guid), (Func<object[], object>)(_ => new Guid("6f9619ff-8b86-d011-b42d-00cf4fc964ff")),
            new Guid("6f9619ff-8b86-d011-b42d-00cf4fc964ff")
        };
        yield return new object[]
        {
            typeof(DateTime), (Func<object[], object>)(_ => new DateTime(2026, 11, 25, 0, 0, 0, DateTimeKind.Utc)),
            new DateTime(2026, 11, 25, 0, 0, 0, DateTimeKind.Utc)
        };
    }

    [Theory]
    [MemberData(nameof(SupportedValueTypeReturns))]
    public void Handle_WhenDynamicCodeIsNotSupported_AndMethodReturnsSupportedValueType_InvokesWrapper(
        Type returnType, Func<object[], object> target, object expected)
    {
        RuntimeFeatureWrapper.SetIsDynamicCodeSupported(false);

        var result = Handle(target, returnType);

        Assert.Equal(expected, result);
        Assert.Equal(returnType, _wrapper.WrappedType);
    }

    public static IEnumerable<object[]> UnsupportedValueTypeReturns()
    {
        // A distinct delegate per case, because handlers are cached per target method.
        yield return new object[] { typeof(UserStruct), (Func<object[], object>)(_ => new UserStruct(1)), "UserStruct" };
        yield return new object[]
        {
            typeof(Task<UserStruct>), (Func<object[], object>)(_ => Task.FromResult(new UserStruct(1))),
            "Task<UserStruct>"
        };
        yield return new object[]
            { typeof(ValueTask<int>), (Func<object[], object>)(_ => new ValueTask<int>(1)), "ValueTask<Int32>" };
    }

    [Theory]
    [MemberData(nameof(UnsupportedValueTypeReturns))]
    public void Handle_WhenDynamicCodeIsNotSupported_AndMethodReturnsUnsupportedValueType_ThrowsNotSupportedWithGuidance(
        Type returnType, Func<object[], object> target, string expectedTypeName)
    {
        RuntimeFeatureWrapper.SetIsDynamicCodeSupported(false);

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

    [Fact]
    public void Handle_WhenDynamicCodeIsSupported_AndMethodReturnsUserStruct_InvokesWrapper()
    {
        RuntimeFeatureWrapper.SetIsDynamicCodeSupported(true);

        var result = Handle(_ => new UserStruct(7), typeof(UserStruct));

        Assert.Equal(new UserStruct(7), result);
        Assert.Equal(typeof(UserStruct), _wrapper.WrappedType);
    }

    [Fact]
    public async Task Handle_WhenDynamicCodeIsSupported_AndMethodReturnsTaskOfUserStruct_InvokesWrapper()
    {
        RuntimeFeatureWrapper.SetIsDynamicCodeSupported(true);

        var result = Handle(_ => Task.FromResult(new UserStruct(7)), typeof(Task<UserStruct>));

        Assert.Equal(new UserStruct(7), await Assert.IsAssignableFrom<Task<UserStruct>>(result));
        Assert.Equal(typeof(UserStruct), _wrapper.WrappedType);
    }

    public static IEnumerable<object[]> SupportedTaskOfValueTypeReturns()
    {
        // A distinct delegate per case, because handlers are cached per target method.
        yield return new object[] { typeof(int), (Func<object[], object>)(_ => Task.FromResult(42)), 42 };
        yield return new object[] { typeof(long), (Func<object[], object>)(_ => Task.FromResult(42L << 33)), 42L << 33 };
        yield return new object[] { typeof(bool), (Func<object[], object>)(_ => Task.FromResult(true)), true };
        yield return new object[] { typeof(double), (Func<object[], object>)(_ => Task.FromResult(1.5)), 1.5 };
        yield return new object[] { typeof(decimal), (Func<object[], object>)(_ => Task.FromResult(12.34m)), 12.34m };
        yield return new object[]
        {
            typeof(Guid), (Func<object[], object>)(_ => Task.FromResult(new Guid("6f9619ff-8b86-d011-b42d-00cf4fc964ff"))),
            new Guid("6f9619ff-8b86-d011-b42d-00cf4fc964ff")
        };
        yield return new object[]
        {
            typeof(DateTime),
            (Func<object[], object>)(_ => Task.FromResult(new DateTime(2026, 11, 25, 0, 0, 0, DateTimeKind.Utc))),
            new DateTime(2026, 11, 25, 0, 0, 0, DateTimeKind.Utc)
        };
    }

    [Theory]
    [MemberData(nameof(SupportedTaskOfValueTypeReturns))]
    public async Task Handle_WhenDynamicCodeIsNotSupported_AndMethodReturnsTaskOfSupportedValueType_InvokesWrapper(
        Type resultType, Func<object[], object> target, object expected)
    {
        RuntimeFeatureWrapper.SetIsDynamicCodeSupported(false);

        var result = Handle(target, typeof(Task<>).MakeGenericType(resultType));

        var task = Assert.IsAssignableFrom<Task>(result);
        await task;
        Assert.Equal(expected, task.GetType().GetProperty("Result")!.GetValue(task));
        Assert.Equal(resultType, _wrapper.WrappedType);
    }

    [Fact]
    public async Task Handle_WhenDynamicCodeIsNotSupported_AndMethodReturnsTask_InvokesWrapperAndAwaitsTarget()
    {
        RuntimeFeatureWrapper.SetIsDynamicCodeSupported(false);
        var completed = false;

        var result = Handle(_ => CompleteLater(), typeof(Task));

        await Assert.IsAssignableFrom<Task>(result);
        Assert.True(completed);
        Assert.True(_wrapper.Called);
        return;

        async Task CompleteLater()
        {
            await Task.Yield();
            completed = true;
        }
    }

    [Fact]
    public async Task Handle_WhenDynamicCodeIsNotSupported_AndMethodReturnsCompletedTask_InvokesWrapper()
    {
        RuntimeFeatureWrapper.SetIsDynamicCodeSupported(false);

        var result = Handle(_ => Task.CompletedTask, typeof(Task));

        await Assert.IsAssignableFrom<Task>(result);
        Assert.True(_wrapper.Called);
    }

    [Fact]
    public async Task Handle_WhenDynamicCodeIsNotSupported_AndMethodReturnsFaultedTask_PropagatesException()
    {
        RuntimeFeatureWrapper.SetIsDynamicCodeSupported(false);
        var expected = new InvalidOperationException("boom");

        var result = Handle(_ => Task.FromException(expected), typeof(Task));

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => Assert.IsAssignableFrom<Task>(result));
        Assert.Same(expected, actual);
        Assert.True(_wrapper.Called);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Handle_WithSeveralWrappers_RunsLastWrapperFirst(bool dynamicCodeSupported)
    {
        RuntimeFeatureWrapper.SetIsDynamicCodeSupported(dynamicCodeSupported);
        var log = new List<string>();
        // A distinct delegate per case, because handlers are cached per target method.
        Func<object[], object> target = dynamicCodeSupported ? _ => 1 : _ => 2;

        Handle(target, typeof(int), new OrderWrapperAttribute("first", log), new OrderWrapperAttribute("second", log));

        Assert.Equal(new[] { "second", "first" }, log);
    }

    [Fact]
    public async Task Handle_WithSeveralWrappers_AndMethodReturnsTask_RunsLastWrapperFirst()
    {
        var log = new List<string>();

        var result = Handle(_ => Task.CompletedTask, typeof(Task), new OrderWrapperAttribute("first", log),
            new OrderWrapperAttribute("second", log));

        await Assert.IsAssignableFrom<Task>(result);
        Assert.Equal(new[] { "second", "first" }, log);
    }

    private object Handle(Func<object[], object> target, Type returnType)
    {
        return Handle(target, returnType, _wrapper);
    }

    private object Handle(Func<object[], object> target, Type returnType, params Attribute[] triggers)
    {
        var method = typeof(UniversalWrapperAspectTests).GetMethod(nameof(SampleMethod),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return _aspect.Handle(this, GetType(), method, target, nameof(SampleMethod), Array.Empty<object>(),
            returnType, triggers);
    }

    private static void SampleMethod()
    {
    }

    private readonly record struct UserStruct(int Value);

    private sealed class RecordingWrapperAttribute : UniversalWrapperAttribute
    {
        public bool Called { get; private set; }

        /// <summary>The <c>T</c> that WrapSync/WrapAsync was called with.</summary>
        public Type WrappedType { get; private set; }

        protected internal override T WrapSync<T>(Func<object[], T> target, object[] args, AspectEventArgs eventArgs)
        {
            Called = true;
            WrappedType = typeof(T);
            return base.WrapSync(target, args, eventArgs);
        }

        protected internal override Task<T> WrapAsync<T>(Func<object[], Task<T>> target, object[] args,
            AspectEventArgs eventArgs)
        {
            Called = true;
            WrappedType = typeof(T);
            return base.WrapAsync(target, args, eventArgs);
        }
    }

    private sealed class OrderWrapperAttribute(string name, List<string> log) : UniversalWrapperAttribute
    {
        protected internal override T WrapSync<T>(Func<object[], T> target, object[] args, AspectEventArgs eventArgs)
        {
            log.Add(name);
            return base.WrapSync(target, args, eventArgs);
        }

        protected internal override Task<T> WrapAsync<T>(Func<object[], Task<T>> target, object[] args,
            AspectEventArgs eventArgs)
        {
            log.Add(name);
            return base.WrapAsync(target, args, eventArgs);
        }
    }
}
