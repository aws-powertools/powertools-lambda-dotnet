using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using AWS.Lambda.Powertools.Common.Utils;
using AWS.Lambda.Powertools.Idempotency.Exceptions;
using AWS.Lambda.Powertools.Idempotency.Internal.Serializers;
using AWS.Lambda.Powertools.Idempotency.Persistence;
using AWS.Lambda.Powertools.Idempotency.Tests.Model;
using Xunit;

namespace AWS.Lambda.Powertools.Idempotency.Tests.Internal;

/// <summary>
///     [Idempotent] methods that return value types or a plain Task, with Native AOT simulated through
///     <see cref="RuntimeFeatureWrapper" /> (source-generated serialization, no MakeGenericMethod over value types).
/// </summary>
[Collection("Sequential")]
public class IdempotentValueTypeReturnTests : IDisposable
{
    private static readonly Guid SampleGuid = new("6f9619ff-8b86-d011-b42d-00cf4fc964ff");
    private static readonly DateTime SampleDateTime = new(2026, 11, 25, 10, 30, 0, DateTimeKind.Utc);

    private readonly ValueTypeFunctions _functions = new();

    public IdempotentValueTypeReturnTests()
    {
        RuntimeFeatureWrapper.SetIsDynamicCodeSupported(false);
        // The user context doesn't register any primitive: they must work without it.
        Idempotency.Configure(builder => builder
            .WithJsonSerializationContext(TestJsonSerializerContext.Default)
            .WithPersistenceStore(new ConflictingInMemoryStore()));
    }

    public void Dispose()
    {
        RuntimeFeatureWrapper.Reset();
        IdempotencySerializer.AddTypeInfoResolver(TestJsonSerializerContext.Default);
    }

    public static IEnumerable<object[]> ValueTypeCalls()
    {
        yield return new object[] { "int", (Func<ValueTypeFunctions, string, object>)((f, p) => f.Int(p)), 42 };
        yield return new object[] { "long", (Func<ValueTypeFunctions, string, object>)((f, p) => f.Long(p)), 42L << 33 };
        yield return new object[] { "bool", (Func<ValueTypeFunctions, string, object>)((f, p) => f.Bool(p)), true };
        yield return new object[] { "double", (Func<ValueTypeFunctions, string, object>)((f, p) => f.Double(p)), 1.5 };
        yield return new object[] { "decimal", (Func<ValueTypeFunctions, string, object>)((f, p) => f.Decimal(p)), 12.34m };
        yield return new object[] { "Guid", (Func<ValueTypeFunctions, string, object>)((f, p) => f.Guid(p)), SampleGuid };
        yield return new object[]
            { "DateTime", (Func<ValueTypeFunctions, string, object>)((f, p) => f.DateTime(p)), SampleDateTime };
        yield return new object[]
            { "Task<int>", (Func<ValueTypeFunctions, string, object>)((f, p) => f.TaskInt(p).GetAwaiter().GetResult()), 7 };
        yield return new object[]
        {
            "Task<decimal>",
            (Func<ValueTypeFunctions, string, object>)((f, p) => f.TaskDecimal(p).GetAwaiter().GetResult()), 56.78m
        };
        yield return new object[]
        {
            "Task<DateTime>",
            (Func<ValueTypeFunctions, string, object>)((f, p) => f.TaskDateTime(p).GetAwaiter().GetResult()),
            SampleDateTime
        };
    }

    [Theory]
    [MemberData(nameof(ValueTypeCalls))]
    public void Handle_WhenDynamicCodeIsNotSupported_AndSecondCall_ReturnsStoredValueType(string returnType,
        Func<ValueTypeFunctions, string, object> call, object expected)
    {
        var payload = $"payload-{returnType}";

        var first = call(_functions, payload);
        var second = call(_functions, payload);

        Assert.Equal(expected, first);
        Assert.Equal(expected, second);
        Assert.Equal(1, _functions.Executions);
    }

    [Fact]
    public async Task Handle_WhenDynamicCodeIsNotSupported_AndMethodReturnsTask_SecondCallIsServedFromStore()
    {
        await _functions.Task("payload-task");
        await _functions.Task("payload-task");

        Assert.Equal(1, _functions.Executions);
    }

