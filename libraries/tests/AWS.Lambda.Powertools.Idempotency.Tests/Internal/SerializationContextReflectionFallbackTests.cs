using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using AWS.Lambda.Powertools.Common.Utils;
using AWS.Lambda.Powertools.Idempotency.Exceptions;
using AWS.Lambda.Powertools.Idempotency.Internal.Serializers;
using AWS.Lambda.Powertools.Idempotency.Persistence;
using AWS.Lambda.Powertools.Idempotency.Tests.Model;
using Xunit;

namespace AWS.Lambda.Powertools.Idempotency.Tests.Internal;

/// <summary>
///     <see cref="Idempotency.IdempotencyBuilder.WithJsonSerializationContext" /> adds metadata from the user's context.
///     When reflection-based serialization is available (JIT), types the context doesn't know must keep working.
/// </summary>
[Collection("Sequential")]
public class SerializationContextReflectionFallbackTests : IDisposable
{
    public void Dispose()
    {
        RuntimeFeatureWrapper.Reset();
        IdempotencySerializer.AddTypeInfoResolver(TestJsonSerializerContext.Default);
    }

    [Fact]
    public void AddTypeInfoResolver_WhenReflectionIsAvailable_TypesNotInContextStillRoundTrip()
    {
        IdempotencySerializer.AddTypeInfoResolver(TestJsonSerializerContext.Default);

        Assert.Equal("\"hello\"", IdempotencySerializer.Serialize("hello", typeof(string)));
        Assert.Equal("hello", IdempotencySerializer.Deserialize<string>("\"hello\""));

        var json = IdempotencySerializer.Serialize(new NotInContext { Value = "x" }, typeof(NotInContext));
        Assert.Equal("{\"Value\":\"x\"}", json);
        Assert.Equal("x", IdempotencySerializer.Deserialize<NotInContext>(json).Value);
    }

    [Fact]
    public void AddTypeInfoResolver_WhenReflectionIsAvailable_TypesInContextStillRoundTrip()
    {
        IdempotencySerializer.AddTypeInfoResolver(TestJsonSerializerContext.Default);

        var json = IdempotencySerializer.Serialize(new TestClass { Id = 1, Name = "Test" }, typeof(TestClass));
        var result = IdempotencySerializer.Deserialize<TestClass>(json);

        Assert.Equal(1, result.Id);
        Assert.Equal("Test", result.Name);
    }

    [Fact]
    public void AddTypeInfoResolver_WhenReflectionIsAvailable_UserContextComesBeforeReflection()
    {
        IdempotencySerializer.AddTypeInfoResolver(TestJsonSerializerContext.Default);

        var chain = GetOptions().TypeInfoResolverChain;
        Assert.Same(TestJsonSerializerContext.Default, chain.First());
        Assert.IsType<DefaultJsonTypeInfoResolver>(chain.Last());
    }

    [Fact]
    public void AddTypeInfoResolver_WhenDynamicCodeIsNotSupported_DoesNotFallBackToReflection()
    {
        RuntimeFeatureWrapper.SetIsDynamicCodeSupported(false);

        IdempotencySerializer.AddTypeInfoResolver(TestJsonSerializerContext.Default);

        Assert.DoesNotContain(GetOptions().TypeInfoResolverChain, r => r is DefaultJsonTypeInfoResolver);
        Assert.Throws<SerializationException>(() => IdempotencySerializer.GetTypeInfo(typeof(NotInContext)));
    }

    [Fact]
    public void Idempotent_WithJsonSerializationContext_WhenResultTypeIsNotInContext_ReplaysStoredResult()
    {
        Idempotency.Configure(builder => builder
            .WithJsonSerializationContext(TestJsonSerializerContext.Default)
            .WithPersistenceStore(new ConflictingInMemoryStore()));
        var handlers = new Handlers();

        var first = handlers.Handle("payload");
        var second = handlers.Handle("payload");

        Assert.Equal("payload-1", first.Value);
        Assert.Equal("payload-1", second.Value);
        Assert.Equal(1, handlers.Executions);
    }

    private static JsonSerializerOptions GetOptions()
    {
        var field = typeof(IdempotencySerializer).GetField("_jsonOptions",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return (JsonSerializerOptions)field!.GetValue(null)!;
    }

    public class NotInContext
    {
        public string Value { get; set; }
    }

    public class Handlers
    {
        public int Executions { get; private set; }

        [Idempotent]
        public NotInContext Handle(string payload)
        {
            Executions++;
            return new NotInContext { Value = $"{payload}-{Executions}" };
        }
    }

    /// <summary>
    ///     Throws <see cref="IdempotencyItemAlreadyExistsException" /> for an existing record, like the real stores,
    ///     so the second call is replayed.
    /// </summary>
    private sealed class ConflictingInMemoryStore : BasePersistenceStore
    {
        private readonly ConcurrentDictionary<string, DataRecord> _records = new();

        public override Task<DataRecord> GetRecord(string idempotencyKey)
        {
            return _records.TryGetValue(idempotencyKey, out var record)
                ? Task.FromResult(record)
                : throw new IdempotencyItemNotFoundException(idempotencyKey);
        }

        public override Task PutRecord(DataRecord record, DateTimeOffset now)
        {
            if (_records.TryGetValue(record.IdempotencyKey, out var existing) && !existing.IsExpired(now))
                throw new IdempotencyItemAlreadyExistsException("Record exists") { Record = existing };

            _records[record.IdempotencyKey] = record;
            return Task.CompletedTask;
        }

        public override Task UpdateRecord(DataRecord record)
        {
            _records[record.IdempotencyKey] = record;
            return Task.CompletedTask;
        }

        public override Task DeleteRecord(string idempotencyKey)
        {
            _records.TryRemove(idempotencyKey, out _);
            return Task.CompletedTask;
        }
    }
}
