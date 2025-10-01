using System;
using System.Diagnostics.CodeAnalysis;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Internal.Emitters;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using Amazon.XRay.Recorder.Core.Strategies;
using AWS.Lambda.Powertools.Common;

namespace AWS.Lambda.Powertools.Tracing.Internal;

/// <summary>
///     Class XRayRecorder.
///     Implements the <see cref="IXRayRecorder" />
/// </summary>
/// <seealso cref="IXRayRecorder" />
internal class XRayRecorder : IXRayRecorder
{
    /// <summary>
    ///     Maximum recursion depth for sanitization to prevent infinite loops
    /// </summary>
    private const int MaxSanitizationDepth = 10;

    /// <summary>
    ///     The instance
    /// </summary>
    private static IXRayRecorder _instance;

    /// <summary>
    ///     The AWS X-Ray recorder instance
    /// </summary>
    private readonly IAWSXRayRecorder _awsxRayRecorder;

    /// <summary>
    ///     Gets the instance.
    /// </summary>
    /// <value>The instance.</value>
    public static IXRayRecorder Instance =>
        _instance ??= new XRayRecorder(AWSXRayRecorder.Instance, PowertoolsConfigurations.Instance);

    public XRayRecorder(IAWSXRayRecorder awsxRayRecorder, IPowertoolsConfigurations powertoolsConfigurations)
    {
        _instance = this;
        _isLambda = powertoolsConfigurations.IsLambdaEnvironment;
        _awsxRayRecorder = awsxRayRecorder;
    }

    /// <summary>
    /// Resets the singleton instance. This method is intended for testing purposes only.
    /// </summary>
    internal static void ResetInstance()
    {
        _instance = null;
    }

    /// <summary>
    ///     Checks whether current execution is in AWS Lambda.
    /// </summary>
    /// <returns>Returns true if current execution is in AWS Lambda.</returns>
    private readonly bool _isLambda;

    /// <summary>
    ///     Gets the emitter.
    /// </summary>
    /// <value>The emitter.</value>
    public ISegmentEmitter Emitter => _isLambda ? _awsxRayRecorder.Emitter : null;

    /// <summary>
    ///     Gets the streaming strategy.
    /// </summary>
    /// <value>The streaming strategy.</value>
    public IStreamingStrategy StreamingStrategy => _isLambda ? _awsxRayRecorder.StreamingStrategy : null;

    /// <summary>
    ///     Begins the subsegment.
    /// </summary>
    /// <param name="name">The name.</param>
    public void BeginSubsegment(string name)
    {
        if (_isLambda)
        {
            _awsxRayRecorder.BeginSubsegment(Helpers.SanitizeString(name));
        }
    }

    /// <summary>
    ///     Sets the namespace.
    /// </summary>
    /// <param name="value">The value.</param>
    public void SetNamespace(string value)
    {
        if (_isLambda)
            _awsxRayRecorder.SetNamespace(value);
    }

    /// <summary>
    ///     Adds the annotation.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    public void AddAnnotation(string key, object value)
    {
        if (_isLambda)
        {
            var sanitizedValue = SanitizeValueForAnnotation(value);
            _awsxRayRecorder.AddAnnotation(key, sanitizedValue);
        }
    }

    /// <summary>
    ///     Adds the metadata.
    /// </summary>
    /// <param name="nameSpace">The name space.</param>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    public void AddMetadata(string nameSpace, string key, object value)
    {
        if (_isLambda)
        {
            var sanitizedValue = SanitizeValueForMetadata(value);
            _awsxRayRecorder.AddMetadata(nameSpace, key, sanitizedValue);
        }
    }

    /// <summary>
    ///     Ends the subsegment.
    /// </summary>
    public void EndSubsegment()
    {
        if (!_isLambda) return;

        try
        {
            // First attempt: Sanitize the entire entity before ending the subsegment
            SanitizeCurrentEntitySafely();
            _awsxRayRecorder.EndSubsegment();
        }
        catch (Exception e) when (IsSerializationError(e))
        {
            // This is a JSON serialization error - handle it aggressively
            Console.WriteLine("JSON serialization error detected in Tracing utility - attempting recovery");
            Console.WriteLine($"Error: {e.Message}");

            HandleSerializationError(e);
        }
        catch (Exception e)
        {
            // Handle other types of errors with the original logic
            Console.WriteLine("Error in Tracing utility - see Exceptions tab in Cloudwatch Traces");
            Console.WriteLine(e.StackTrace);

            try
            {
                _awsxRayRecorder.TraceContext.ClearEntity();
                _awsxRayRecorder.BeginSubsegment("Error in Tracing utility - see Exceptions tab");
                _awsxRayRecorder.AddException(e);
                _awsxRayRecorder.MarkError();
                _awsxRayRecorder.EndSubsegment();
            }
            catch
            {
                // If even error handling fails, give up gracefully
                Console.WriteLine("Failed to handle tracing error");
            }
        }
    }

