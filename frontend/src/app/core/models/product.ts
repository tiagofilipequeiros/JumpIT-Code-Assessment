import { ProductStatus, StockStatus } from '../enums/product-status';

export interface Product {
  id: number;
  name: string;
  description: string | null;
  price: number;
  stock: number;
  categoryId: number;
  categoryName: string;
  isActive: boolean;
  status: ProductStatus;
  stockStatus: StockStatus;
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

// Filters for GET /api/products. Empty lists and nulls mean "no filter"; all filters combine with AND.
export interface ProductFilters {
  search: string;
  categoryIds: number[];
  statuses: ProductStatus[];
  stockStatuses: StockStatus[];
  minStock: number | null;
  maxStock: number | null;
  minPrice: number | null;
  maxPrice: number | null;
}

export const NO_FILTERS: ProductFilters = {
  search: '',
  categoryIds: [],
  statuses: [],
  stockStatuses: [],
  minStock: null,
  maxStock: null,
  minPrice: null,
  maxPrice: null,
};
