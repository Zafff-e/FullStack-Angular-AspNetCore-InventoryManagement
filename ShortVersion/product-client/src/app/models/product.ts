export interface Brand {
  brandId?: number;
  brandName: string;
  stock: number;
}

export interface Product {
  productId?: number;
  productName: string;
  manufacturingDate: string;   
  serialNo: string;
  imageUrl?: string;
  isAvailable: boolean;
  categoryId: number;
  categoryName?: string;
  price: number;
  brands: Brand[];
}

export interface Category {
  categoryId: number;
  categoryName: string;
}