    /// <summary>
    /// Determines if an exception is related to JSON serialization
    /// </summary>
    private static bool IsSerializationError(Exception e)
    {
        if (e == null) return false;

        var message = e.Message ?? string.Empty;
        var stackTrace = e.StackTrace ?? string.Empty;
        var typeName = e.GetType().Name;

        return message.Contains("LitJson") ||
               message.Contains("JsonMapper") ||
               stackTrace.Contains("JsonMapper") ||
               stackTrace.Contains("LitJson") ||
               stackTrace.Contains("JsonSegmentMarshaller") ||
               typeName.Contains("Json");
    }

    /// <summary>
    /// Handles serialization errors by progressively trying different recovery strategies
    /// </summary>
    private void HandleSerializationError(Exception originalException)
    {
        try
        {
            // Strategy 1: Try to clear and recreate with minimal data
            Console.WriteLine("Attempting serialization error recovery - Strategy 1: Clear and recreate");

            _awsxRayRecorder.TraceContext.ClearEntity();
            _awsxRayRecorder.BeginSubsegment("Tracing_Sanitized");
            _awsxRayRecorder.AddAnnotation("SerializationError", true);
            _awsxRayRecorder.AddMetadata("Error", "Type", "JSON Serialization Error");
            _awsxRayRecorder.AddMetadata("Error", "Message", SanitizeValueForMetadata(originalException.Message));
            _awsxRayRecorder.EndSubsegment();

            Console.WriteLine("Serialization error recovery successful");
        }
        catch (Exception e2)
        {
            try
            {
                // Strategy 2: Even more minimal approach
                Console.WriteLine("Strategy 1 failed, attempting Strategy 2: Minimal segment");

                _awsxRayRecorder.TraceContext.ClearEntity();
                _awsxRayRecorder.BeginSubsegment("Tracing_Error");
                _awsxRayRecorder.AddAnnotation("Error", "SerializationFailed");
                _awsxRayRecorder.EndSubsegment();

                Console.WriteLine("Minimal serialization error recovery successful");
            }
            catch (Exception e3)
            {
                // Strategy 3: Complete failure - just log and give up
                Console.WriteLine("All serialization error recovery strategies failed");
                Console.WriteLine($"Original error: {originalException.Message}");
                Console.WriteLine($"Recovery error 1: {e2.Message}");
                Console.WriteLine($"Recovery error 2: {e3.Message}");

                // Try one last time to clear the entity to prevent further issues
                try
                {
                    _awsxRayRecorder.TraceContext.ClearEntity();
                }
                catch
                {
                    // If we can't even clear, there's nothing more we can do
                    Console.WriteLine("Failed to clear X-Ray entity - tracing may be in an inconsistent state");
                }
            }
        }
    }

    /// <summary>
    ///     Gets the entity.
    /// </summary>
    /// <returns>Entity.</returns>
    public Entity GetEntity()
    {
        if (_isLambda)
        {
            try
            {
                return _awsxRayRecorder?.TraceContext?.GetEntity() ?? new Subsegment("Root");
            }
            catch
            {
                // If we can't get the entity from X-Ray context, fall back to a root subsegment
                return new Subsegment("Root");
            }
        }

        return new Subsegment("Root");
    }

    /// <summary>
    ///     Sets the entity.
    /// </summary>
    /// <param name="entity">The entity.</param>
    public void SetEntity(Entity entity)
    {
        if (_isLambda)
            _awsxRayRecorder.TraceContext.SetEntity(entity);
    }

    /// <summary>
    ///     Adds the exception.
    /// </summary>
    /// <param name="exception">The exception.</param>
    public void AddException(Exception exception)
    {
        if (_isLambda)
        {
            // Sanitize exception data if it contains problematic types
            try
            {
                _awsxRayRecorder.AddException(exception);
            }
            catch (Exception ex) when (ex.Message.Contains("LitJson") || ex.Message.Contains("JsonMapper"))
            {
                // If the exception itself causes serialization issues, create a sanitized version
                var sanitizedException =
                    new Exception($"[Sanitized Exception] {exception.GetType().Name}: {exception.Message}");
                _awsxRayRecorder.AddException(sanitizedException);
            }
        }
    }

