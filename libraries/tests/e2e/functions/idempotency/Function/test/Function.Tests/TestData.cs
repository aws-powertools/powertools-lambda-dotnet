namespace Function.Tests;

public static class TestData
{
    public static IEnumerable<object[]> Inline =>
        new List<object[]>
        {
            new object[] { "E2ETestLambda_X64_NET6_idempotency", "IdempotencyTable" },
            new object[] { "E2ETestLambda_ARM_NET6_idempotency", "IdempotencyTable" },
            new object[] { "E2ETestLambda_X64_NET8_idempotency", "IdempotencyTable" },
            new object[] { "E2ETestLambda_ARM_NET8_idempotency", "IdempotencyTable" }
        };
}

public static class TestDataAot
{
    public static IEnumerable<object[]> Inline =>
        new List<object[]>
        {
            new object[] { "E2ETestLambda_X64_AOT_NET8_idempotency", "IdempotencyTable-AOT-x86_64" },
            new object[] { "E2ETestLambda_ARM_AOT_NET8_idempotency", "IdempotencyTable-AOT-arm64" }
        };
}