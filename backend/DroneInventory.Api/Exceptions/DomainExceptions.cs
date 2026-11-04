namespace DroneInventory.Api.Exceptions;

/// <summary>Input violates a business rule (mapped to 400 Bad Request).</summary>
public class ValidationException(string message) : Exception(message);

/// <summary>A referenced Drone, StudentGroup or Loan does not exist (mapped to 404 Not Found).</summary>
public class NotFoundException(string message) : Exception(message);

/// <summary>The request conflicts with the current state, e.g. a duplicate Name or a Drone that is already loaned (mapped to 409 Conflict).</summary>
public class ConflictException(string message) : Exception(message);