    /// <summary>
    ///     Adds the HTTP information.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    public void AddHttpInformation(string key, object value)
    {
        if (_isLambda)
        {
            var sanitizedValue = SanitizeValueForMetadata(value);
            _awsxRayRecorder.AddHttpInformation(key, sanitizedValue);
        }
    }

    /// <summary>
    ///     Sanitizes annotation values to ensure they are supported by X-Ray.
    ///     X-Ray annotations only support: string, int, long, double, float, bool
    /// </summary>
    /// <param name="value">The value to sanitize</param>
    /// <returns>A sanitized value safe for X-Ray annotations</returns>
    private static object SanitizeValueForAnnotation(object value)
    {
        if (value == null)
            return null;

        var type = value.GetType();

        // X-Ray supported annotation types: string, int, long, double, float, bool
        if (type == typeof(string) ||
            type == typeof(int) ||
            type == typeof(long) ||
            type == typeof(double) ||
            type == typeof(float) ||
            type == typeof(bool))
        {
            return value;
        }

        // Convert all other types to string
        return value.ToString();
    }

    /// <summary>
    ///     Sanitizes metadata values to ensure they can be serialized by X-Ray.
    ///     This method recursively processes complex objects to handle problematic types.
    /// </summary>
    /// <param name="value">The value to sanitize</param>
    /// <returns>A sanitized value safe for X-Ray metadata serialization</returns>
    private static object SanitizeValueForMetadata(object value)
    {
        try
        {
            return SanitizeValueRecursive(value, 0);
        }
        catch (Exception ex)
        {
            // If sanitization fails, return a safe string representation
            // This ensures we don't break the tracing functionality
            return $"[Sanitization failed: {ex.Message}] {value.ToString()}";
        }
    }

    /// <summary>
    ///     Recursively sanitizes values with depth protection to prevent infinite recursion.
    /// </summary>
    /// <param name="value">The value to sanitize</param>
    /// <param name="depth">Current recursion depth</param>
    /// <returns>A sanitized value</returns>
    private static object SanitizeValueRecursive(object value, int depth)
    {
        // Prevent infinite recursion
        if (depth > MaxSanitizationDepth)
            return "[Max depth reached]";

        if (value == null)
            return null;

        var type = value.GetType();

        // Handle primitive and simple types
        var primitiveResult = SanitizePrimitiveTypes(value, type);
        if (primitiveResult != null)
            return primitiveResult;

        // Handle special types (DateTime, TimeSpan, Guid, Enum)
        var specialResult = SanitizeSpecialTypes(value, type);
        if (specialResult != null)
            return specialResult;

        // Handle collections (arrays, dictionaries, enumerables)
        var collectionResult = SanitizeCollectionTypes(value, type, depth);
        if (collectionResult != null)
            return collectionResult;

        // Handle complex objects
        return SanitizeComplexObject(value, type, depth);
    }

    /// <summary>
    ///     Sanitizes primitive types and strings.
    /// </summary>
    /// <param name="value">The value to sanitize</param>
    /// <param name="type">The type of the value</param>
    /// <returns>Sanitized value or null if not a primitive type</returns>
    private static object SanitizePrimitiveTypes(object value, Type type)
    {
        if (!type.IsPrimitive && type != typeof(string) && type != typeof(decimal))
            return null;

        // Handle problematic numeric types that cause JSON serialization issues
        if (type == typeof(IntPtr) || type == typeof(UIntPtr) ||
            type == typeof(uint) || type == typeof(ulong) ||
            type == typeof(ushort) || type == typeof(byte) || type == typeof(sbyte))
        {
            return value.ToString();
        }

        // Keep safe primitive types as-is
        return value;
    }

    /// <summary>
    ///     Sanitizes special types like DateTime, TimeSpan, Guid, and Enums.
    /// </summary>
    /// <param name="value">The value to sanitize</param>
    /// <param name="type">The type of the value</param>
    /// <returns>Sanitized value or null if not a special type</returns>
    private static object SanitizeSpecialTypes(object value, Type type)
    {
        if (type == typeof(DateTime))
            return ((DateTime)value).ToString("O"); // ISO 8601 format

        if (type == typeof(TimeSpan))
            return ((TimeSpan)value).ToString();

        if (type == typeof(Guid))
            return value.ToString();

        if (type.IsEnum)
            return value.ToString();

        return null;
    }

    /// <summary>
    ///     Sanitizes collection types (arrays, dictionaries, enumerables).
    /// </summary>
    /// <param name="value">The value to sanitize</param>
    /// <param name="type">The type of the value</param>
    /// <param name="depth">Current recursion depth</param>
    /// <returns>Sanitized value or null if not a collection type</returns>
    private static object SanitizeCollectionTypes(object value, Type type, int depth)
    {
        // Handle arrays
        if (type.IsArray)
            return SanitizeArray((Array)value, type, depth);

