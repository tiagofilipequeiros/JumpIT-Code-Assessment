import { BreakpointObserver } from '@angular/cdk/layout';
import { CurrencyPipe, NgTemplateOutlet } from '@angular/common';
import {
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { debounceTime, filter, map } from 'rxjs';
import { ProductLimits } from '../../core/enums/limits';
import {
  PRODUCT_STATUS_LABELS,
  ProductStatus,
  STOCK_STATUS_LABELS,
  StockStatus,
} from '../../core/enums/product-status';
import { Category } from '../../core/models/category';
import { NO_FILTERS, ProductFilters } from '../../core/models/product';
import { integer } from '../../core/utils/validators';

// Search reacts while typing, from this many characters (or when cleared).
export const SEARCH_MIN_LENGTH = 3;
const SEARCH_DEBOUNCE_MS = 300;
const RANGE_DEBOUNCE_MS = 400;

interface ActiveFilter {
  label: string;
  remove: Partial<ProductFilters>;
}

// Always-visible filter bar for the products table. Every filter combines with the others (AND).
// It only emits the combined value; the page loads the data.
@Component({
  selector: 'app-product-filters',
  imports: [
    ReactiveFormsModule,
    NgTemplateOutlet,
    MatButtonModule,
    MatChipsModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
  ],
  templateUrl: './product-filters.html',
  styleUrl: './product-filters.css',
  providers: [CurrencyPipe],
})
export class ProductFiltersBar {
  readonly filters = input.required<ProductFilters>();
  readonly categories = input<Category[]>([]);
  // Product statuses other than Active only exist for editors and admins.
  readonly canFilterStatus = input(false);
  readonly resultCount = input<number | null>(null);
  readonly filtersChange = output<ProductFilters>();

  protected readonly searchMinLength = SEARCH_MIN_LENGTH;
  protected readonly productStatuses = Object.values(ProductStatus);
  protected readonly stockStatuses = Object.values(StockStatus);
  protected readonly statusLabels = PRODUCT_STATUS_LABELS;
  protected readonly stockLabels = STOCK_STATUS_LABELS;
  protected readonly stockMax = ProductLimits.StockMax;
  protected readonly priceMax = ProductLimits.PriceMax;

  protected readonly search = new FormControl('', { nonNullable: true });
  protected readonly ranges = new FormGroup(
    {
      minStock: new FormControl<number | null>(null, [Validators.min(0), integer]),
      maxStock: new FormControl<number | null>(null, [Validators.min(0), integer]),
      minPrice: new FormControl<number | null>(null, [Validators.min(0)]),
      maxPrice: new FormControl<number | null>(null, [Validators.min(0)]),
    },
    {
      validators: [
        rangeOrder('minStock', 'maxStock', 'stockRange'),
        rangeOrder('minPrice', 'maxPrice', 'priceRange'),
      ],
    },
  );

  // Phones: only the search stays visible; the other filters open with a "Filters" button.
  protected readonly compact = toSignal(
    inject(BreakpointObserver)
      .observe('(max-width: 575.98px)')
      .pipe(map((state) => state.matches)),
    { initialValue: false },
  );
  protected readonly showFilters = signal(false);
  protected readonly showMore = signal(false);
  protected readonly searchTooShort = signal(false);
  protected readonly rangeCount = computed(
    () =>
      ['minStock', 'maxStock', 'minPrice', 'maxPrice'].filter(
        (k) => this.filters()[k as keyof ProductFilters] !== null,
      ).length,
  );
  protected readonly hiddenFilterCount = computed(() => {
    const f = this.filters();
    return f.categoryIds.length + f.statuses.length + f.stockStatuses.length + this.rangeCount();
  });
  protected readonly activeFilters = computed(() =>
    this.describe(this.filters(), this.categories()),
  );

  private readonly currency = inject(CurrencyPipe);

  constructor() {
    const destroyRef = inject(DestroyRef);

    // Sync the controls when an applied value changes from outside (URL, chip removed, clear all),
    // without overwriting what the user is still typing in the other fields.
    let previous: ProductFilters | null = null;
    effect(() => {
      const current = this.filters();
      if (previous?.search !== current.search && this.search.value.trim() !== current.search) {
        this.search.setValue(current.search, { emitEvent: false });
      }
      const rangeKeys = ['minStock', 'maxStock', 'minPrice', 'maxPrice'] as const;
      if (!previous || rangeKeys.some((key) => previous![key] !== current[key])) {
        this.ranges.setValue(
          {
            minStock: current.minStock,
            maxStock: current.maxStock,
            minPrice: current.minPrice,
            maxPrice: current.maxPrice,
          },
          { emitEvent: false },
        );
      }
      if (!previous && this.rangeCount() > 0) {
        this.showMore.set(true);
      }
      previous = current;
    });

    this.search.valueChanges
      .pipe(
        map((value) => value.trim()),
        debounceTime(SEARCH_DEBOUNCE_MS),
        // 1-2 characters would match almost everything: wait for more.
        filter((value) => {
          const tooShort = value.length > 0 && value.length < SEARCH_MIN_LENGTH;
          this.searchTooShort.set(tooShort);
          return !tooShort;
        }),
        // Compared with the applied value (not the previous keystroke): clearing sets the field without events.
        filter((value) => value !== this.filters().search),
        takeUntilDestroyed(destroyRef),
      )
      .subscribe((search) => this.emit({ search }));

    // Ranges apply while typing, once they are valid (min ≤ max, not negative).
    this.ranges.valueChanges
      .pipe(
        debounceTime(RANGE_DEBOUNCE_MS),
        filter(() => this.ranges.valid),
        takeUntilDestroyed(destroyRef),
      )
      .subscribe((value) =>
        this.emit({
          minStock: toNumber(value.minStock),
          maxStock: toNumber(value.maxStock),
          minPrice: toNumber(value.minPrice),
          maxPrice: toNumber(value.maxPrice),
        }),
      );
  }

  protected hasNegative(): boolean {
    return Object.values(this.ranges.controls).some((control) => control.hasError('min'));
  }

  protected emit(change: Partial<ProductFilters>): void {
    this.filtersChange.emit({ ...this.filters(), ...change });
  }

  protected clearSearch(): void {
    this.search.setValue('', { emitEvent: false });
    this.searchTooShort.set(false);
    this.emit({ search: '' });
  }

  protected clearAll(): void {
    this.searchTooShort.set(false);
    this.filtersChange.emit(NO_FILTERS);
  }

  protected categoryName(id: number): string {
    return this.categories().find((c) => c.id === id)?.name ?? `#${id}`;
  }

  private describe(filters: ProductFilters, categories: Category[]): ActiveFilter[] {
    const active: ActiveFilter[] = [];
    if (filters.search) {
      active.push({ label: `"${filters.search}"`, remove: { search: '' } });
    }
    for (const id of filters.categoryIds) {
      active.push({
        label: categories.find((c) => c.id === id)?.name ?? `Category #${id}`,
        remove: { categoryIds: filters.categoryIds.filter((c) => c !== id) },
      });
    }
    for (const status of filters.statuses) {
      active.push({
        label: PRODUCT_STATUS_LABELS[status],
        remove: { statuses: filters.statuses.filter((s) => s !== status) },
      });
    }
    for (const status of filters.stockStatuses) {
      active.push({
        label: STOCK_STATUS_LABELS[status],
        remove: { stockStatuses: filters.stockStatuses.filter((s) => s !== status) },
      });
    }
    if (filters.minStock !== null || filters.maxStock !== null) {
      active.push({
        label: `Stock ${range(filters.minStock, filters.maxStock, String)}`,
        remove: { minStock: null, maxStock: null },
      });
    }
    if (filters.minPrice !== null || filters.maxPrice !== null) {
      const money = (v: number) =>
        this.currency.transform(v, 'EUR', 'symbol', '1.0-2') ?? String(v);
      active.push({
        label: `Price ${range(filters.minPrice, filters.maxPrice, money)}`,
        remove: { minPrice: null, maxPrice: null },
      });
    }
    return active;
  }
}

function rangeOrder(minKey: string, maxKey: string, error: string) {
  return (group: AbstractControl): ValidationErrors | null => {
    const min = toNumber(group.get(minKey)?.value);
    const max = toNumber(group.get(maxKey)?.value);
    return min !== null && max !== null && min > max ? { [error]: true } : null;
  };
}

function toNumber(value: unknown): number | null {
  return value === null || value === undefined || value === '' || Number.isNaN(Number(value))
    ? null
    : Number(value);
}

function range(min: number | null, max: number | null, format: (v: number) => string): string {
  if (min !== null && max !== null) {
    return `${format(min)}–${format(max)}`;
  }
  return min !== null ? `≥ ${format(min)}` : `≤ ${format(max!)}`;
}
