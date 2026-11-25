using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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

        var message = e.Message;
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
            _awsxRayRecorder.TraceContext.ClearEntity();
            _awsxRayRecorder.BeginSubsegment("Tracing_Sanitized");
            _awsxRayRecorder.AddAnnotation("SerializationError", true);
            _awsxRayRecorder.AddMetadata("Error", "Type", "JSON Serialization Error");
            _awsxRayRecorder.AddMetadata("Error", "Message", SanitizeValueForMetadata(originalException.Message));
            _awsxRayRecorder.EndSubsegment();
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
            return $"[Sanitization failed: {ex.Message}] {value}";
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
            var key = entry.Key.ToString() ?? "null";
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
    ///     Sanitizes complex objects by converting them to dictionaries.
    ///     Uses reflection with proper AOT attributes for compatibility.
    /// </summary>
    /// <param name="value">The object to sanitize</param>
    /// <param name="type">The object type</param>
    /// <param name="depth">Current recursion depth</param>
    /// <returns>Sanitized dictionary representation or string fallback</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2070:UnrecognizedReflectionPattern",
        Justification = "Reads the properties of metadata values passed by the application. Under Native AOT, Tracing " +
                        "requires TrimMode partial (see the Tracing AOT docs), which keeps the metadata of application " +
                        "types; the X-Ray SDK, which serializes the same values, needs it too. If a property can't be " +
                        "read, the value is recorded as an error string instead of failing.")]
    private static object SanitizeComplexObject(object value, Type type, int depth)
    {
        try
        {
            var properties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var sanitizedObject = new System.Collections.Generic.Dictionary<string, object>();

            foreach (var prop in properties)
            {
                try
                {
                    if (prop.CanRead && prop.GetIndexParameters().Length == 0) // Skip indexers
                    {
                        var propValue = prop.GetValue(value);
                        sanitizedObject[prop.Name] = SanitizeValueRecursive(propValue, depth + 1);
                    }
                }
                catch (Exception ex)
                {
                    // If we can't read a property, record the error
                    sanitizedObject[prop.Name] = $"[Error reading property: {ex.Message}]";
                }
            }

            return sanitizedObject;
        }
        catch (Exception ex)
        {
            // If reflection fails, fall back to string representation
            try
            {
                return $"[{type.Name}] {value}";
            }
            catch (Exception toStringEx)
            {
                return $"[Object conversion failed: {ex.Message}, ToString failed: {toStringEx.Message}]";
            }
        }
    }


    /// <summary>
    ///     Safely sanitizes the current entity to prevent JSON serialization errors.
    /// </summary>
    private void SanitizeCurrentEntitySafely()
    {
        try
        {
            var entity = _awsxRayRecorder?.TraceContext?.GetEntity();
            if (entity == null) return;

            // Sanitize known entity properties without reflection. Annotations aren't sanitized here: the X-Ray
            // SDK's Annotations type isn't a dictionary, and AddAnnotation already sanitizes each value.
            SanitizeEntityMetadata(entity);
            SanitizeEntityHttpInformation(entity);
        }
        catch
        {
            // ignored
        }
    }

    /// <summary>
    /// Sanitizes the metadata in an entity
    /// </summary>
    private static void SanitizeEntityMetadata(Entity entity)
    {
        try
        {
            // Snapshot the namespaces and keys to avoid modifying collections while iterating
            foreach (var namespaceData in entity.Metadata.Values.ToList())
            {
                foreach (var dataKey in namespaceData.Keys.Cast<object>().ToList())
                {
                    namespaceData[dataKey] = SanitizeValueForMetadata(namespaceData[dataKey]);
                }
            }
        }
        catch
        {
            // ignored
        }
    }

    /// <summary>
    /// Sanitizes HTTP information in an entity
    /// </summary>
    private static void SanitizeEntityHttpInformation(Entity entity)
    {
        try
        {
            var http = entity.Http;
            foreach (var key in http.Keys.ToList())
            {
                http[key] = SanitizeValueForMetadata(http[key]);
            }
        }
        catch
        {
            // ignored
        }
    }
}