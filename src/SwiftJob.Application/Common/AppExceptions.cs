namespace SwiftJob.Application.Common;

/// <summary>The request is valid but conflicts with the current state (HTTP 409).</summary>
public class ConflictException(string message, Exception? innerException = null)
    : Exception(message, innerException);

/// <summary>A required external service (e.g. Google) failed or rejected the call (HTTP 502).</summary>
public class ExternalServiceException(string message, Exception? innerException = null)
    : Exception(message, innerException);
