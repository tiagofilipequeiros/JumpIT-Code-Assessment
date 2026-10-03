import { ChartConfig } from './chart-config';

export interface ChartTable {
  headers: string[];
  rows: (string | number)[][];
}

// The same data as a table: every chart has an accessible, exact-value twin.
export function toTable(config: ChartConfig, formatTime: (iso: string) => string): ChartTable {
  switch (config.type) {
    case 'timeline': {
      const times = [...new Set(config.series.flatMap((s) => s.data.map((p) => p.x)))].sort();
      return {
        headers: ['Date', ...config.series.map((s) => s.name)],
        rows: times.map((time) => [
          formatTime(time),
          ...config.series.map((s) => s.data.find((p) => p.x === time)?.y ?? '–'),
        ]),
      };
    }
    case 'bar':
      return {
        headers: ['', ...config.series.map((s) => s.name)],
        rows: config.categories.map((category, i) => [category, ...config.series.map((s) => s.data[i] ?? 0)]),
      };
    case 'heatmap':
      return {
        headers: ['', ...config.columns],
        rows: config.rows.map((row) => [row.name, ...row.values]),
      };
  }
}
