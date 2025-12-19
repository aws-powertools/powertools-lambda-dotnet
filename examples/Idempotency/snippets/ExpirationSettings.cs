// This file is referenced by docs/utilities/idempotency.md
// via pymdownx.snippets (mkdocs).

// --8<-- [start:customizing_expiration_time]
new IdempotencyOptionsBuilder()
    .WithExpiration(TimeSpan.FromMinutes(5))
    .Build()
// --8<-- [end:customizing_expiration_time]
