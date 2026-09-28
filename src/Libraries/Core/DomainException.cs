namespace ValetaxTest.Core;

/// <summary>A business rule of the domain is violated by the requested operation.</summary>
public class DomainException(string message) : Exception(message);
