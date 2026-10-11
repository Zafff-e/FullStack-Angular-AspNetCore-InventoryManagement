import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Product } from '../../models/product';
import { Brand } from '../../models/brand';
import { Category } from '../../models/category';
import { ProductService } from '../../services/product-service';
import { CategoryService } from '../../services/category-service';
import { UploadResponse } from '../../models/upload-response';

const API_BASE_URL = 'http://localhost:5234';

@Component({
  selector: 'app-add-product',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './add-product.html',
  styleUrl: './add-product.css'
})
export class AddProduct implements OnInit {
  isEditMode: boolean = false;
  productIdToEdit: number | null = null;

  categories: Category[] = [];

  productObj: Product = {
    productId: 0,
    productName: '',
    manufacturingDate: new Date().toISOString().split('T')[0],
    serialNo: '',
    imageUrl: '',
    isAvailable: true,
    categoryId: 0,
    price: 0,
    brands: []
  };

  brandObj: Brand = {
    brandId: 0,
    brandName: '',
    stock: 0,
    productId: 0
  };

  brandList: Brand[] = [];

  imageUrl: any = null;
  selectedFile: File | null = null;
  isSaving: boolean = false;

  constructor(
    private productService: ProductService,
    private categoryService: CategoryService,
    private router: Router,
    private route: ActivatedRoute,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadCategories();

    this.route.paramMap.subscribe((params) => {
      const idParam = params.get('id');
      if (idParam) {
        this.isEditMode = true;
        this.productIdToEdit = +idParam;
        this.loadProductForEditing(this.productIdToEdit);
      } else {
        this.resetForm();
      }
    });
  }

  loadCategories(): void {
    this.categoryService.getCategories().subscribe({
      next: (data) => {
        this.categories = data;
        if (this.categories.length > 0 && !this.productObj.categoryId) {
          this.productObj.categoryId = this.categories[0].categoryId;
        }
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Failed to load categories:', err)
    });
  }

  resetForm(): void {
    this.productObj = {
      productId: 0,
      productName: '',
      manufacturingDate: new Date().toISOString().split('T')[0],
      serialNo: '',
      imageUrl: '',
      isAvailable: true,
      categoryId: this.categories.length > 0 ? this.categories[0].categoryId : 0,
      price: 0,
      brands: []
    };
    this.brandObj = { brandId: 0, brandName: '', stock: 0, productId: 0 };
    this.brandList = [];
    this.selectedFile = null;
    this.imageUrl = null;
    this.productIdToEdit = null;
  }

  loadProductForEditing(id: number): void {
    this.productService.getById(id).subscribe({
      next: (foundProduct: Product) => {
        if (foundProduct) {
          this.productObj = { ...foundProduct };
          if (foundProduct.manufacturingDate) {
            this.productObj.manufacturingDate = new Date(foundProduct.manufacturingDate).toISOString().split('T')[0];
          }
          this.brandList = foundProduct.brands ? [...foundProduct.brands] : [];
          if (foundProduct.imageUrl && foundProduct.imageUrl !== 'noimage.png') {
            this.imageUrl = `${API_BASE_URL}/images/${foundProduct.imageUrl}`;
          }
          this.cdr.detectChanges();
        } else {
          console.error('Product not found ID: ' + id);
          this.router.navigate(['/display-products']);
        }
      },
      error: (err) => {
        console.error('Failed to load product details:', err);
      }
    });
  }

  onSelectedFile(event: any): void {
    const file = event.target.files?.[0];
    if (file) {
      this.selectedFile = file;
      const reader = new FileReader();
      reader.onload = (e: any) => {
        this.imageUrl = e.target.result;
        this.cdr.detectChanges();
      };
      reader.readAsDataURL(file);
    }
  }

  addBrand(): void {
    if (this.brandObj.brandName.trim() !== '' && this.brandObj.stock >= 0) {
      this.brandObj.productId = this.productObj.productId ?? 0;
      const cleanBrandObj = JSON.parse(JSON.stringify(this.brandObj));
      this.brandList.push(cleanBrandObj);
      this.brandObj = { brandId: 0, brandName: '', stock: 0, productId: 0 };
    }
  }

  deleteBrand(brandItem: Brand): void {
    const index = this.brandList.indexOf(brandItem);
    if (index > -1) {
      this.brandList.splice(index, 1);
    }
  }

  saveProduct(): void {
    if (!this.productObj.productName || !this.productObj.serialNo || !this.productObj.categoryId) {
      alert('Please fill in Product Name, Serial No, and Category.');
      return;
    }

    this.isSaving = true;

    const targetProduct: Product = {
      productId: this.productObj.productId,
      productName: this.productObj.productName,
      manufacturingDate: this.productObj.manufacturingDate,
      serialNo: this.productObj.serialNo,
      imageUrl: this.productObj.imageUrl || '',
      isAvailable: this.productObj.isAvailable,
      categoryId: +this.productObj.categoryId,
      price: +this.productObj.price,
      brands: this.brandList
    };

    if (this.isEditMode) {
      this.productService.update(targetProduct).subscribe({
        next: () => {
          console.log('Product updated successfully');
          this.handleImage(this.productIdToEdit!);
        },
        error: (err) => {
          this.isSaving = false;
          console.error('Error updating product:', err);
        }
      });
    } else {
      this.productService.save(targetProduct).subscribe({
        next: (savedProduct) => {
          console.log('Product saved successfully');
          this.handleImage(savedProduct.productId);
        },
        error: (err) => {
          this.isSaving = false;
          console.error('Error saving product:', err);
        }
      });
    }
  }

  private handleImage(id: number): void {
    if (this.selectedFile) {
      this.uploadImage(id);
    } else {
      this.finishAndNavigate();
    }
  }

  private uploadImage(id: number): void {
    if (this.selectedFile) {
      this.productService.uploadImage(id, this.selectedFile).subscribe({
        next: (uploadResponse: UploadResponse) => {
          console.log('Image uploaded successfully:', uploadResponse);
          this.finishAndNavigate();
        },
        error: (err) => {
          console.error('Failed to upload image:', err);
          this.finishAndNavigate();
        }
      });
    }
  }

  finishAndNavigate(): void {
    this.isSaving = false;
    this.resetForm();
    this.cdr.detectChanges();
    this.router.navigate(['/display-products']);
  }
}
