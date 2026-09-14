namespace ErrorMonitor.API.Exceptions;

/// <summary>
/// يُرمى عندما لا يكون المستخدم مصادقاً عليه.
/// يُرجع تلقائياً 401 Unauthorized.
/// </summary>
public sealed class UnauthorizedException(string message = "Authentication is required to access this resource.")
    : AppException(message, StatusCodes.Status401Unauthorized);
