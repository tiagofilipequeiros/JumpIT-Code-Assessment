import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { Permission } from '../../core/enums/permission';
import { ProductMetrics, ProductStockHistory, UserMetrics } from '../../core/models/metrics';
import { Product } from '../../core/models/product';
import { MetricsService } from '../../core/services/metrics.service';
import { NotificationService } from '../../core/services/notification.service';
import { ProductsService } from '../../core/services/products.service';
import { SessionStore } from '../../core/services/session.store';
import { Chart } from '../../shared/chart/chart';
import { SERIES_COLORS } from '../../shared/chart/chart-theme';
import { StatTile } from '../../shared/stat-tile/stat-tile';
import {
  actionsPerUserChart,
  activityChart,
  activityHeatmap,
  movementsChart,
  stockHistoryChart,
  topRemovedChart,
} from './metrics-charts';

export enum MetricsView {
  Products = 'products',
  Users = 'users',
}

export const TIME_RANGES = [7, 30, 90] as const;
const MAX_STOCK_PRODUCTS = 5;
const DEFAULT_STOCK_PRODUCTS = 3;

@Component({
  selector: 'app-metrics-page',
  imports: [
    CurrencyPipe,
    DecimalPipe,
    MatButtonModule,
    MatButtonToggleModule,
    MatFormFieldModule,
    MatIconModule,
    MatSelectModule,
    MatTooltipModule,
    Chart,
    StatTile,
  ],
  templateUrl: './metrics-page.html',
  styleUrl: './metrics-page.css',
  host: { '(document:visibilitychange)': 'reloadWhenVisible()' },
})
export class MetricsPage {
  private readonly metricsService = inject(MetricsService);
  private readonly productsService = inject(ProductsService);
  private readonly notifications = inject(NotificationService);
  private readonly session = inject(SessionStore);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly MetricsView = MetricsView;
  protected readonly timeRanges = TIME_RANGES;
  protected readonly maxStockProducts = MAX_STOCK_PRODUCTS;
  protected readonly seriesColors = SERIES_COLORS;

  // Editors see product metrics; per-user activity is admin only.
  protected readonly canViewUsers = computed(() => this.session.can(Permission.ViewUserMetrics));
  // Initial state comes from the URL (?view=users&days=90), so a refresh or a shared link shows the same view.
  protected readonly view = signal(parseView(this.route.snapshot.queryParamMap.get('view')));
  protected readonly days = signal<number>(parseDays(this.route.snapshot.queryParamMap.get('days')));

  protected readonly productMetrics = signal<ProductMetrics | null>(null);
  protected readonly stockHistory = signal<ProductStockHistory[] | null>(null);
  protected readonly userMetrics = signal<UserMetrics | null>(null);
  protected readonly productOptions = signal<Product[]>([]);
  protected readonly selectedProductIds = signal<number[]>([]);
  protected readonly loadingProducts = signal(false);
  protected readonly loadingStock = signal(false);
  protected readonly loadingUsers = signal(false);

  // Each selected product keeps its colour while others are added or removed.
  private readonly colorSlots = new Map<number, number>();

  protected readonly movementsConfig = computed(() => map(this.productMetrics(), (m) => movementsChart(m.movementsPerDay)));
  protected readonly topRemovedConfig = computed(() => map(this.productMetrics(), (m) => topRemovedChart(m.topRemoved)));
  protected readonly stockConfig = computed(() => map(this.stockHistory(), (h) => stockHistoryChart(h, this.colorSlots)));
  protected readonly activityConfig = computed(() => map(this.userMetrics(), (m) => activityChart(m.activityPerDay)));
  protected readonly perUserConfig = computed(() => map(this.userMetrics(), (m) => actionsPerUserChart(m.perUser)));
  protected readonly heatmapConfig = computed(() => map(this.userMetrics(), (m) => activityHeatmap(m.perHour)));
  protected readonly periodLabel = computed(() => `Last ${this.days()} days`);

  private productSubscription?: Subscription;
  private stockSubscription?: Subscription;
  private userSubscription?: Subscription;

