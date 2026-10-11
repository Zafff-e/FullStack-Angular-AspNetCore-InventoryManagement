import { Brand } from './brand';
import { Category } from './category';

export interface Product {
  productId: number;
  productName: string;
  manufacturingDate: Date | string;
  serialNo: string;
  imageUrl?: string;
  isAvailable: boolean;
  categoryId: number;
  price: number;
  category?: Category;
  brands?: Brand[];
}
