using ErrorMonitor.API.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ErrorMonitor.API.Errors;

/// <summary>
/// معالج الأخطاء المركزي — IExceptionHandler (.NET 8+)
/// يصطاد كل استثناء غير معالج ويُرجع ProblemDetails موحد (RFC 7807)
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // ─────────────────────────────────────────────────────────
        // 1. تحديد StatusCode + Title بذكاء
        //    AppException → يحمل StatusCode بداخله (أذكى نهج)
        //    Exception    → يرجع دائماً 500
        // ─────────────────────────────────────────────────────────
        var (statusCode, title) = exception switch
        {
            // ← Custom Exceptions (تُجيب بـ StatusCode المدمج فيها)
            AppException app => (app.StatusCode, GetTitleForStatus(app.StatusCode)),

            // ← .NET Built-in Exceptions كـ fallback
            OperationCanceledException => (StatusCodes.Status499ClientClosedRequest, "Request Cancelled"),
            _                          => (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };

        // ─────────────────────────────────────────────────────────
        // 2. تسجيل الخطأ في Serilog (مع تمييز مستوى الخطورة)
        // ─────────────────────────────────────────────────────────
        if (statusCode >= 500)
        {
            logger.LogError(
                exception,
                "Server Error [{StatusCode}] | Type: {ExceptionType} | Path: {Path} | TraceId: {TraceId}",
                statusCode,
                exception.GetType().Name,
                httpContext.Request.Path,
                httpContext.TraceIdentifier);
        }
        else
        {
            // أخطاء 4xx هي client errors — نسجلها كـ Warning وليس Error
            logger.LogWarning(
                "Client Error [{StatusCode}] | Type: {ExceptionType} | Path: {Path} | Message: {Message}",
                statusCode,
                exception.GetType().Name,
                httpContext.Request.Path,
                exception.Message);
        }

        // ─────────────────────────────────────────────────────────
        // 3. بناء ProblemDetails (RFC 7807)
        // ─────────────────────────────────────────────────────────
        var problemDetails = new ProblemDetails
        {
            Status   = statusCode,
            Title    = title,
            Detail   = exception.Message,
            Type     = $"https://httpstatuses.com/{statusCode}",
            Instance = httpContext.Request.Path
        };

        // TraceId لربط السجل في Seq بالطلب المحدد
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode  = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }

    private static string GetTitleForStatus(int statusCode) => statusCode switch
    {
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        409 => "Conflict",
        422 => "Unprocessable Entity",
        _   => "Internal Server Error"
    };
}
