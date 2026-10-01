// Same limits as the backend (ProductLimits / CategoryLimits). Keep both in sync.
export enum ProductLimits {
  NameMinLength = 2,
  NameMaxLength = 100,
  DescriptionMaxLength = 500,
  PriceMin = 0,
  PriceMax = 99999999.99,
  StockMin = 0,
  StockMax = 1000000,
  QuantityMin = 1,
  QuantityMax = 100000,
  LowStock = 5,
}

export enum CategoryLimits {
  NameMinLength = 2,
  NameMaxLength = 100,
}
