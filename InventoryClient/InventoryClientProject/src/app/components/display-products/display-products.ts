import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ProductService } from '../../services/product-service';
import { Product } from '../../models/product';

const API_BASE_URL = 'http://localhost:5234';

@Component({
  selector: 'app-display-products',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './display-products.html',
  styleUrl: './display-products.css'
})
export class DisplayProducts implements OnInit {
  products: Product[] = [];
  isLoading: boolean = true;

  constructor(
    private service: ProductService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.getAllProductsWithBrands();
  }

  getImageUrl(fileName?: string): string {
    if (!fileName || fileName === 'noimage.png') {
      return 'https://via.placeholder.com/150?text=No+Image';
    }
    return `${API_BASE_URL}/images/${fileName}`;
  }

  getAllProductsWithBrands(): void {
    this.isLoading = true;
    this.service.getWithBrands().subscribe({
      next: (data) => {
        this.products = data;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error fetching products:', err);
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  deleteProduct(productId: number): void {
    if (confirm('Are you sure you want to delete this product record?')) {
      this.service.delete(productId).subscribe({
        next: () => {
          this.getAllProductsWithBrands();
        },
        error: (err) => {
          console.error('Delete failed:', err);
          alert('Failed to delete product.');
        }
      });
    }
  }
}
