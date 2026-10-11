import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Product, Category } from '../models/product';

@Injectable({ providedIn: 'root' })
export class ProductService {
  apiUrl = 'http://localhost:5058/api';
  imageUrl = 'http://localhost:5058/images/';

  private http = inject(HttpClient);

  getCategories(): Observable<Category[]> {
    return this.http.get<Category[]>(`${this.apiUrl}/categories`);
  }

  getProducts(): Observable<Product[]> {
    return this.http.get<Product[]>(`${this.apiUrl}/products`);
  }

  getProduct(id: number): Observable<Product> {
    return this.http.get<Product>(`${this.apiUrl}/products/${id}`);
  }

  addProduct(p: Product): Observable<Product> {
    return this.http.post<Product>(`${this.apiUrl}/products`, p);
  }

  updateProduct(id: number, p: Product): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/products/${id}`, p);
  }

  deleteProduct(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/products/${id}`);
  }

  uploadImage(file: File): Observable<{ fileName: string }> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<{ fileName: string }>(`${this.apiUrl}/products/upload`, formData);
  }
}
