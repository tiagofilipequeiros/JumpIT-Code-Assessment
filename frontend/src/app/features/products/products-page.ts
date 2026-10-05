import { CurrencyPipe } from '@angular/common';
import { Component, computed, effect, inject, signal, untracked, viewChild } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { Subscription, filter, switchMap } from 'rxjs';
import { FeedbackMessage } from '../../core/enums/feedback-message';
import { Permission } from '../../core/enums/permission';
import {
  PRODUCT_STATUS_LABELS,
  ProductStatus,
  STOCK_STATUS_LABELS,
  StockStatus,
} from '../../core/enums/product-status';
import { Category } from '../../core/models/category';
import { NO_FILTERS, Product, ProductFilters } from '../../core/models/product';
import { CategoriesService } from '../../core/services/categories.service';
import { NotificationService } from '../../core/services/notification.service';
import { ProductsService } from '../../core/services/products.service';
import { SessionStore } from '../../core/services/session.store';
import { ConfirmDialog, ConfirmDialogData } from '../../shared/confirm-dialog/confirm-dialog';
import { HistoryDialog } from './history-dialog';
import { ProductFiltersBar } from './product-filters';
import { ProductFormDialog, ProductFormData } from './product-form-dialog';
import { StockDialog } from './stock-dialog';

@Component({
  selector: 'app-products-page',
  imports: [
    CurrencyPipe,
    MatTableModule,
    MatSortModule,
    MatPaginatorModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatProgressBarModule,
    MatTooltipModule,
    ProductFiltersBar,
  ],
  templateUrl: './products-page.html',
  styleUrl: './products-page.css',
  // Coming back to the tab reloads, so changes made elsewhere (another user, Swagger) show up.
  host: { '(document:visibilitychange)': 'reloadWhenVisible()' },
})
export class ProductsPage {
  private readonly productsService = inject(ProductsService);
  private readonly categoriesService = inject(CategoriesService);
  private readonly notifications = inject(NotificationService);
  private readonly dialog = inject(MatDialog);
  private readonly session = inject(SessionStore);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly ProductStatus = ProductStatus;
  protected readonly StockStatus = StockStatus;

  protected readonly loading = signal(false);
  protected readonly resultCount = signal<number | null>(null);
  protected readonly categories = signal<Category[]>([]);
  protected readonly dataSource = new MatTableDataSource<Product>([]);

  // All filters in one value, also kept in the URL (?search=lens&category=2&stock=LowStock).
  protected readonly filters = signal<ProductFilters>(filtersFromUrl(this.route.snapshot.queryParamMap));

  // What the current user may do (the API enforces the same rules).
  protected readonly canEdit = computed(() => this.session.can(Permission.Edit));
  protected readonly canToggle = computed(() => this.session.can(Permission.ToggleActive));
  protected readonly canDelete = computed(() => this.session.can(Permission.Delete));
  protected readonly canViewHidden = computed(() => this.session.can(Permission.ViewHidden));
  protected readonly canChangeStock = computed(() => this.session.can(Permission.ChangeStock));

  protected readonly columns = computed(() => [
    'id',
    'name',
    'category',
    'price',
    'stock',
    ...(this.canViewHidden() ? ['status'] : []),
    'actions',
  ]);

  private readonly sort = viewChild.required(MatSort);
  private readonly paginator = viewChild.required(MatPaginator);
  private loadSubscription?: Subscription;

  constructor() {
    this.dataSource.sortingDataAccessor = (product, column) =>
      column === 'category' ? product.categoryName.toLowerCase() : (product as any)[column];

    effect(() => {
      this.dataSource.sort = this.sort();
      this.dataSource.paginator = this.paginator();
    });

    // Reload when the filters or the user change (each role sees different products).
    effect(() => {
      this.session.user();
      const filters = this.filters();
      untracked(() => this.load(filters));
    });

    effect(() => {
      this.session.user();
      untracked(() => this.loadCategories());
    });
  }

  protected reload(): void {
    this.load(this.filters());
  }

  protected reloadWhenVisible(): void {
    if (document.visibilityState === 'visible') {
      this.reload();
    }
  }

  protected statusLabel(product: Product): string {
    return PRODUCT_STATUS_LABELS[product.status];
  }

