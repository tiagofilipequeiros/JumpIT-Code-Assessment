// Same limits as the backend (ProductLimits / CategoryLimits). Keep both in sync.
// Minimums of 0 (price, stock) are not listed: they mean "not negative", not a configurable limit.
export enum ProductLimits {
  NameMinLength = 2,
  NameMaxLength = 100,
  DescriptionMaxLength = 500,
  PriceMax = 99999999.99,
  StockMax = 1000000,
  QuantityMin = 1,
  QuantityMax = 100000,
}

export enum CategoryLimits {
  NameMinLength = 2,
  NameMaxLength = 100,
}
