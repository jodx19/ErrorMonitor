<div align="center">
  
# 🛡️ ErrorMonitor Full-Stack

**نظام متكامل لتسجيل ومراقبة الأخطاء (Centralized Logging) ومعالجتها بذكاء**

![.NET 8](https://img.shields.io/badge/.NET_8-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Angular](https://img.shields.io/badge/Angular-DD0031?style=for-the-badge&logo=angular&logoColor=white)
![Serilog](https://img.shields.io/badge/Serilog-F7B93E?style=for-the-badge&logo=data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAACXBIWXMAAAsTAAALEwEAmpwYAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAJESURBVHgB7VQ9a1NRFD3v3XzE/EBMShO1ltoPqFWqoG51c3EQVDrp4iC4urg4+A8cHDoI/gEXVwcRUXSwaaWl1jYJNWmM+Xg/zrmv7yX5aFPTx5sHTrj33HPuPefcc98jZPhfIRb1+42R0NipQ4HRYwF/ICwNydy893+F/oKefN7Y2dxdm9t69rU8X10x0Z3O5w2o1N4dD02cHZ78KqEIRg0Y04AhwNwgLMM0n208e7VVL2fndf01uP86dXx08uHw1A0/120yS2SGAeE6EFdglR2H9XJ28cPtl7v6P4H/SHTy/tDkjSAr7rJ4rI1s+vYyO1n6tJ1uA5gJtYnB4MTR/eR8pB2eA7R5rXN00p/c+lT+Wlw14PcHx6Pj15Mcvu1aO75N+I/GJu7GzB1c33F7FvH/lXlX84Bv2016r5MavxGzDWA9uL7j9ixy+V/m28wDvm03ye8dCE8e/R8Qd8076k6vYx1o4u48f76zZcL2nIN8N2D/X5hE/M+jQ9dnZ8eP3x90x32Wd2zP8e76y/Xp9Q/P0/q89l/253bL6eRk71B6Yqj/nCsk6wTf2v+wvbN58Wnxy/NaqbCu2858bXdlbmP+/r2BxP2w2xdl1rF5q569uV/Jv9T1/wA6u7I2f+V++fPK62+190Z9U98uVbY+tTj+XUB9r+Wqj8Z+AGh12B26Z9sNAAAAAElFTkSuQmCC)
![Seq](https://img.shields.io/badge/Seq_Logging-0082FF?style=for-the-badge)
![JWT](https://img.shields.io/badge/JWT_Auth-000000?style=for-the-badge&logo=jsonwebtokens)

</div>

---

## 📖 عن المشروع

**ErrorMonitor** هو مشروع Full-Stack متقدم يهدف إلى حل مشكلة شائعة في بيئات العمل الحقيقية (Production): **تتبع الأخطاء بدقة ومعالجتها بطريقة موحدة**. 

بناءً على معايير `RFC 7807` (Problem Details for HTTP APIs)، يقوم النظام بالتقاط كافة الأخطاء غير المتوقعة (Exceptions) في الواجهة الخلفية، وتسجيلها مركزياً في خادم **Seq** باستخدام **Serilog** مع ربطها بهوية المستخدم (JWT UserId)، ثم إرجاع رسالة خطأ قياسية للواجهة الأمامية (Angular) ليتم عرضها للمستخدم بتجربة سلسة عبر الـ Functional Interceptors.

---

## 🚀 المميزات الأساسية (Features)

- **🧠 Global Exception Handling (.NET 8):** استخدام `IExceptionHandler` لاصطياد الأخطاء مركزياً بدون تلويث الكود بـ `try/catch`.
- **🎯 Custom Exceptions:** فئات أخطاء مخصصة مثل `NotFoundException` و `ValidationException` تقوم تلقائياً بإنشاء الـ HTTP Status Code المناسب.
- **📜 Centralized Logging (Serilog + Seq):** توجيه السجلات إلى Console و Files وخادم Seq السحابي، مع مستويات دقيقة (Debug, Info, Warn, Error, Critical).
- **🔐 JWT Authentication + User Logging:** تسجيل الدخول بـ JWT، واستخدام `UserEnricherMiddleware` لدمج `UserId` مع كل عملية تسجيل (Log) لتتبع أخطاء كل مستخدم على حدة.
- **❤️ Health Checks:** نقطة فحص `/health` تراقب حالة الـ API واتصال قاعدة البيانات/Seq وتُسجل النتائج تلقائياً.
- **⚡ Angular 17 Standalone:** واجهة حديثة خالية من الـ Modules مع Functional HTTP Interceptors لالتقاط أخطاء الـ API وعرضها تلقائياً عبر Notifications مخصصة.
- **🎨 Premium Cyberpunk UI:** تصميم حصري يعتمد على Glassmorphism، Mesh Gradients، و Neon Glow Effects.

---

## 🏗️ البنية التحتية (Architecture Flow)

يوضح المخطط التالي دورة حياة الطلب من لحظة إرساله عبر Angular، مروراً باصطياد الخطأ في .NET، وحتى ظهوره في Seq:

```mermaid
sequenceDiagram
    participant User as 💻 Angular UI
    participant API as ⚙️ .NET 8 API
    participant ExceptionHandler as 🛡️ GlobalExceptionHandler
    participant Serilog as 📝 Serilog
    participant Seq as 🔍 Seq Dashboard

    User->>API: 1. إرسال HTTP Request (مع JWT)
    API->>API: 2. فشل العملية (مثلاً رمي NotFoundException)
    API-->>ExceptionHandler: 3. التقاط الخطأ
    ExceptionHandler->>Serilog: 4. تسجيل تفاصيل الخطأ + UserId + TraceId
    Serilog->>Seq: 5. رفع السجل (Log) للخادم المركزي
    ExceptionHandler-->>User: 6. إرجاع ProblemDetails (404 Not Found)
    User->>User: 7. الـ Interceptor يقرأ التفاصيل ويعرض Snackbar للمستخدم
```

---

## 🛠️ التقنيات المستخدمة (Tech Stack)

### Backend (.NET 8)
- ASP.NET Core Web API
- `Microsoft.AspNetCore.Diagnostics.ExceptionHandler`
- `Serilog` & `Serilog.Sinks.Seq` & `Serilog.Sinks.File`
- `Microsoft.AspNetCore.Authentication.JwtBearer` (v9.0)
- `AspNetCore.HealthChecks.UI.Client`

### Frontend (Angular 17+)
- Angular Standalone Components
- Functional HTTP Interceptors
- Signals for State Management
- RxJS (finalize, catchError)
- SCSS (Custom Dark Premium Theme)

---

## 💻 كيفية التشغيل محلياً

### 1. المتطلبات الأساسية
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (v18 أو أحدث)
- خادم [Seq](https://datalust.co/seq) (يمكن تثبيته محلياً أو استخدام النسخة السحابية)

### 2. تشغيل الواجهة الخلفية (Backend)
```bash
cd ErrorMonitor.API
dotnet restore
dotnet run
```
*سيعمل الـ API على المنفذ `http://localhost:5151`*

### 3. تشغيل الواجهة الأمامية (Frontend)
```bash
cd error-monitor-ui
npm install
ng serve -o
```
*سيعمل تطبيق Angular على المنفذ `http://localhost:4200`*

---

## 📸 نظرة على النظام

### 1. الواجهة الرئيسية (Premium Dashboard)
![Dashboard](docs/dashboard.png)

### 2. واجهة Seq لتتبع السجلات والأخطاء
![Seq Logs](docs/seq-logs.png)

### 3. معالجة الأخطاء الذكية (500 Internal Server Error)
![Error Snackbar](docs/error-snackbar.png)

### 4. انقطاع الاتصال (Connection Error)
![Connection Error](docs/connection-error.png)

---
*Developed as a Full-Stack CV Project to demonstrate enterprise-grade error handling and centralized logging capabilities.*
