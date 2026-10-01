import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Product, ProductRequest, ProductVersion, UpdateProductRequest } from '../models/product';

const url = `${environment.apiUrl}/products`;

@Injectable({ providedIn: 'root' })
export class ProductsService {
  private readonly http = inject(HttpClient);

  getAll(includeHidden: boolean): Observable<Product[]> {
    return this.http.get<Product[]>(url, { params: { includeHidden } });
  }

  search(name: string, includeHidden: boolean): Observable<Product[]> {
    return this.http.get<Product[]>(`${url}/search`, { params: { name, includeHidden } });
  }

  getByStockLevel(min: number | null, max: number | null, includeHidden: boolean): Observable<Product[]> {
    let params = new HttpParams().set('includeHidden', includeHidden);
    if (min !== null) {
      params = params.set('min', min);
    }
    if (max !== null) {
      params = params.set('max', max);
    }
    return this.http.get<Product[]>(`${url}/stock-level`, { params });
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
