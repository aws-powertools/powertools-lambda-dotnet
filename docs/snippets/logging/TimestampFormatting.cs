// This file is referenced by docs/core/logging.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.Logging;

// --8<-- [start:timestamp_formatting]
Logger.Configure(logger =>
{
    logger.TimestampFormat = "yyyy-MM-dd HH:mm:ss";
});
// --8<-- [end:timestamp_formatting]
