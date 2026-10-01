export interface Product {
  id: number;
  name: string;
  description: string | null;
  price: number;
  stock: number;
  categoryId: number;
  categoryName: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  updatedByName: string | null;
  rowVersion: string;
}

export interface ProductRequest {
  name: string;
  description: string | null;
  price: number;
  stock: number;
  categoryId: number;
}

export interface UpdateProductRequest extends ProductRequest {
  rowVersion: string;
}

export interface ProductVersion {
  name: string;
  price: number;
  stock: number;
  isActive: boolean;
  categoryId: number;
  updatedByName: string | null;
  validFrom: string;
  validTo: string | null;
}
