import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Product, ProductFilters, ProductRequest, ProductVersion, UpdateProductRequest } from '../models/product';

const url = `${environment.apiUrl}/products`;

@Injectable({ providedIn: 'root' })
export class ProductsService {
  private readonly http = inject(HttpClient);

  // All filters in one request; the API combines them with AND.
  getAll(filters: Partial<ProductFilters> = {}): Observable<Product[]> {
    let params = new HttpParams();
    if (filters.search?.trim()) {
      params = params.set('search', filters.search.trim());
    }
    for (const id of filters.categoryIds ?? []) {
      params = params.append('categoryIds', id);
    }
    for (const status of filters.statuses ?? []) {
      params = params.append('statuses', status);
    }
    for (const status of filters.stockStatuses ?? []) {
      params = params.append('stockStatuses', status);
    }
    for (const key of ['minStock', 'maxStock', 'minPrice', 'maxPrice'] as const) {
      const value = filters[key];
      if (value !== null && value !== undefined) {
        params = params.set(key, value);
      }
    }
    return this.http.get<Product[]>(url, { params });
  }

  getHistory(id: number): Observable<ProductVersion[]> {
    return this.http.get<ProductVersion[]>(`${url}/${id}/history`);
  }

  create(request: ProductRequest): Observable<Product> {
    return this.http.post<Product>(url, request);
  }

  update(id: number, request: UpdateProductRequest): Observable<Product> {
    return this.http.put<Product>(`${url}/${id}`, request);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${url}/${id}`);
  }

  setActive(id: number, isActive: boolean): Observable<Product> {
    return this.http.post<Product>(`${url}/${id}/${isActive ? 'enable' : 'disable'}`, null);
  }

  addToStock(id: number, quantity: number): Observable<Product> {
    return this.http.post<Product>(`${url}/${id}/add-to-stock/${quantity}`, null);
  }

  decrementStock(id: number, quantity: number): Observable<Product> {
    return this.http.post<Product>(`${url}/${id}/decrement-stock/${quantity}`, null);
  }
}
