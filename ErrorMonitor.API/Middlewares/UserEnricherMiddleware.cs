using System.Security.Claims;
using Serilog.Context;

namespace ErrorMonitor.API.Middlewares;

/// <summary>
/// Middleware يقرأ UserId من JWT Token ويُضيفه تلقائياً لكل سجل Serilog.
/// بعد هذا، كل Log في Seq سيحتوي على خاصية UserId — مما يُسهّل تتبع أخطاء مستخدم معين.
/// </summary>
public class UserEnricherMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // استخراج UserId من الـ JWT Claims
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? context.User.FindFirstValue("sub")
                  ?? "anonymous";

        var userEmail = context.User.FindFirstValue(ClaimTypes.Email)
                     ?? context.User.FindFirstValue("email")
                     ?? "unknown";

        // PushProperty يُضيف الخاصية لكل سجل ضمن نطاق هذا الطلب
        using (LogContext.PushProperty("UserId",    userId))
        using (LogContext.PushProperty("UserEmail", userEmail))
        {
            await next(context);
        }
        // بعد انتهاء الطلب، تُزال الخصائص تلقائياً
    }
}