        // Handle dictionaries
        if (value is System.Collections.IDictionary dict)
            return SanitizeDictionary(dict, depth);

        // Handle other collections (List, etc.)
        if (value is System.Collections.IEnumerable enumerable && !(value is string))
            return SanitizeEnumerable(enumerable, depth);

        return null;
    }

    /// <summary>
    ///     Sanitizes array values
    /// </summary>
    /// <param name="array">The array to sanitize</param>
    /// <param name="type">The array type</param>
    /// <param name="depth">Current recursion depth</param>
    /// <returns>Sanitized array</returns>
    private static object SanitizeArray(Array array, Type type, int depth)
    {
        // Check if it's a known safe array type
        if (IsKnownSafeArrayType(type))
        {
            return array; // Return original array for known safe types
        }

        // Otherwise, sanitize to object array
        var sanitizedArray = new object[array.Length];
        for (int i = 0; i < array.Length; i++)
        {
            sanitizedArray[i] = SanitizeValueRecursive(array.GetValue(i), depth + 1);
        }

        return sanitizedArray;
    }

    /// <summary>
    /// Checks if an array type is known to be safe without reflection
    /// </summary>
    private static bool IsKnownSafeArrayType(Type type)
    {
        return type == typeof(string[]) ||
               type == typeof(int[]) ||
               type == typeof(long[]) ||
               type == typeof(double[]) ||
               type == typeof(float[]) ||
               type == typeof(bool[]) ||
               type == typeof(decimal[]);
    }

    /// <summary>
    ///     Checks if all elements in an array are safe types.
    /// </summary>
    /// <param name="array">The array to check</param>
    /// <returns>True if all elements are safe</returns>
    private static bool IsArrayElementsSafe(Array array)
    {
        for (int i = 0; i < array.Length; i++)
        {
            var element = array.GetValue(i);
            if (element != null && NeedsTypeSanitization(element.GetType()))
                return false;
        }

        return true;
    }

    /// <summary>
    ///     Sanitizes dictionary values.
    /// </summary>
    /// <param name="dict">The dictionary to sanitize</param>
    /// <param name="depth">Current recursion depth</param>
    /// <returns>Sanitized dictionary</returns>
    private static object SanitizeDictionary(System.Collections.IDictionary dict, int depth)
    {
        var sanitizedDict = new System.Collections.Generic.Dictionary<string, object>();
        foreach (System.Collections.DictionaryEntry entry in dict)
        {
            var key = entry.Key?.ToString() ?? "null";
            sanitizedDict[key] = SanitizeValueRecursive(entry.Value, depth + 1);
        }

        return sanitizedDict;
    }

    /// <summary>
    ///     Sanitizes enumerable values.
    /// </summary>
    /// <param name="enumerable">The enumerable to sanitize</param>
    /// <param name="depth">Current recursion depth</param>
    /// <returns>Sanitized list</returns>
    private static object SanitizeEnumerable(System.Collections.IEnumerable enumerable, int depth)
    {
        var sanitizedList = new System.Collections.Generic.List<object>();
        foreach (var item in enumerable)
        {
            sanitizedList.Add(SanitizeValueRecursive(item, depth + 1));
        }

        return sanitizedList;
    }

    /// <summary>
    ///     Sanitizes complex objects by converting them to safe string representation.
    /// </summary>
    /// <param name="value">The object to sanitize</param>
    /// <param name="type">The object type</param>
    /// <param name="depth">Current recursion depth</param>
    /// <returns>Sanitized string representation</returns>
    private static object SanitizeComplexObject(object value, Type type, int depth)
    {
        try
        {
            // For Native AOT compatibility, we avoid reflection and convert to string
            // This ensures the object can be serialized without issues
            return $"[{type.Name}] {value.ToString()}";
        }
        catch (Exception ex)
        {
            // If all else fails, return a safe fallback
            return $"[Object conversion failed: {ex.Message}]";
        }
    }



    /// <summary>
    ///     Determines if a type is safe for X-Ray without sanitization
    /// </summary>
    /// <param name="type">The type to check</param>
    /// <returns>True if the type is safe</returns>
    private static bool IsSafeType(Type type)
    {
        return type == typeof(string) ||
               type == typeof(int) ||
               type == typeof(long) ||
               type == typeof(double) ||
               type == typeof(float) ||
               type == typeof(bool) ||
               type == typeof(decimal);
    }

