import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Category } from '../models/category';

const apibaseUrl = 'http://localhost:5234';

@Injectable({
  providedIn: 'root'
})
export class CategoryService {
  private http = inject(HttpClient);

  getCategories(): Observable<Category[]> {
    return this.http.get<Category[]>(`${apibaseUrl}/api/Categories`);
  }

  getById(id: number): Observable<Category> {
    return this.http.get<Category>(`${apibaseUrl}/api/Categories/${id}`);
  }

  save(data: Category): Observable<Category> {
    return this.http.post<Category>(`${apibaseUrl}/api/Categories`, data);
  }

  update(data: Category): Observable<any> {
    return this.http.put<any>(`${apibaseUrl}/api/Categories/${data.categoryId}`, data);
  }

  delete(id: number): Observable<any> {
    return this.http.delete<any>(`${apibaseUrl}/api/Categories/${id}`);
  }
}
