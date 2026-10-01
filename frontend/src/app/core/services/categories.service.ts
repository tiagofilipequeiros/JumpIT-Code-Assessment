import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Category } from '../models/category';

const url = `${environment.apiUrl}/categories`;

@Injectable({ providedIn: 'root' })
export class CategoriesService {
  private readonly http = inject(HttpClient);

  getAll(includeHidden: boolean): Observable<Category[]> {
    return this.http.get<Category[]>(url, { params: { includeHidden } });
  }

  create(name: string): Observable<Category> {
    return this.http.post<Category>(url, { name });
  }

  update(id: number, name: string, rowVersion: string): Observable<Category> {
    return this.http.put<Category>(`${url}/${id}`, { name, rowVersion });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${url}/${id}`);
  }

  setActive(id: number, isActive: boolean): Observable<Category> {
    return this.http.post<Category>(`${url}/${id}/${isActive ? 'enable' : 'disable'}`, null);
  }
}
