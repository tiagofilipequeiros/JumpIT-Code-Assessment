import { BarChartConfig, HeatmapChartConfig, TimelineChartConfig } from './chart-config';
import { buildChartOptions, isEmptyChart } from './chart-options';
import { toTable } from './chart-table';
import { SERIES_COLORS } from './chart-theme';

const timeline: TimelineChartConfig = {
  type: 'timeline',
  style: 'step',
  series: [
    { name: 'A', data: [{ x: '2026-09-01', y: 3 }, { x: '2026-09-02', y: 5 }] },
    { name: 'B', color: 4, data: [{ x: '2026-09-01', y: 1 }] },
  ],
};

describe('buildChartOptions', () => {
  it('makes timelines zoomable with a datetime axis', () => {
    const options = buildChartOptions(timeline, 300);

    expect(options.chart?.zoom?.enabled).toBe(true);
    expect(options.xaxis?.type).toBe('datetime');
    expect(options.stroke?.curve).toBe('stepline');
  });

  it('assigns colours in fixed order, unless a series asks for its own slot', () => {
    expect(buildChartOptions(timeline, 300).colors).toEqual([SERIES_COLORS[0], SERIES_COLORS[4]]);
  });

  it('shows a legend only when there is more than one series', () => {
    const single: BarChartConfig = { type: 'bar', categories: ['x'], series: [{ name: 'Only', data: [1] }] };

    expect(buildChartOptions(timeline, 300).legend?.show).toBe(true);
    expect(buildChartOptions(single, 300).legend?.show).toBe(false);
  });

  it('never prints numbers on the marks', () => {
    expect(buildChartOptions(timeline, 300).dataLabels?.enabled).toBe(false);
  });

  it('keeps category names readable on horizontal bars', () => {
    const bar: BarChartConfig = { type: 'bar', horizontal: true, categories: ['Lens'], series: [{ name: 'Units', data: [1200] }] };
    const options = buildChartOptions(bar, 300);
    const xFormatter = (options.xaxis?.labels?.formatter as (v: string) => string)!;

    expect((options.yaxis as { labels?: { formatter?: unknown } }).labels?.formatter).toBeUndefined();
    expect(xFormatter('1200')).toMatch(/1.?200/);
  });

  it('puts the first heatmap row at the top', () => {
    const heatmap: HeatmapChartConfig = { type: 'heatmap', columns: ['00h'], rows: [{ name: 'Mon', values: [1] }, { name: 'Tue', values: [2] }] };

    expect((buildChartOptions(heatmap, 300).series as { name: string }[]).map((s) => s.name)).toEqual(['Tue', 'Mon']);
  });
});

describe('isEmptyChart', () => {
  it('treats only-zero data as empty', () => {
    expect(isEmptyChart({ type: 'bar', categories: ['a'], series: [{ name: 's', data: [0] }] })).toBe(true);
    expect(isEmptyChart(timeline)).toBe(false);
  });
});

describe('toTable', () => {
  it('lists every time point with one column per series', () => {
    const table = toTable(timeline, (iso) => iso);

    expect(table.headers).toEqual(['Date', 'A', 'B']);
    expect(table.rows).toEqual([
      ['2026-09-01', 3, 1],
      ['2026-09-02', 5, '–'],
    ]);
  });
});
