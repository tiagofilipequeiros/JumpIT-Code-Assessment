export const UNCATEGORIZED_ID = 1;

export interface Category {
  id: number;
  name: string;
  isActive: boolean;
  isProtected: boolean;
  productCount: number;
  createdAt: string;
  updatedAt: string;
  rowVersion: string;
}
