// Same values as the backend ProductStatus / StockStatus enums. The API computes them for every product.

export enum ProductStatus {
  Active = 'Active',
  Disabled = 'Disabled',
  Uncategorized = 'Uncategorized',
  CategoryDisabled = 'CategoryDisabled',
}

export const PRODUCT_STATUS_LABELS: Record<ProductStatus, string> = {
  [ProductStatus.Active]: 'Active',
  [ProductStatus.Disabled]: 'Disabled',
  [ProductStatus.Uncategorized]: 'Uncategorized',
  [ProductStatus.CategoryDisabled]: 'Category disabled',
};

export enum StockStatus {
  InStock = 'InStock',
  LowStock = 'LowStock',
  OutOfStock = 'OutOfStock',
}

export const STOCK_STATUS_LABELS: Record<StockStatus, string> = {
  [StockStatus.InStock]: 'In stock',
  [StockStatus.LowStock]: 'Low stock',
  [StockStatus.OutOfStock]: 'Out of stock',
};
