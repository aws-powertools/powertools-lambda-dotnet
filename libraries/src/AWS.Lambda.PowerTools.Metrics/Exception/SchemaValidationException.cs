using System;

[Serializable]
public class SchemaValidationException : Exception
{
    public SchemaValidationException(string propertyName) : base($"EMF schema is invalid. '{propertyName}' is mandatory and not specified.") { }
}