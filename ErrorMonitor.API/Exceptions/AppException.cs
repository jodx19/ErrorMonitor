namespace ErrorMonitor.API.Exceptions;

/// <summary>
/// الكلاس الأساسي لجميع الاستثناءات المخصصة في التطبيق.
/// يحمل StatusCode مناسباً يُستخدم مباشرةً في GlobalExceptionHandler.
/// </summary>
public abstract class AppException(string message, int statusCode) : Exception(message)
{
    /// <summary>HTTP Status Code المرتبط بهذا الاستثناء</summary>
    public int StatusCode { get; } = statusCode;
}
