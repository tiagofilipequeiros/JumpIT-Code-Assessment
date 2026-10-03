import { DatePipe } from '@angular/common';
import {
  Component,
  DestroyRef,
  ElementRef,
  computed,
  effect,
  inject,
  input,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import type ApexCharts from 'apexcharts';
import { ChartConfig } from './chart-config';
import { buildChartOptions, isEmptyChart } from './chart-options';
import { toTable } from './chart-table';

// Reusable chart card: title, chart, loading state, empty state and a table view.
// Usage: <app-chart title="Units per day" subtitle="Last 30 days" [config]="config" [loading]="loading" />
@Component({
  selector: 'app-chart',
  imports: [MatButtonModule, MatIconModule, MatProgressBarModule, MatTooltipModule],
  templateUrl: './chart.html',
  styleUrl: './chart.css',
  providers: [DatePipe],
})
export class Chart {
  readonly title = input.required<string>();
  readonly subtitle = input<string>();
  readonly config = input<ChartConfig | null>(null);
  readonly loading = input(false);
  readonly height = input(300);
  readonly emptyMessage = input('No data for this period.');

  protected readonly showTable = signal(false);
  protected readonly isEmpty = computed(() => {
    const config = this.config();
    return config !== null && isEmptyChart(config);
  });
  protected readonly table = computed(() => {
    const config = this.config();
    return config ? toTable(config, (iso) => this.formatTime(iso, config)) : null;
  });

  private readonly plot = viewChild<ElementRef<HTMLElement>>('plot');
  private readonly datePipe = inject(DatePipe);
  private chart: ApexCharts | null = null;
  private chartElement: HTMLElement | null = null;

  constructor() {
    // (Re)draw whenever the config or the plot element changes. ApexCharts is loaded on first use,
    // so it is not part of the main bundle.
    effect(() => {
      const config = this.config();
      const element = this.plot()?.nativeElement ?? null;
      const height = this.height();
      untracked(() => void this.render(config, element, height));
    });

    inject(DestroyRef).onDestroy(() => this.destroyChart());
  }

  protected toggleTable(): void {
    this.showTable.update((show) => !show);
  }

  private async render(config: ChartConfig | null, element: HTMLElement | null, height: number): Promise<void> {
    if (!config || !element) {
      this.destroyChart();
      return;
    }

    const options = buildChartOptions(config, height);
    if (this.chart && this.chartElement === element) {
      await this.chart.updateOptions(options, true, true);
      return;
    }

    this.destroyChart();
    const { default: ApexChartsClass } = await import('apexcharts');
    this.chart = new ApexChartsClass(element, options);
    this.chartElement = element;
    await this.chart.render();
  }

  private destroyChart(): void {
    this.chart?.destroy();
    this.chart = null;
    this.chartElement = null;
  }

  private formatTime(iso: string, config: ChartConfig): string {
    const format = config.type === 'timeline' && config.precision === 'datetime' ? 'd MMM y, HH:mm' : 'EEE d MMM y';
    return this.datePipe.transform(iso, format) ?? iso;
  }
}
