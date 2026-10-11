import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProductService } from '../../services/product.service';
import { Category, Product, Brand } from '../../models/product';

@Component({
  selector: 'app-product',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './product.component.html',
  styleUrl: './product.component.css'
})
export class ProductComponent implements OnInit {
  public productService = inject(ProductService);
  private cdr = inject(ChangeDetectorRef);

  categories: Category[] = [];
  products: Product[] = [];
  isEditMode = false;
  imagePreview: string | null = null;

  brandObj: Brand = { brandName: '', stock: 0 };
  product: Product = this.defaultProduct();

  defaultProduct(): Product {
    return {
      productName: '',
      manufacturingDate: '',
      serialNo: '',
      imageUrl: 'noimage.png',
      isAvailable: true,
      categoryId: 0,
      price: 0,
      brands: []
    };
  }

  ngOnInit(): void {
    this.productService.getCategories().subscribe(d => this.categories = d);
    this.loadProducts();
  }

  loadProducts(): void {
    this.productService.getProducts().subscribe(d => {
      this.products = d;
      this.cdr.detectChanges();
    });
  }

  addBrand(): void {
    if (this.brandObj.brandName.trim() && this.brandObj.stock >= 0) {
      this.product.brands.push({ ...this.brandObj });
      this.brandObj = { brandName: '', stock: 0 };
    }
  }

  deleteBrand(index: number): void {
    this.product.brands.splice(index, 1);
  }

  onFileSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (file) {
      this.imagePreview = URL.createObjectURL(file);
      this.cdr.detectChanges();

      this.productService.uploadImage(file).subscribe(res => {
        this.product.imageUrl = res.fileName;
      });
    }
  }

  editProduct(p: Product): void {
    this.isEditMode = true;
    this.product = { ...p, brands: p.brands ? [...p.brands] : [] };
    this.imagePreview = p.imageUrl ? `${this.productService.imageUrl}${p.imageUrl}` : null;
    this.cdr.detectChanges();
  }

  resetForm(): void {
    this.isEditMode = false;
    this.imagePreview = null;
    this.product = this.defaultProduct();
    this.brandObj = { brandName: '', stock: 0 };
    this.cdr.detectChanges();
  }

  onSubmit(): void {
    if (!this.product.productName || !this.product.categoryId) {
      alert('Please fill Product Name and Category.');
      return;
    }

    if (this.isEditMode && this.product.productId) {
      this.productService.updateProduct(this.product.productId, this.product).subscribe(() => {
        this.loadProducts();
        this.resetForm();
      });
    } else {
      this.productService.addProduct(this.product).subscribe(() => {
        this.loadProducts();
        this.resetForm();
      });
    }
  }

  deleteProduct(id?: number): void {
    if (id && confirm('Are you sure you want to delete this product?')) {
      this.productService.deleteProduct(id).subscribe(() => {
        this.loadProducts();
      });
    }
  }
}
