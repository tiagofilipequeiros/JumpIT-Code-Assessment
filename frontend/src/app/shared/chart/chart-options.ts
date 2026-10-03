import type { ApexOptions } from 'apexcharts';
import {
  BarChartConfig,
  ChartConfig,
  ChartSeries,
  HeatmapChartConfig,
  TimelineChartConfig,
} from './chart-config';
import {
  CHART_FONT,
  CHART_GRID,
  CHART_SURFACE,
  CHART_TEXT,
  EMPTY_CELL,
  SEQUENTIAL_BLUES,
  SERIES_COLORS,
} from './chart-theme';

// Turns a ChartConfig into ApexCharts options. Pure function: easy to test, one place for the visual rules.
export function buildChartOptions(config: ChartConfig, height: number): ApexOptions {
  switch (config.type) {
    case 'timeline':
      return timeline(config, height);
    case 'bar':
      return bar(config, height);
    case 'heatmap':
      return heatmap(config, height);
  }
}

// True when there is nothing to draw (no series, or only zeros).
export function isEmptyChart(config: ChartConfig): boolean {
  switch (config.type) {
    case 'timeline':
      return !config.series.some((s) => s.data.some((p) => p.y !== 0));
    case 'bar':
      return !config.series.some((s) => s.data.some((v) => v !== 0));
    case 'heatmap':
      return !config.rows.some((r) => r.values.some((v) => v !== 0));
  }
}

function timeline(config: TimelineChartConfig, height: number): ApexOptions {
  const style = config.style ?? 'line';
  const type = style === 'area' ? 'area' : style === 'bar' ? 'bar' : 'line';
  const common = base(height, type, config.series.length, config.unit);
  return {
    ...common,
    chart: {
      ...common.chart,
      stacked: config.stacked ?? false,
      zoom: { enabled: true, type: 'x', autoScaleYaxis: true },
      toolbar: {
        show: true,
        autoSelected: 'zoom',
        tools: { download: false, selection: false, pan: false, zoom: true, zoomin: true, zoomout: true, reset: true },
      },
    },
    series: config.series.map((s) => ({ name: s.name, data: s.data.map((p) => ({ x: new Date(p.x).getTime(), y: p.y })) })),
    colors: colors(config.series),
    // Bars get a 2px surface gap instead of an outline; lines are 2px.
    stroke:
      style === 'bar'
        ? { show: true, width: 2, colors: [CHART_SURFACE] }
        : { width: 2, curve: style === 'step' ? 'stepline' : 'straight' },
    fill: style === 'area' ? { type: 'solid', opacity: 0.2 } : { type: 'solid', opacity: 1 },
    plotOptions: { bar: { columnWidth: '80%', borderRadius: 2, borderRadiusApplication: 'end' } },
    markers: { size: 0, hover: { size: 5 } },
    xaxis: {
      type: 'datetime',
      labels: { datetimeUTC: false, style: { colors: CHART_TEXT } },
      axisBorder: { color: CHART_GRID },
      axisTicks: { color: CHART_GRID },
    },
    tooltip: {
      ...common.tooltip,
      shared: true,
      intersect: false,
      x: { format: config.precision === 'datetime' ? 'dd MMM yyyy, HH:mm' : 'ddd, dd MMM yyyy' },
    },
  };
}

function bar(config: BarChartConfig, height: number): ApexOptions {
  const common = base(height, 'bar', config.series.length, config.unit);
  return {
    ...common,
    chart: { ...common.chart, stacked: config.stacked ?? false },
    series: config.series.map((s) => ({ name: s.name, data: s.data })),
    colors: colors(config.series),
    plotOptions: {
      bar: {
        horizontal: config.horizontal ?? false,
        borderRadius: 4,
        borderRadiusApplication: 'end',
        borderRadiusWhenStacked: 'last',
        barHeight: '65%',
        columnWidth: '55%',
      },
    },
    // A 2px surface-coloured gap between bars and stacked segments, instead of borders.
    stroke: { show: true, width: 2, colors: [CHART_SURFACE] },
    // Horizontal bars: values on the x axis, category names on the y axis.
    yaxis: config.horizontal ? { labels: { style: { colors: CHART_TEXT }, maxWidth: 220 } } : common.yaxis,
    xaxis: {
      categories: config.categories,
      labels: {
        style: { colors: CHART_TEXT },
        formatter: config.horizontal ? (value: string) => formatNumber(Number(value)) : undefined,
      },
      axisBorder: { color: CHART_GRID },
      axisTicks: { color: CHART_GRID },
    },
    // Horizontal bars: gridlines follow the value axis.
    grid: { ...common.grid, xaxis: { lines: { show: config.horizontal ?? false } }, yaxis: { lines: { show: !config.horizontal } } },
  };
}

