import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Category } from '../../models/category';
import { CategoryService } from '../../services/category-service';

@Component({
  selector: 'app-categories',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './categories.html',
  styleUrl: './categories.css'
})
export class CategoriesComponent implements OnInit {
  categories: Category[] = [];
  categoryName: string = '';
  editingCategory: Category | null = null;
  isLoading: boolean = false;

  constructor(private categoryService: CategoryService) {}

  ngOnInit(): void {
    this.loadCategories();
  }

  loadCategories(): void {
    this.isLoading = true;
    this.categoryService.getCategories().subscribe({
      next: (data) => {
        this.categories = data;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Failed to load categories:', err);
        this.isLoading = false;
      }
    });
  }

  saveCategory(): void {
    if (!this.categoryName.trim()) return;

    if (this.editingCategory) {
      const updatedCategory: Category = {
        categoryId: this.editingCategory.categoryId,
        categoryName: this.categoryName
      };
      this.categoryService.update(updatedCategory).subscribe({
        next: () => {
          this.categoryName = '';
          this.editingCategory = null;
          this.loadCategories();
        },
        error: (err) => console.error('Failed to update category:', err)
      });
    } else {
      const newCategory: Category = {
        categoryId: 0,
        categoryName: this.categoryName
      };
      this.categoryService.save(newCategory).subscribe({
        next: () => {
          this.categoryName = '';
          this.loadCategories();
        },
        error: (err) => console.error('Failed to add category:', err)
      });
    }
  }

  editCategory(cat: Category): void {
    this.editingCategory = cat;
    this.categoryName = cat.categoryName;
  }

  cancelEdit(): void {
    this.editingCategory = null;
    this.categoryName = '';
  }

  deleteCategory(id: number): void {
    if (confirm('Are you sure you want to delete this category?')) {
      this.categoryService.delete(id).subscribe({
        next: () => this.loadCategories(),
        error: (err) => console.error('Failed to delete category:', err)
      });
    }
  }
}
