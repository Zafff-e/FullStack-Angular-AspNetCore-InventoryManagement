import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Product } from '../models/product';
import { UploadResponse } from '../models/upload-response';

const apibaseUrl = 'http://localhost:5234';

@Injectable({
  providedIn: 'root'
})
export class ProductService {
  private http = inject(HttpClient);

  get(): Observable<Product[]> {
    return this.http.get<Product[]>(`${apibaseUrl}/api/Products`);
  }

  getWithBrands(): Observable<Product[]> {
    return this.http.get<Product[]>(`${apibaseUrl}/api/Products/Brand/Include`);
  }

  getById(id: number): Observable<Product> {
    return this.http.get<Product>(`${apibaseUrl}/api/Products/${id}/Include`);
  }

  save(data: Product): Observable<Product> {
    return this.http.post<Product>(`${apibaseUrl}/api/Products`, data);
  }

  uploadImage(id: number, f: File): Observable<UploadResponse> {
    const formData = new FormData();
    formData.append('file', f);
    return this.http.post<UploadResponse>(`${apibaseUrl}/api/Products/Upload/${id}`, formData);
  }

  update(data: Product): Observable<any> {
    return this.http.put<Product>(`${apibaseUrl}/api/Products/${data.productId}`, data);
  }

  delete(id: number): Observable<any> {
    return this.http.delete<any>(`${apibaseUrl}/api/Products/${id}`);
  }
}