function heatmap(config: HeatmapChartConfig, height: number): ApexOptions {
  const max = Math.max(1, ...config.rows.flatMap((r) => r.values));
  const step = max / SEQUENTIAL_BLUES.length;
  return {
    ...base(height, 'heatmap', 1, config.unit),
    // ApexCharts draws the first series at the bottom; reverse so the first row is at the top.
    series: [...config.rows].reverse().map((r) => ({
      name: r.name,
      data: r.values.map((y, i) => ({ x: config.columns[i], y })),
    })),
    plotOptions: {
      heatmap: {
        radius: 2,
        enableShades: false,
        colorScale: {
          ranges: [
            { from: 0, to: 0, color: EMPTY_CELL, name: 'None' },
            ...SEQUENTIAL_BLUES.map((color, i) => ({
              from: i === 0 ? 0.0001 : Math.round(step * i) + 0.0001,
              to: i === SEQUENTIAL_BLUES.length - 1 ? max : Math.round(step * (i + 1)),
              color,
              name: i === SEQUENTIAL_BLUES.length - 1 ? `${Math.round(step * i) + 1}+` : `${Math.round(step * i) + 1}-${Math.round(step * (i + 1))}`,
            })),
          ],
        },
      },
    },
    stroke: { show: true, width: 2, colors: [CHART_SURFACE] },
    legend: { show: true, position: 'bottom', horizontalAlign: 'left', fontSize: '12px', labels: { colors: CHART_TEXT } },
    xaxis: { labels: { style: { colors: CHART_TEXT }, rotate: 0, hideOverlappingLabels: true }, axisBorder: { show: false }, axisTicks: { show: false } },
    yaxis: { labels: { style: { colors: CHART_TEXT } } },
    grid: { show: false },
  };
}

// Shared look: quiet axes, hairline solid grid, no numbers on the marks, legend only for 2+ series.
function base(height: number, type: NonNullable<ApexOptions['chart']>['type'], seriesCount: number, unit?: string): ApexOptions {
  return {
    chart: {
      type,
      height,
      fontFamily: CHART_FONT,
      foreColor: CHART_TEXT,
      toolbar: { show: false },
      zoom: { enabled: false },
      animations: { enabled: !prefersReducedMotion(), speed: 300, animateGradually: { enabled: false } },
      parentHeightOffset: 0,
    },
    dataLabels: { enabled: false },
    legend: {
      show: seriesCount > 1,
      position: 'top',
      horizontalAlign: 'left',
      fontSize: '12px',
      markers: { size: 6 },
      labels: { colors: CHART_TEXT },
    },
    grid: { borderColor: CHART_GRID, strokeDashArray: 0, xaxis: { lines: { show: false } }, padding: { left: 8, right: 8 } },
    yaxis: { labels: { style: { colors: CHART_TEXT }, formatter: (v: number) => formatNumber(v) } },
    tooltip: { theme: 'light', y: { formatter: (v: number) => (unit ? `${formatNumber(v)} ${unit}` : formatNumber(v)) } },
    states: { hover: { filter: { type: 'darken' } }, active: { filter: { type: 'none' } } },
  };
}

function colors(series: ChartSeries<unknown>[]): string[] {
  return series.map((s, i) => SERIES_COLORS[(s.color ?? i) % SERIES_COLORS.length]);
}

function prefersReducedMotion(): boolean {
  return typeof window !== 'undefined' && window.matchMedia?.('(prefers-reduced-motion: reduce)').matches === true;
}

export function formatNumber(value: number): string {
  return new Intl.NumberFormat(undefined, { maximumFractionDigits: 0 }).format(value);
}
