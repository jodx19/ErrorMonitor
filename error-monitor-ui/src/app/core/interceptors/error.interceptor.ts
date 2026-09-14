import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { MatSnackBar } from '@angular/material/snack-bar';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const snackBar = inject(MatSnackBar);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      let errorMessage = 'حدث خطأ غير متوقع في الخادم. يرجى المحاولة لاحقاً.';

      // قراءة حقل detail مباشرةً من ProblemDetails القادم من .NET
      if (error.error?.detail) {
        errorMessage = error.error.detail;
      } else if (error.status === 0) {
        errorMessage = '🔌 لا يوجد اتصال بالخادم. تأكد من تشغيل الـ API.';
      } else if (error.status === 401) {
        errorMessage = '🔒 غير مصرح لك بالوصول. يرجى تسجيل الدخول.';
      } else if (error.status === 403) {
        errorMessage = '⛔ ليس لديك صلاحية للوصول إلى هذا المورد.';
      } else if (error.status === 404) {
        errorMessage = '🔍 البيانات المطلوبة غير موجودة.';
      } else if (error.status === 400) {
        errorMessage = '⚠️ بيانات الطلب غير صحيحة، تأكد من المدخلات.';
      } else if (error.status >= 500) {
        errorMessage = '🔥 حدث خطأ داخلي في الخادم، يرجى المحاولة لاحقاً.';
      }

      snackBar.open(errorMessage, 'إغلاق', {
        duration: 5000,
        horizontalPosition: 'end',
        verticalPosition: 'top',
        panelClass: ['error-snackbar']
      });

      return throwError(() => error);
    })
  );
};
