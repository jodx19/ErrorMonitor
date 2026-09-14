using ErrorMonitor.API.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace ErrorMonitor.API.Controllers;

/// <summary>
/// Controller تجريبي لاختبار نظام Logging والـ Global Exception Handler
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TestController(ILogger<TestController> logger) : ControllerBase
{
    // GET /api/test/ok — طلب ناجح
    [HttpGet("ok")]
    public IActionResult GetSuccess()
    {
        logger.LogInformation("✅ Request received at /api/test/ok | Time: {Time}", DateTime.UtcNow);

        return Ok(new
        {
            Message   = "النظام يعمل بشكل صحيح!",
            Timestamp = DateTime.UtcNow,
            TraceId   = HttpContext.TraceIdentifier
        });
    }

    // GET /api/test/throw — استثناء عام → 500
    [HttpGet("throw")]
    public IActionResult ThrowGeneral()
    {
        logger.LogWarning("⚠️ Test endpoint triggered: about to throw a general Exception");
        throw new Exception("هذا خطأ تجريبي عام لاختبار Serilog و Seq!");
    }

    // GET /api/test/not-found — NotFoundException → 404 ✅ (كان يرجع 500)
    [HttpGet("not-found")]
    public IActionResult ThrowNotFound()
    {
        // نستخدم Custom Exception حتى يُرجع GlobalExceptionHandler 404 تلقائياً
        throw new NotFoundException("Product", 99);
    }

    // GET /api/test/bad-request — ValidationException → 400 ✅ (كان يرجع 500)
    [HttpGet("bad-request")]
    public IActionResult ThrowBadRequest()
    {
        // نستخدم Custom Exception حتى يُرجع GlobalExceptionHandler 400 تلقائياً
        throw new ValidationException("حقل 'userId' مطلوب ولا يمكن أن يكون فارغاً.");
    }

    // GET /api/test/unauthorized — UnauthorizedException → 401
    [HttpGet("unauthorized")]
    public IActionResult ThrowUnauthorized()
    {
        throw new UnauthorizedException("يجب تسجيل الدخول للوصول إلى هذا المورد.");
    }

    // GET /api/test/log-levels — تسجيل كل مستويات Serilog
    [HttpGet("log-levels")]
    public IActionResult TestAllLogLevels()
    {
        logger.LogDebug   ("🔍 [DEBUG]    تفاصيل التشخيص");
        logger.LogInformation("ℹ️ [INFO]     معلومات عامة عن الطلب");
        logger.LogWarning ("⚠️ [WARNING]  تحذير: شيء غير متوقع");
        logger.LogError   ("❌ [ERROR]    خطأ في منطق التطبيق");
        logger.LogCritical("🔥 [CRITICAL] خطأ حرج يستدعي تدخلاً فورياً");

        return Ok(new { Message = "تم تسجيل جميع مستويات Serilog — تحقق من Seq على http://localhost:5341" });
    }
}
