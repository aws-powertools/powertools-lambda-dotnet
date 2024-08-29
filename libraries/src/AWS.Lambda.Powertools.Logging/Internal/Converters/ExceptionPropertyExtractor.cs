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
    
    // Note: Leave code comments for future reference
    
    /// <summary>
    ///     The property extractors
    /// </summary>
    private static readonly Dictionary<Type, Func<Exception, IEnumerable<KeyValuePair<string, object>>>> PropertyExtractors = new()
    {
        { typeof(Exception), GetBaseExceptionProperties },
        // Add more specific exception types here
        // { typeof(ArgumentException), GetArgumentExceptionProperties },
        // { typeof(InvalidOperationException), GetInvalidOperationExceptionProperties },
        // { typeof(NullReferenceException), GetBaseExceptionProperties },
        // ... add more as needed
    };

    /// <summary>
    /// Use this method to extract properties from and Exception based type
    /// This method is used when building for native AOT    
    /// </summary>
    /// <param name="exception"></param>
    /// <returns></returns>
    public static IEnumerable<KeyValuePair<string, object>> ExtractProperties(Exception exception)
    {
        // var exceptionType = exception.GetType();
        
        // if (PropertyExtractors.TryGetValue(exceptionType, out var extractor))
        // {
        //     return extractor(exception);
        // }

        // If we don't have a specific extractor, use the base Exception extractor
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
        // Add any other properties you want to extract
    }

    // private static IEnumerable<KeyValuePair<string, object>> GetArgumentExceptionProperties(Exception ex)
    // {
    //     var argEx = (ArgumentException)ex;
    //     foreach (var prop in GetBaseExceptionProperties(ex))
    //     {
    //         yield return prop;
    //     }
    //     yield return new KeyValuePair<string, object>(nameof(ArgumentException.ParamName), argEx.ParamName);
    // }
    //
    // private static IEnumerable<KeyValuePair<string, object>> GetInvalidOperationExceptionProperties(Exception ex)
    // {
    //     // InvalidOperationException doesn't have any additional properties
    //     // but we include it here as an example
    //     return GetBaseExceptionProperties(ex);
    // }
}