import { BarChartConfig, HeatmapChartConfig, TimelineChartConfig } from '../../shared/chart/chart-config';
import { ActivityPoint, ProductStockHistory, ProductUnits, StockMovementPoint, UserActivity } from '../../core/models/metrics';

// Turns API metrics into chart configs. Pure functions, so they are easy to test and to reuse.
// Series names keep the same colour slot everywhere (colour follows the entity, not its position).

export enum ActivitySeries {
  Logins = 'Logins',
  Edits = 'Edits',
  StockChanges = 'Stock changes',
}

const ACTIVITY_COLORS: Record<ActivitySeries, number> = {
  [ActivitySeries.Logins]: 0,
  [ActivitySeries.Edits]: 1,
  [ActivitySeries.StockChanges]: 2,
};

export function movementsChart(points: StockMovementPoint[]): TimelineChartConfig {
  return {
    type: 'timeline',
    style: 'bar',
    unit: 'units',
    series: [
      { name: 'Added', color: 0, data: points.map((p) => ({ x: p.date, y: p.added })) },
      { name: 'Removed', color: 1, data: points.map((p) => ({ x: p.date, y: p.removed })) },
    ],
  };
}

// colorSlots: product id -> colour slot, kept stable while products are added or removed from the selection.
export function stockHistoryChart(history: ProductStockHistory[], colorSlots: Map<number, number>): TimelineChartConfig {
  return {
    type: 'timeline',
    style: 'step',
    precision: 'datetime',
    unit: 'in stock',
    series: history.map((product) => ({
      name: product.name,
      color: colorSlots.get(product.productId),
      data: product.points.map((p) => ({ x: p.time, y: p.stock })),
    })),
  };
}

export function topRemovedChart(products: ProductUnits[]): BarChartConfig {
  return {
    type: 'bar',
    horizontal: true,
    unit: 'units',
    categories: products.map((p) => p.name),
    series: [{ name: 'Units removed', data: products.map((p) => p.units) }],
  };
}

export function activityChart(points: ActivityPoint[]): TimelineChartConfig {
  return {
    type: 'timeline',
    style: 'area',
    stacked: true,
    unit: 'actions',
    series: [
      activitySeries(ActivitySeries.Logins, points.map((p) => ({ x: p.date, y: p.logins }))),
      activitySeries(ActivitySeries.Edits, points.map((p) => ({ x: p.date, y: p.edits }))),
      activitySeries(ActivitySeries.StockChanges, points.map((p) => ({ x: p.date, y: p.stockChanges }))),
    ],
  };
}

export function actionsPerUserChart(users: UserActivity[]): BarChartConfig {
  return {
    type: 'bar',
    horizontal: true,
    stacked: true,
    unit: 'actions',
    categories: users.map((u) => `${u.name} (${u.role})`),
    series: [
      activitySeries(ActivitySeries.Logins, users.map((u) => u.logins)),
      activitySeries(ActivitySeries.Edits, users.map((u) => u.edits)),
      activitySeries(ActivitySeries.StockChanges, users.map((u) => u.stockChanges)),
    ],
  };
}

const WEEKDAYS = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];

// The API sends counts per UTC hour; the heatmap shows the viewer's local weekday and hour.
export function activityHeatmap(perHour: { hourUtc: string; count: number }[]): HeatmapChartConfig {
  const grid = WEEKDAYS.map(() => new Array<number>(24).fill(0));
  for (const { hourUtc, count } of perHour) {
    const local = new Date(hourUtc);
    grid[(local.getDay() + 6) % 7][local.getHours()] += count;
  }

  return {
    type: 'heatmap',
    unit: 'actions',
    columns: Array.from({ length: 24 }, (_, hour) => `${String(hour).padStart(2, '0')}h`),
    rows: WEEKDAYS.map((name, day) => ({ name, values: grid[day] })),
  };
}

function activitySeries<T>(name: ActivitySeries, data: T[]) {
  return { name, color: ACTIVITY_COLORS[name], data };
}
