import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ProductMetrics, ProductStockHistory, UserMetrics } from '../models/metrics';

const url = `${environment.apiUrl}/metrics`;

@Injectable({ providedIn: 'root' })
export class MetricsService {
  private readonly http = inject(HttpClient);

  getProductMetrics(days: number): Observable<ProductMetrics> {
    return this.http.get<ProductMetrics>(`${url}/products`, { params: { days } });
  }

  getStockHistory(productIds: number[], days: number): Observable<ProductStockHistory[]> {
    let params = new HttpParams().set('days', days);
    for (const id of productIds) {
      params = params.append('productIds', id);
    }
    return this.http.get<ProductStockHistory[]>(`${url}/products/stock-history`, { params });
  }

  getUserMetrics(days: number): Observable<UserMetrics> {
    return this.http.get<UserMetrics>(`${url}/users`, { params: { days } });
  }
}
