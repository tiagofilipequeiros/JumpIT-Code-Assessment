// What a chart needs to know. Pass one of these to <app-chart [config]>; the component does the rest
// (theme, colours, zoom, tooltips, legend, empty state, table view).
//
// Example - a new bar chart is just:
//   config: ChartConfig = { type: 'bar', categories: ['A', 'B'], series: [{ name: 'Units', values: [3, 5] }] };

export type ChartConfig = TimelineChartConfig | BarChartConfig | HeatmapChartConfig;

// Values over time. Drag on the plot to zoom; the toolbar resets.
export interface TimelineChartConfig {
  type: 'timeline';
  series: ChartSeries<TimePoint>[];
  // line: trends · area: volumes (stack them with stacked: true) · bar: separate counts per period
  // step: levels that stay until they change (e.g. stock)
  style?: 'line' | 'area' | 'bar' | 'step';
  stacked?: boolean;
  // 'day' for one point per day, 'datetime' when points have a time.
  precision?: 'day' | 'datetime';
  unit?: string;
}

// Compare amounts between categories.
export interface BarChartConfig {
  type: 'bar';
  categories: string[];
  series: ChartSeries<number>[];
  horizontal?: boolean;
  stacked?: boolean;
  unit?: string;
}

// Intensity on a grid (rows x columns), on a single-colour scale.
export interface HeatmapChartConfig {
  type: 'heatmap';
  columns: string[];
  rows: { name: string; values: number[] }[];
  unit?: string;
}

export interface ChartSeries<T> {
  name: string;
  data: T[];
  // Fixed colour slot (0-4). Give it when series come and go, so a series keeps its colour ("colour follows the entity").
  color?: number;
}

export interface TimePoint {
  // ISO date or date-time.
  x: string;
  y: number;
}
