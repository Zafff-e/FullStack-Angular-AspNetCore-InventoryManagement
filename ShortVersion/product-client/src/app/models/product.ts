export interface Brand {
  brandId?: number;
  brandName: string;
  stock: number;
}

export interface Product {
  productId?: number;
  productName: string;
  manufacturingDate: string;   // "yyyy-MM-dd" -> works directly with <input type="date">
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
