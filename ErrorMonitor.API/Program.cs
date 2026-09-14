using System.Text;
using ErrorMonitor.API.Errors;
using ErrorMonitor.API.HealthChecks;
using ErrorMonitor.API.Middlewares;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc;
using Serilog;

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Bootstrap Logger — يلتقط الأخطاء حتى قبل بناء التطبيق
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("🚀 Starting ErrorMonitor.API...");

    var builder = WebApplication.CreateBuilder(args);

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 1. Serilog — قراءة الإعدادات من appsettings + الـ Sinks
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    var seqUrl    = builder.Configuration["SeqSettings:ServerUrl"] ?? "http://localhost:5341";
    var seqApiKey = builder.Configuration["SeqSettings:ApiKey"];

    builder.Host.UseSerilog((context, loggerConfig) =>
    {
        loggerConfig
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext()           // يُضيف UserId تلقائياً عبر UserEnricherMiddleware
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {NewLine}{Exception}")
            .WriteTo.File(
                path: "Logs/log-.txt",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] UserId={UserId} {Message:lj}{NewLine}{Exception}")
            .WriteTo.Seq(seqUrl, apiKey: seqApiKey);
    });

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 2. تسجيل الخدمات
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    builder.Services.AddControllers();
    builder.Services.AddOpenApi();
    builder.Services.AddHttpClient(); // مطلوب لـ SeqHealthCheck

    // CORS — السماح لـ Angular Dev Server
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AngularDevPolicy", policy =>
        {
            policy.WithOrigins("http://localhost:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });

        options.AddPolicy("AllowVercel",
            policy =>
            {
                policy.WithOrigins("https://your-angular-app.vercel.app") // سيتم استبداله لاحقاً برابط Vercel الفعلي
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
    });

    // Global Exception Handler (.NET 8)
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // ─── Health Checks ──────────────────────────────────────
    builder.Services.AddHealthChecks()
        .AddCheck<SeqHealthCheck>("seq-logging", tags: ["external", "logging"]);

    // ─── JWT Authentication ──────────────────────────────────
    var jwtKey    = builder.Configuration["JwtSettings:SecretKey"]!;
    var jwtIssuer = builder.Configuration["JwtSettings:Issuer"]!;
    var jwtAudience = builder.Configuration["JwtSettings:Audience"]!;

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateIssuer           = true,
                ValidIssuer              = jwtIssuer,
                ValidateAudience         = true,
                ValidAudience            = jwtAudience,
                ValidateLifetime         = true,
                ClockSkew                = TimeSpan.Zero     // لا تسامح في الوقت
            };

            // تسجيل أحداث JWT في Serilog
            options.Events = new JwtBearerEvents
            {
                // ─── OnChallenge: 401 يُرجع ProblemDetails بدل رد JwtBearer الافتراضي ───
                // الإصلاح: الـ interceptor في Angular يقرأ error.error.detail بشكل صح
                OnChallenge = async ctx =>
                {
                    ctx.HandleResponse(); // نمنع رد JwtBearer الفارغ الافتراضي

                    ctx.Response.StatusCode  = StatusCodes.Status401Unauthorized;
                    ctx.Response.ContentType = "application/problem+json";

                    var problem = new ProblemDetails
                    {
                        Status   = StatusCodes.Status401Unauthorized,
                        Title    = "Unauthorized",
                        Detail   = ctx.AuthenticateFailure?.Message
                                   ?? "Token is missing or invalid. Please login first.",
                        Type     = "https://httpstatuses.com/401",
                        Instance = ctx.Request.Path
                    };
                    problem.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;

                    Log.Warning("JWT Challenge triggered at {Path} — {Reason}",
                        ctx.Request.Path,
                        problem.Detail);

                    await ctx.Response.WriteAsJsonAsync(problem);
                },

                OnAuthenticationFailed = ctx =>
                {
                    Log.Warning("JWT Auth failed: {Error}", ctx.Exception.Message);
                    return Task.CompletedTask;
                },
                OnTokenValidated = ctx =>
                {
                    Log.Debug("JWT Token validated for: {User}",
                        ctx.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value);
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization();

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 3. بناء التطبيق
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    var app = builder.Build();

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms | User={UserId}";
    });

    // CORS يجب قبل Authentication
    app.UseCors("AngularDevPolicy");

    // Exception Handler في أول الـ Pipeline
    app.UseExceptionHandler();

    if (app.Environment.IsDevelopment())
        app.MapOpenApi();

    app.UseHttpsRedirection();

    // UserEnricherMiddleware — يُضيف UserId لكل سجل
    app.UseMiddleware<UserEnricherMiddleware>();

    app.UseAuthentication();
    
    // أضف هذا السطر قبل app.UseAuthorization();
    app.UseCors("AllowVercel");
    app.UseAuthorization();

    app.MapControllers();

    // ─── Health Check Endpoint ──────────────────────────────
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            Log.Information("Health Check Status: {Status} | Total Duration: {Duration}", 
                report.Status, 
                report.TotalDuration);
                
            await UIResponseWriter.WriteHealthCheckUIResponse(context, report);
        }
    });

    Log.Information("✅ ErrorMonitor.API is ready and listening...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ Application terminated unexpectedly during startup");
}
finally
{
    Log.CloseAndFlush();
}
