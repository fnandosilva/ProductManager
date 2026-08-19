import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { Product, ProductRequest } from '../models/product.model';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly baseUrl = `${environment.apiUrl}/products`;

  constructor(private readonly http: HttpClient) {}

  getAll(): Observable<Product[]> {
    return this.http.get<Product[]>(this.baseUrl);
  }

  getById(id: number): Observable<Product> {
    return this.http.get<Product>(`${this.baseUrl}/${id}`);
  }

  search(name: string): Observable<Product[]> {
    const params = new HttpParams().set('name', name);
    return this.http.get<Product[]>(`${this.baseUrl}/search`, { params });
  }

  getByStockLevel(min: number, max: number): Observable<Product[]> {
    const params = new HttpParams().set('min', min).set('max', max);
    return this.http.get<Product[]>(`${this.baseUrl}/stock-level`, { params });
  }

  create(request: ProductRequest): Observable<Product> {
    return this.http.post<Product>(this.baseUrl, request);
  }

  update(id: number, request: ProductRequest): Observable<Product> {
    return this.http.put<Product>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  addToStock(id: number, quantity: number): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/add-to-stock/${quantity}`, null);
  }

  decrementStock(id: number, quantity: number): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/decrement-stock/${quantity}`, null);
  }
}
