using System;
using System.Collections.Generic;

namespace AWS.Lambda.Powertools.Logging.Internal.Converters;

/// <summary>
///     Class ExceptionPropertyExtractor.
///     This class is used to extract properties from an exception object.
///     It uses a dictionary of type to function mappings to extract specific properties based on the exception type.
///     If no specific extractor is found, it falls back to the base Exception extractor.
/// </summary>
internal static class ExceptionPropertyExtractor
{
    /// <summary>
    ///     The property extractors
    /// </summary>
    private static readonly Dictionary<Type, Func<Exception, IEnumerable<KeyValuePair<string, object>>>> PropertyExtractors = new()
    {
        { typeof(Exception), GetBaseExceptionProperties },
    };

    /// <summary>
    /// Use this method to extract properties from and Exception based type
    /// This method is used when building for native AOT    
    /// </summary>
    /// <param name="exception"></param>
    /// <returns></returns>
    public static IEnumerable<KeyValuePair<string, object>> ExtractProperties(Exception exception)
    {
        return GetBaseExceptionProperties(exception);
    }

    /// <summary>
    /// Get the base Exception properties
    /// </summary>
    /// <param name="ex"></param>
    /// <returns></returns>
    private static IEnumerable<KeyValuePair<string, object>> GetBaseExceptionProperties(Exception ex)
    {
        yield return new KeyValuePair<string, object>(nameof(Exception.Message), ex.Message);
        yield return new KeyValuePair<string, object>(nameof(Exception.Source), ex.Source);
        yield return new KeyValuePair<string, object>(nameof(Exception.StackTrace), ex.StackTrace);
    }
}