    /// <summary>
    ///     Checks if a type needs sanitization due to potential JSON serialization issues.
    /// </summary>
    /// <param name="type">The type to check</param>
    /// <returns>True if the type needs sanitization</returns>
    private static bool NeedsTypeSanitization(Type type)
    {
        // Problematic primitive types that cause LitJson issues
        if (type == typeof(IntPtr) || type == typeof(UIntPtr) ||
            type == typeof(uint) || type == typeof(ulong) ||
            type == typeof(ushort) || type == typeof(byte) || type == typeof(sbyte))
            return true;

        // Other problematic types
        if (type == typeof(DateTime) || type == typeof(TimeSpan) ||
            type == typeof(Guid) || type.IsEnum)
            return true;

        // Check for specific nullable types without reflection
        if (IsKnownNullableProblematicType(type))
            return true;

        return false;
    }

    /// <summary>
    /// Checks for known nullable problematic types without using reflection
    /// </summary>
    private static bool IsKnownNullableProblematicType(Type type)
    {
        return type == typeof(IntPtr?) || type == typeof(UIntPtr?) ||
               type == typeof(uint?) || type == typeof(ulong?) ||
               type == typeof(ushort?) || type == typeof(byte?) || type == typeof(sbyte?) ||
               type == typeof(DateTime?) || type == typeof(TimeSpan?) ||
               type == typeof(Guid?);
    }

    /// <summary>
    ///     Safely sanitizes the current entity to prevent JSON serialization errors.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Entity properties are preserved by X-Ray SDK")]
    private void SanitizeCurrentEntitySafely()
    {
        try
        {
            var entity = _awsxRayRecorder?.TraceContext?.GetEntity();
            if (entity == null) return;

            // Sanitize known entity properties without reflection
            SanitizeEntityMetadata(entity);
            SanitizeEntityAnnotations(entity);
            SanitizeEntityHttpInformation(entity);
        }
        catch (Exception ex)
        {
            // Log the error but don't break tracing
            Console.WriteLine($"Warning: Entity sanitization failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Sanitizes the metadata in an entity
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Entity.Metadata property is preserved by X-Ray SDK")]
    private static void SanitizeEntityMetadata(Entity entity)
    {
        try
        {
            var metadataProperty = entity.GetType().GetProperty("Metadata");
            if (metadataProperty?.GetValue(entity) is not System.Collections.IDictionary metadata) return;

            // Create a list of keys to avoid modifying collection while iterating
            var namespaceKeys = new System.Collections.Generic.List<object>();
            foreach (var key in metadata.Keys)
            {
                namespaceKeys.Add(key);
            }

            // Process each namespace
            foreach (var namespaceKey in namespaceKeys)
            {
                if (metadata[namespaceKey] is System.Collections.IDictionary namespaceData)
                {
                    var dataKeys = new System.Collections.Generic.List<object>();
                    foreach (var key in namespaceData.Keys)
                    {
                        dataKeys.Add(key);
                    }

                    // Sanitize each value in the namespace
                    foreach (var dataKey in dataKeys)
                    {
                        var originalValue = namespaceData[dataKey];
                        var sanitizedValue = SanitizeValueForMetadata(originalValue);
                        namespaceData[dataKey] = sanitizedValue;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Metadata sanitization failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Sanitizes the annotations in an entity
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Entity.Annotations property is preserved by X-Ray SDK")]
    private static void SanitizeEntityAnnotations(Entity entity)
    {
        try
        {
            var annotationsProperty = entity.GetType().GetProperty("Annotations");
            if (annotationsProperty?.GetValue(entity) is not System.Collections.IDictionary annotations) return;

            var annotationKeys = new System.Collections.Generic.List<object>();
            foreach (var key in annotations.Keys)
            {
                annotationKeys.Add(key);
            }

            foreach (var key in annotationKeys)
            {
                var originalValue = annotations[key];
                var sanitizedValue = SanitizeValueForAnnotation(originalValue);
                annotations[key] = sanitizedValue;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Annotations sanitization failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Sanitizes HTTP information in an entity
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Entity.Http property is preserved by X-Ray SDK")]
    private static void SanitizeEntityHttpInformation(Entity entity)
    {
        try
        {
            var httpProperty = entity.GetType().GetProperty("Http");
            if (httpProperty?.GetValue(entity) is not System.Collections.IDictionary http) return;

            var httpKeys = new System.Collections.Generic.List<object>();
            foreach (var key in http.Keys)
            {
                httpKeys.Add(key);
            }

            foreach (var key in httpKeys)
            {
                var originalValue = http[key];
                var sanitizedValue = SanitizeValueForMetadata(originalValue);
                http[key] = sanitizedValue;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: HTTP information sanitization failed: {ex.Message}");
        }
    }


}