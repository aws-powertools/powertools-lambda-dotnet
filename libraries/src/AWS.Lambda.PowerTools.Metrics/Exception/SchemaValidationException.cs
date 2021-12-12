using System;

[Serializable]
public class SchemaValidationException : Exception
{
    /// <summary>
    /// Thrown when required property is missing on Metrics Object
    /// </summary>
    /// <param name="propertyName">Missing property name</param>
    public SchemaValidationException(string propertyName) : base($"EMF schema is invalid. '{propertyName}' is mandatory and not specified.") { }
}