  constructor() {
    effect(() => {
      // Re-runs when the user, the view or the time range changes.
      this.session.user();
      const view = this.view();
      const days = this.days();
      untracked(() => {
        if (view === MetricsView.Users && !this.canViewUsers()) {
          this.view.set(MetricsView.Products);
          return;
        }
        void this.router.navigate([], { queryParams: { view, days }, replaceUrl: true });
        this.load(view, days);
      });
    });

    effect(() => {
      const ids = this.selectedProductIds();
      const days = this.days();
      untracked(() => this.loadStockHistory(ids, days));
    });

    this.productsService.getAll(true).subscribe({
      next: (products) => this.productOptions.set(products),
      error: (error) => this.notifications.error(error),
    });
  }

  protected reload(): void {
    this.load(this.view(), this.days());
    if (this.view() === MetricsView.Products) {
      this.loadStockHistory(this.selectedProductIds(), this.days());
    }
  }

  protected reloadWhenVisible(): void {
    if (document.visibilityState === 'visible') {
      this.reload();
    }
  }

  protected selectProducts(ids: number[]): void {
    const kept = ids.slice(0, MAX_STOCK_PRODUCTS);
    this.assignColors(kept);
    this.selectedProductIds.set(kept);
  }

  protected colorOf(productId: number): string {
    return SERIES_COLORS[this.colorSlots.get(productId) ?? 0];
  }

  private load(view: MetricsView, days: number): void {
    if (view === MetricsView.Products) {
      this.loadProductMetrics(days);
    } else {
      this.loadUserMetrics(days);
    }
  }

  private loadProductMetrics(days: number): void {
    this.productSubscription?.unsubscribe();
    this.loadingProducts.set(true);
    this.productSubscription = this.metricsService.getProductMetrics(days).subscribe({
      next: (metrics) => {
        this.productMetrics.set(metrics);
        this.loadingProducts.set(false);
        // First visit: show the products that moved the most.
        if (this.selectedProductIds().length === 0 && metrics.topRemoved.length > 0) {
          this.selectProducts(metrics.topRemoved.slice(0, DEFAULT_STOCK_PRODUCTS).map((p) => p.productId));
        }
      },
      error: (error) => {
        this.loadingProducts.set(false);
        this.notifications.error(error);
      },
    });
  }

  private loadStockHistory(ids: number[], days: number): void {
    this.stockSubscription?.unsubscribe();
    if (ids.length === 0) {
      this.stockHistory.set([]);
      return;
    }

    this.loadingStock.set(true);
    this.stockSubscription = this.metricsService.getStockHistory(ids, days).subscribe({
      next: (history) => {
        this.stockHistory.set(history);
        this.loadingStock.set(false);
      },
      error: (error) => {
        this.loadingStock.set(false);
        this.notifications.error(error);
      },
    });
  }

  private loadUserMetrics(days: number): void {
    this.userSubscription?.unsubscribe();
    this.loadingUsers.set(true);
    this.userSubscription = this.metricsService.getUserMetrics(days).subscribe({
      next: (metrics) => {
        this.userMetrics.set(metrics);
        this.loadingUsers.set(false);
      },
      error: (error) => {
        this.loadingUsers.set(false);
        this.notifications.error(error);
      },
    });
  }

  // Frees the slots of removed products and gives new ones the first free slot.
  private assignColors(ids: number[]): void {
    for (const id of [...this.colorSlots.keys()]) {
      if (!ids.includes(id)) {
        this.colorSlots.delete(id);
      }
    }
    for (const id of ids) {
      if (!this.colorSlots.has(id)) {
        const used = new Set(this.colorSlots.values());
        this.colorSlots.set(id, [...SERIES_COLORS.keys()].find((slot) => !used.has(slot)) ?? 0);
      }
    }
  }
}

function parseView(value: string | null): MetricsView {
  return value === MetricsView.Users ? MetricsView.Users : MetricsView.Products;
}

function parseDays(value: string | null): number {
  const days = Number(value);
  return (TIME_RANGES as readonly number[]).includes(days) ? days : 30;
}

function map<T, R>(value: T | null, transform: (value: T) => R): R | null {
  return value === null ? null : transform(value);
}
