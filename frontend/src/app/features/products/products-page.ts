import { CurrencyPipe } from '@angular/common';
import { Component, computed, effect, inject, signal, untracked, viewChild } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Observable, filter, switchMap } from 'rxjs';
import { FeedbackMessage } from '../../core/enums/feedback-message';
import { ProductLimits } from '../../core/enums/limits';
import { Permission } from '../../core/enums/permission';
import { Category, UNCATEGORIZED_ID } from '../../core/models/category';
import { Product } from '../../core/models/product';
import { CategoriesService } from '../../core/services/categories.service';
import { NotificationService } from '../../core/services/notification.service';
import { ProductsService } from '../../core/services/products.service';
import { SessionStore } from '../../core/services/session.store';
import { ConfirmDialog, ConfirmDialogData } from '../../shared/confirm-dialog/confirm-dialog';
import { HistoryDialog } from './history-dialog';
import { ProductFormDialog, ProductFormData } from './product-form-dialog';
import { StockDialog } from './stock-dialog';

// Each filter uses its own API endpoint: all products, search by name, or stock level.
export enum FilterMode {
  All = 'all',
  Name = 'name',
  StockLevel = 'stockLevel',
}

@Component({
  selector: 'app-products-page',
  imports: [
    ReactiveFormsModule,
    CurrencyPipe,
    MatTableModule,
    MatSortModule,
    MatPaginatorModule,
    MatButtonModule,
    MatButtonToggleModule,
    MatChipsModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatMenuModule,
    MatProgressBarModule,
    MatSlideToggleModule,
    MatTooltipModule,
  ],
  templateUrl: './products-page.html',
  styleUrl: './products-page.css',
})
export class ProductsPage {
  private readonly productsService = inject(ProductsService);
  private readonly categoriesService = inject(CategoriesService);
  private readonly notifications = inject(NotificationService);
  private readonly dialog = inject(MatDialog);
  private readonly session = inject(SessionStore);

  protected readonly FilterMode = FilterMode;
  protected readonly lowStock = ProductLimits.LowStock;
  protected readonly uncategorizedId = UNCATEGORIZED_ID;

  protected readonly loading = signal(false);
  protected readonly includeHidden = signal(false);
  protected readonly dataSource = new MatTableDataSource<Product>([]);
  private categories: Category[] = [];

  protected readonly filters = new FormGroup({
    mode: new FormControl(FilterMode.All, { nonNullable: true }),
    name: new FormControl('', { nonNullable: true }),
    min: new FormControl<number | null>(null),
    max: new FormControl<number | null>(null),
  });

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

  constructor() {
    this.dataSource.sortingDataAccessor = (product, column) =>
      column === 'category' ? product.categoryName.toLowerCase() : (product as any)[column];

    effect(() => {
      this.dataSource.sort = this.sort();
      this.dataSource.paginator = this.paginator();
    });

    // Reload whenever the user changes (each role sees different products).
    effect(() => {
      this.session.user();
      untracked(() => {
        if (!this.canViewHidden()) {
          this.includeHidden.set(false);
        }
        this.load();
        this.loadCategories();
      });
    });
  }

  protected load(): void {
    const { mode, name, min, max } = this.filters.getRawValue();
    const includeHidden = this.includeHidden();

    let request$: Observable<Product[]>;
    if (mode === FilterMode.Name && name.trim()) {
      request$ = this.productsService.search(name.trim(), includeHidden);
    } else if (mode === FilterMode.StockLevel && (min !== null || max !== null)) {
      request$ = this.productsService.getByStockLevel(min, max, includeHidden);
    } else {
      request$ = this.productsService.getAll(includeHidden);
    }

    this.loading.set(true);
    request$.subscribe({
      next: (products) => {
        this.dataSource.data = products;
        this.loading.set(false);
      },
      error: (error) => {
        this.loading.set(false);
        this.notifications.error(error);
      },
    });
  }

  protected setIncludeHidden(value: boolean): void {
    this.includeHidden.set(value);
    this.load();
  }

  protected clearFilters(): void {
    this.filters.reset({ mode: this.filters.controls.mode.value });
    this.load();
  }

  protected openForm(product: Product | null = null): void {
    const data: ProductFormData = { product, categories: this.categories };
    this.dialog
      .open(ProductFormDialog, { data, width: '560px', maxWidth: '95vw' })
      .afterClosed()
      .pipe(filter(Boolean))
      .subscribe(() => this.load());
  }

  protected openStock(product: Product): void {
    this.dialog
      .open(StockDialog, { data: product, width: '400px', maxWidth: '95vw' })
      .afterClosed()
      .pipe(filter(Boolean))
      .subscribe(() => this.load());
  }

  protected openHistory(product: Product): void {
    this.dialog.open(HistoryDialog, { data: product, width: '720px', maxWidth: '95vw' });
  }

  protected toggleActive(product: Product): void {
    const isActive = !product.isActive;
    this.productsService.setActive(product.id, isActive).subscribe({
      next: () => {
        this.notifications.success(isActive ? FeedbackMessage.ProductEnabled : FeedbackMessage.ProductDisabled);
        this.load();
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
          this.load();
        },
        error: (error) => this.notifications.error(error),
      });
  }

  // Categories for the product form (active ones; editors also get the hidden ones).
  private loadCategories(): void {
    this.categoriesService.getAll(this.canViewHidden()).subscribe({
      next: (categories) => (this.categories = categories),
      error: () => (this.categories = []),
    });
  }
}