    [Fact]
    public async Task Handle_WhenMethodReturnsTask_StoresSameResponseAsEarlierVersions()
    {
        // Earlier versions wrapped a plain Task as Task<VoidTaskResult>, stored as "{}". Storing the same
        // response keeps records readable by both earlier and later versions.
        var store = new ConflictingInMemoryStore();
        Idempotency.Configure(builder => builder
            .WithJsonSerializationContext(TestJsonSerializerContext.Default)
            .WithPersistenceStore(store));

        await _functions.Task("payload-stored-response");

        Assert.Equal("{}", store.SingleResponseData());
    }

    [Fact]
    public void Handle_WhenDynamicCodeIsNotSupported_AndMethodReturnsUserStruct_ThrowsNotSupported()
    {
        var ex = Assert.Throws<NotSupportedException>(() => _functions.Struct("payload-struct"));

        Assert.Contains($"'{nameof(ValueTypeFunctions)}.{nameof(ValueTypeFunctions.Struct)}'", ex.Message);
        Assert.Contains("'UserStruct'", ex.Message);
        Assert.Contains(nameof(IdempotentAttribute), ex.Message);
        Assert.Equal(0, _functions.Executions);
    }

    public readonly record struct UserStruct(int Value);

    public class ValueTypeFunctions
    {
        public int Executions { get; private set; }

        [Idempotent]
        public int Int(string p) => Run(42);

        [Idempotent]
        public long Long(string p) => Run(42L << 33);

        [Idempotent]
        public bool Bool(string p) => Run(true);

        [Idempotent]
        public double Double(string p) => Run(1.5);

        [Idempotent]
        public decimal Decimal(string p) => Run(12.34m);

        [Idempotent]
        public Guid Guid(string p) => Run(SampleGuid);

        [Idempotent]
        public DateTime DateTime(string p) => Run(SampleDateTime);

        [Idempotent]
        public UserStruct Struct(string p) => Run(new UserStruct(3));

        [Idempotent]
        public async Task<int> TaskInt(string p)
        {
            await System.Threading.Tasks.Task.Yield();
            return Run(7);
        }

        [Idempotent]
        public async Task<decimal> TaskDecimal(string p)
        {
            await System.Threading.Tasks.Task.Yield();
            return Run(56.78m);
        }

        [Idempotent]
        public async Task<DateTime> TaskDateTime(string p)
        {
            await System.Threading.Tasks.Task.Yield();
            return Run(SampleDateTime);
        }

        [Idempotent]
        public async Task Task(string p)
        {
            await System.Threading.Tasks.Task.Yield();
            Executions++;
        }

        private T Run<T>(T value)
        {
            Executions++;
            return value;
        }
    }

    /// <summary>
    ///     In-memory store that, like DynamoDB, rejects a second in-progress record for the same key.
    /// </summary>
    private sealed class ConflictingInMemoryStore : BasePersistenceStore
    {
        private readonly ConcurrentDictionary<string, DataRecord> _records = new();

        public string SingleResponseData() => Assert.Single(_records).Value.ResponseData;

        public override Task<DataRecord> GetRecord(string idempotencyKey)
        {
            return _records.TryGetValue(idempotencyKey, out var record)
                ? System.Threading.Tasks.Task.FromResult(record)
                : throw new IdempotencyItemNotFoundException(idempotencyKey);
        }

        public override Task PutRecord(DataRecord record, DateTimeOffset now)
        {
            if (_records.TryGetValue(record.IdempotencyKey, out var existing) && !existing.IsExpired(now))
                throw new IdempotencyItemAlreadyExistsException("Record exists") { Record = existing };

            _records[record.IdempotencyKey] = record;
            return System.Threading.Tasks.Task.CompletedTask;
        }

        public override Task UpdateRecord(DataRecord record)
        {
            _records[record.IdempotencyKey] = record;
            return System.Threading.Tasks.Task.CompletedTask;
        }

        public override Task DeleteRecord(string idempotencyKey)
        {
            _records.TryRemove(idempotencyKey, out _);
            return System.Threading.Tasks.Task.CompletedTask;
        }
    }
}
