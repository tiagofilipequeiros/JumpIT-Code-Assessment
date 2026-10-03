import { Role } from '../enums/permission';

// Same shapes as the backend metrics DTOs. Dates are ISO strings in UTC.

export interface ProductMetrics {
  kpis: ProductKpis;
  movementsPerDay: StockMovementPoint[];
  topRemoved: ProductUnits[];
}

export interface ProductKpis {
  inventoryValue: number;
  outOfStock: number;
  lowStock: number;
  unitsAdded: number;
  unitsRemoved: number;
}

export interface StockMovementPoint {
  date: string;
  added: number;
  removed: number;
}

export interface ProductUnits {
  productId: number;
  name: string;
  units: number;
}

export interface ProductStockHistory {
  productId: number;
  name: string;
  points: { time: string; stock: number }[];
}

export interface UserMetrics {
  kpis: UserKpis;
  activityPerDay: ActivityPoint[];
  perUser: UserActivity[];
  perHour: { hourUtc: string; count: number }[];
}

export interface UserKpis {
  activeUsers: number;
  logins: number;
  edits: number;
  stockChanges: number;
}

export interface ActivityPoint {
  date: string;
  logins: number;
  edits: number;
  stockChanges: number;
}

export interface UserActivity {
  userId: number;
  name: string;
  role: Role;
  logins: number;
  edits: number;
  stockChanges: number;
}
