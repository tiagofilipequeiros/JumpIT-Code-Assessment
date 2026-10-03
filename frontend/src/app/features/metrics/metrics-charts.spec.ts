import { activityHeatmap, actionsPerUserChart, stockHistoryChart } from './metrics-charts';
import { Role } from '../../core/enums/permission';

describe('metrics charts', () => {
  it('keeps the same colour for an action type in every chart', () => {
    const chart = actionsPerUserChart([{ userId: 1, name: 'Alex', role: Role.Admin, logins: 1, edits: 2, stockChanges: 3 }]);

    expect(chart.series.map((s) => [s.name, s.color])).toEqual([
      ['Logins', 0],
      ['Edits', 1],
      ['Stock changes', 2],
    ]);
  });

  it('keeps each product on its colour slot', () => {
    const chart = stockHistoryChart(
      [{ productId: 7, name: 'Lens', points: [{ time: '2026-09-01T00:00:00Z', stock: 3 }] }],
      new Map([[7, 3]]),
    );

    expect(chart.series[0].color).toBe(3);
    expect(chart.style).toBe('step');
  });

  it('places activity on the local weekday and hour', () => {
    const local = new Date(2026, 8, 30, 14, 0); // Wednesday 14:00 in the viewer's time zone.

    const heatmap = activityHeatmap([{ hourUtc: local.toISOString(), count: 5 }]);

    expect(heatmap.rows.find((r) => r.name === 'Wed')!.values[14]).toBe(5);
    expect(heatmap.rows.flatMap((r) => r.values).reduce((a, b) => a + b)).toBe(5);
  });
});
