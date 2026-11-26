# Powertools for AWS Lambda (.NET) - Data Masking Example

This example shows how to use the [Data Masking](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/1257)
utility to irreversibly **erase** sensitive fields (PII) from a payload before it is logged or returned,
while keeping the non-sensitive fields readable.

This example focuses on the `Erase` operation and its integration with Powertools Logging. For
reversible `Encrypt`/`Decrypt` with AWS KMS, see the utility README.

## How it works

The function receives a JSON order that contains PII (`ssn`, `creditCard`, `email`, `phone`, `street`)
and applies three masking styles:

- **Full mask** (default `*****`) for `customer.ssn`, `payment.creditCard`, and `address.street`
- **Regex substitution** for `customer.email` (keeps the first character: `j****@example.com`)
- **Length-preserving mask** for `customer.phone` (`555-0134` -> `********`)

The result keeps `orderId`, `item`, `payment.amount`, and `address.city` readable, so the payload
remains useful for debugging and monitoring. `LogEvent` is intentionally disabled so the raw PII is
never auto-logged.

**Logging integration:** the handler logs the masked payload with
`Logger.LogInformation(_dataMasking.EraseToNode(...))`. `EraseToNode` returns a `JsonNode`, so the
masked object is logged as structured JSON with no extra string round-trip.

## Prerequisites

- .NET 8.0 - [Install .NET 8.0](https://www.microsoft.com/net/download)
- [AWS SAM CLI](https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/install-sam-cli.html)

## Run locally

Build and invoke the function with the sample event:

```bash
sam build
sam local invoke HelloWorldFunction --event events/event.json
```

Expected (abbreviated) response body:

```json
{
  "orderId": "ORD-1001",
  "item": "Powertools T-Shirt",
  "customer": {
    "name": "John Doe",
    "email": "j****@example.com",
    "phone": "********",
    "ssn": "*****"
  },
  "address": { "street": "*****", "city": "Anytown" },
  "payment": { "creditCard": "*****", "amount": 42.5 }
}
```

## Deploy

```bash
sam build
sam deploy --guided
```
