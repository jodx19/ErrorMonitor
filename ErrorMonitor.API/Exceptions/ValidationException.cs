namespace ErrorMonitor.API.Exceptions;

/// <summary>
/// يُرمى عندما تكون بيانات الإدخال غير صالحة.
/// يُرجع تلقائياً 400 Bad Request.
/// </summary>
public sealed class ValidationException(string message)
    : AppException(message, StatusCodes.Status400BadRequest);
