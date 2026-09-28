namespace ValetaxTest.Core.Application;

/// <summary>The requested resource does not exist (HTTP 404).</summary>
public sealed class NotFoundException(string message) : Exception(message);

/// <summary>The request conflicts with the current state of a resource (HTTP 409).</summary>
public sealed class ConflictException(string message) : Exception(message);
