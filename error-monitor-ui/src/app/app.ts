import { Component, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { JsonPipe, NgClass } from '@angular/common';
import { finalize } from 'rxjs';

interface TestResult {
  endpoint:   string;
  status:     'success' | 'error';
  statusCode: number;
  response:   unknown;
  time:       number;
}

interface Endpoint {
  label:    string;
  path:     string;
  method:   'GET' | 'POST';
  btnClass: string;
  desc:     string;
  body?:    object;
  authRequired?: boolean;
}

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [NgClass, JsonPipe],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  private readonly apiBase = 'https://errormonitor-api.runasp.net/api';

  results  = signal<TestResult[]>([]);
  loading  = signal<string | null>(null);
  
  // قراءة التوكن من localStorage عند بدء التطبيق
  jwtToken = signal<string | null>(localStorage.getItem('jwt_token'));

  endpoints: Endpoint[] = [
    // ── Test Endpoints ──────────────────────────────────
    { label: '✅ طلب ناجح',         path: 'test/ok',          method: 'GET',  btnClass: 'btn-success', desc: 'GET /api/test/ok → 200 OK' },
    { label: '🔥 خطأ 500',          path: 'test/throw',       method: 'GET',  btnClass: 'btn-danger',  desc: 'GET /api/test/throw → 500 + ProblemDetails' },
    { label: '🔍 غير موجود 404',    path: 'test/not-found',   method: 'GET',  btnClass: 'btn-warning', desc: 'GET /api/test/not-found → 404' },
    { label: '⚠️ طلب خاطئ 400',    path: 'test/bad-request', method: 'GET',  btnClass: 'btn-info',    desc: 'GET /api/test/bad-request → 400' },
    { label: '📊 مستويات Serilog', path: 'test/log-levels',  method: 'GET',  btnClass: 'btn-purple',  desc: 'GET /api/test/log-levels → يسجل في Seq' },

    // ── Auth Endpoints ──────────────────────────────────
    {
      label:    '🔑 تسجيل الدخول',
      path:     'auth/login',
      method:   'POST',
      btnClass: 'btn-teal',
      desc:     'POST /api/auth/login → يُرجع JWT Token',
      body:     { email: 'admin@demo.com', password: 'Admin@123' }
    },
    {
      label:         '👤 بياناتي (JWT)',
      path:          'auth/me',
      method:        'GET',
      btnClass:      'btn-indigo',
      desc:          'GET /api/auth/me → [Authorize] يقرأ من التوكن',
      authRequired:  true
    },

    // ── Health Check ────────────────────────────────────
    {
      label:    '❤️ Health Check',
      path:     'health',
      method:   'GET',
      btnClass: 'btn-green-glow',
      desc:     'GET /health → فحص صحة API + Seq',
    },
  ];

  constructor(private http: HttpClient) {}

  callEndpoint(ep: Endpoint): void {
    this.loading.set(ep.path);
    const start = Date.now();

    // بناء الـ URL الصحيح (health endpoint مختلف)
    const url = ep.path === 'health'
      ? `https://errormonitor-api.runasp.net/health`
      : `${this.apiBase}/${ep.path}`;

    // إضافة Authorization header لو الـ endpoint يحتاجه
    let headers = new HttpHeaders();
    if (ep.authRequired && this.jwtToken()) {
      headers = headers.set('Authorization', `Bearer ${this.jwtToken()}`);
    }

    const request$ = ep.method === 'POST'
      ? this.http.post(url, ep.body, { headers })
      : this.http.get(url, { headers });

    request$.pipe(
      finalize(() => this.loading.set(null))
    ).subscribe({
      next: (res: unknown) => {
        // حفظ الـ Token لو كان رد Login
        if (ep.path === 'auth/login') {
          const loginRes = res as { token?: string };
          if (loginRes?.token) {
            localStorage.setItem('jwt_token', loginRes.token);
            this.jwtToken.set(loginRes.token);
          }
        }
        this.addResult({ endpoint: ep.path, status: 'success', statusCode: 200, response: res, time: Date.now() - start });
      },
      error: (err: HttpErrorResponse) => {
        this.addResult({ endpoint: ep.path, status: 'error', statusCode: err.status, response: err.error, time: Date.now() - start });
      }
    });
  }

  clearResults(): void {
    this.results.set([]);
  }

  clearToken(): void {
    localStorage.removeItem('jwt_token');
    this.jwtToken.set(null);
  }

  private addResult(result: TestResult): void {
    this.results.update((prev: TestResult[]) => [result, ...prev].slice(0, 10));
  }
}