  protected stockLabel(product: Product): string {
    return STOCK_STATUS_LABELS[product.stockStatus];
  }

  protected applyFilters(filters: ProductFilters): void {
    this.filters.set(filters);
    this.paginator().firstPage();
  }

  private load(filters: ProductFilters): void {
    // Normal users only have active products: a status filter would mean nothing to them.
    const applied = this.canViewHidden() ? filters : { ...filters, statuses: [] };
    this.saveFiltersToUrl(applied);

    this.loadSubscription?.unsubscribe();
    this.loading.set(true);
    this.loadSubscription = this.productsService.getAll(applied).subscribe({
      next: (products) => {
        this.dataSource.data = products;
        this.resultCount.set(products.length);
        this.loading.set(false);
      },
      error: (error) => {
        this.loading.set(false);
        this.notifications.error(error);
      },
    });
  }

  protected openForm(product: Product | null = null): void {
    const data: ProductFormData = { product, categories: this.categories() };
    this.dialog
      .open(ProductFormDialog, { data, width: '560px', maxWidth: '95vw' })
      .afterClosed()
      .pipe(filter(Boolean))
      .subscribe(() => this.reload());
  }

  protected openStock(product: Product): void {
    this.dialog
      .open(StockDialog, { data: product, width: '400px', maxWidth: '95vw' })
      .afterClosed()
      .pipe(filter(Boolean))
      .subscribe(() => this.reload());
  }

  protected openHistory(product: Product): void {
    this.dialog.open(HistoryDialog, { data: product, width: '720px', maxWidth: '95vw' });
  }

  protected toggleActive(product: Product): void {
    const isActive = !product.isActive;
    this.productsService.setActive(product.id, isActive).subscribe({
      next: () => {
        this.notifications.success(isActive ? FeedbackMessage.ProductEnabled : FeedbackMessage.ProductDisabled);
        this.reload();
      },
      error: (error) => this.notifications.error(error),
    });
  }

  protected delete(product: Product): void {
    const data: ConfirmDialogData = {
      title: 'Delete product?',
      message: `"${product.name}" will be permanently deleted. Its history is kept in the metrics. Consider disabling it instead.`,
      confirmLabel: 'Delete',
      destructive: true,
    };

    this.dialog
      .open(ConfirmDialog, { data, maxWidth: '95vw' })
      .afterClosed()
      .pipe(
        filter(Boolean),
        switchMap(() => this.productsService.delete(product.id)),
      )
      .subscribe({
        next: () => {
          this.notifications.success(FeedbackMessage.ProductDeleted);
          this.reload();
        },
        error: (error) => this.notifications.error(error),
      });
  }

  // Filters live in the URL (?mode=name&name=lens), so a refresh or a shared link shows the same list.
  private saveFiltersToUrl(filters: ProductFilters): void {
    const queryParams = {
      search: filters.search || null,
      category: filters.categoryIds.length ? filters.categoryIds : null,
      status: filters.statuses.length ? filters.statuses : null,
      stock: filters.stockStatuses.length ? filters.stockStatuses : null,
      minStock: filters.minStock,
      maxStock: filters.maxStock,
      minPrice: filters.minPrice,
      maxPrice: filters.maxPrice,
    };
    void this.router.navigate([], { queryParams, replaceUrl: true });
  }

  // Categories for the filter and the product form (editors and admins also get hidden ones).
  private loadCategories(): void {
    this.categoriesService.getAll(this.canViewHidden()).subscribe({
      next: (categories) => this.categories.set(categories),
      error: () => this.categories.set([]),
    });
  }
}

function filtersFromUrl(params: ParamMap): ProductFilters {
  const number = (key: string) => (params.get(key) ? Number(params.get(key)) : null);
  const valid = <T extends string>(values: string[], allowed: T[]) => values.filter((v): v is T => allowed.includes(v as T));
  return {
    ...NO_FILTERS,
    search: params.get('search') ?? '',
    categoryIds: params.getAll('category').map(Number).filter(Number.isInteger),
    statuses: valid(params.getAll('status'), Object.values(ProductStatus)),
    stockStatuses: valid(params.getAll('stock'), Object.values(StockStatus)),
    minStock: number('minStock'),
    maxStock: number('maxStock'),
    minPrice: number('minPrice'),
    maxPrice: number('maxPrice'),
  };
}
