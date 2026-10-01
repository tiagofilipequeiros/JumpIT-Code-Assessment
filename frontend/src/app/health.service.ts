import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable({ providedIn: 'root' })
export class HealthService {
  private http = inject(HttpClient);

  getHealth() {
    return this.http.get<{ status: string }>('http://localhost:8080/api/health');
  }
}
