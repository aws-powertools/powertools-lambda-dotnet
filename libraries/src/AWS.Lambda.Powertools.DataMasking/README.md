# Powertools for AWS Lambda (.NET) - Data Masking

The Data Masking utility protects sensitive data (PII, credentials, financial records) before it is
logged, stored, or forwarded to another service, helping to meet requirements such as GDPR, HIPAA,
and PCI-DSS.

> **Status:** this package ships **`Erase`** (irreversible masking) and **`Encrypt`/`Decrypt`**
> (reversible, via a pluggable provider using the AWS Encryption SDK + KMS). Logging integration and
> docs parity with other runtimes are planned next.
> See [issue #1257](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/1257).

## Key features

- Irreversibly **erase** sensitive fields while keeping the surrounding payload structure intact
- **Encrypt/decrypt** whole payloads or specific fields with AWS KMS envelope encryption
- Pluggable encryption provider (`IDataMaskingProvider`); default is `AwsEncryptionSdkProvider`
- Select fields with simple dotted paths (for example `address.street`)
- Customizable masking: fixed string, length-preserving mask, or regex + replacement
- `Erase` over JSON strings or `JsonNode` is trimming/Native AOT friendly

## Erase

```csharp
using AWS.Lambda.Powertools.DataMasking;

var masker = new DataMasking();

string json = """
{
  "email": "john@example.com",
  "address": { "street": "123 Main St", "city": "Anytown" },
  "customer": { "ssn": "123-45-6789" }
}
""";

// Mask specific fields; the rest of the payload stays readable
string masked = masker.Erase(json, fields: new[] { "email", "address.street", "customer.ssn" });
// {
//   "email": "*****",
//   "address": { "street": "*****", "city": "Anytown" },
//   "customer": { "ssn": "*****" }
// }
```

### Custom masking

```csharp
// Length-preserving mask: "secret" -> "******"
masker.Erase(json, new[] { "password" }, new MaskingOptions { PreserveLength = true });

// Fixed replacement string
masker.Erase(json, new[] { "token" }, new MaskingOptions { MaskValue = "[REDACTED]" });

// Regex + replacement (keeps the first character of an email local part)
masker.Erase(json, new[] { "email" }, new MaskingOptions
{
    Pattern = new System.Text.RegularExpressions.Regex(@"^(.).*@"),
    Replacement = "$1****@"
});
```

## Encrypt / Decrypt

Unlike `Erase`, encryption is **reversible**. Construct `DataMasking` with an `IDataMaskingProvider`.
The default provider, `AwsEncryptionSdkProvider`, performs AWS KMS envelope encryption.

```csharp
using AWS.Lambda.Powertools.DataMasking;
using AWS.Lambda.Powertools.DataMasking.Providers;

var provider = new AwsEncryptionSdkProvider(KMS_KEY_ARN);
var masker = new DataMasking(provider);

// Encrypt only specific fields; the rest of the payload stays readable
var encrypted = await masker.EncryptAsync(json,
    fields: new[] { "customer.ssn", "payment.creditCard" },
    encryptionContext: new Dictionary<string, string> { ["tenantId"] = "acme-corp" });

// Decrypt restores the original values (encryption context is validated)
var decrypted = await masker.DecryptAsync(encrypted,
    fields: new[] { "customer.ssn", "payment.creditCard" },
    encryptionContext: new Dictionary<string, string> { ["tenantId"] = "acme-corp" });
```

You can also encrypt/decrypt the whole payload by omitting `fields`. The `encryptionContext` is bound
to the ciphertext at encrypt time and validated on decrypt, so a mismatched context fails.

## Logging integration

To redact sensitive fields before they reach your logs, mask the payload and log the result.
`EraseToNode` returns a `JsonNode` that Powertools Logging serializes directly, avoiding an extra
string round-trip:

```csharp
using AWS.Lambda.Powertools.DataMasking;
using AWS.Lambda.Powertools.Logging;

var masker = new DataMasking();

Logger.LogInformation(masker.EraseToNode(json, new[] { "customer.ssn", "payment.creditCard" }));
```

Data Masking and Logging stay independent packages: the integration is just this pattern, so you can
mask before logging without coupling the two utilities.

### AOT / trimming

The `Erase(string, ...)`, `Erase(JsonNode, ...)`, and `EraseToNode(...)` overloads are fully
trimming/AOT safe. The `Erase(object, ...)` overload uses reflection-based serialization and is
annotated accordingly; for AOT scenarios use the overload that accepts a source-generated
`JsonTypeInfo`.

The `AwsEncryptionSdkProvider` relies on the AWS Encryption SDK for .NET (Dafny-based runtime), which
uses reflection and is therefore **not** compatible with trimming/Native AOT. It is annotated with
`[RequiresUnreferencedCode]`/`[RequiresDynamicCode]`. If you need AOT, supply a custom
`IDataMaskingProvider` implementation that is AOT-compatible.

The `AwsEncryptionSdkProvider` relies on the AWS Encryption SDK for .NET (Dafny-based runtime), which
uses reflection and is therefore **not** compatible with trimming/Native AOT. It is annotated with
`[RequiresUnreferencedCode]`/`[RequiresDynamicCode]`. If you need AOT, supply a custom
`IDataMaskingProvider` implementation that is AOT-compatible.
