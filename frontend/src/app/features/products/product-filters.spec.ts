import { TestBed } from '@angular/core/testing';
import { ProductStatus, StockStatus } from '../../core/enums/product-status';
import { NO_FILTERS, ProductFilters } from '../../core/models/product';
import { ProductFiltersBar } from './product-filters';

function setup(filters: ProductFilters = NO_FILTERS, canFilterStatus = true) {
  const fixture = TestBed.createComponent(ProductFiltersBar);
  fixture.componentRef.setInput('filters', filters);
  fixture.componentRef.setInput('canFilterStatus', canFilterStatus);
  fixture.componentRef.setInput('categories', [
    { id: 2, name: 'Objectives', isActive: true, isProtected: false, productCount: 3, createdAt: '', updatedAt: '', rowVersion: '' },
  ]);
  const emitted: ProductFilters[] = [];
  fixture.componentInstance.filtersChange.subscribe((value) => emitted.push(value));
  fixture.detectChanges();
  const element = fixture.nativeElement as HTMLElement;
  const type = (text: string) => {
    const input = element.querySelector<HTMLInputElement>('input[name="search"]')!;
    input.value = text;
    input.dispatchEvent(new Event('input'));
  };
  return { fixture, element, emitted, type };
}

describe('ProductFiltersBar', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it('searches while typing, from the third character', () => {
    const { emitted, type } = setup();

    type('le');
    vi.advanceTimersByTime(400);
    expect(emitted).toEqual([]);

    type('len');
    vi.advanceTimersByTime(400);
    expect(emitted.at(-1)?.search).toBe('len');
  });

  it('waits until the user stops typing', () => {
    const { emitted, type } = setup();

    type('obj');
    vi.advanceTimersByTime(100);
    type('obje');
    vi.advanceTimersByTime(400);

    expect(emitted.map((f) => f.search)).toEqual(['obje']);
  });

  it('keeps the other filters when the search changes (AND, not replace)', () => {
    const current = { ...NO_FILTERS, categoryIds: [2], stockStatuses: [StockStatus.LowStock], maxPrice: 300 };
    const { emitted, type } = setup(current);

    type('lens');
    vi.advanceTimersByTime(400);

    expect(emitted.at(-1)).toEqual({ ...current, search: 'lens' });
  });

  it('shows one removable chip per active filter', () => {
    const current = { ...NO_FILTERS, search: 'lens', categoryIds: [2], statuses: [ProductStatus.Disabled], minStock: 0, maxStock: 5 };
    const { element, emitted } = setup(current);

    const chips = [...element.querySelectorAll('mat-chip')].map((c) => c.textContent?.replace('cancel', '').trim());
    expect(chips).toEqual(['"lens"', 'Objectives', 'Disabled', 'Stock 0–5']);

    element.querySelectorAll<HTMLButtonElement>('mat-chip button')[1].click();
    expect(emitted.at(-1)).toEqual({ ...current, categoryIds: [] });
  });

  it('clears everything at once', () => {
    const { element, emitted } = setup({ ...NO_FILTERS, search: 'lens', minPrice: 10 });

    [...element.querySelectorAll<HTMLButtonElement>('button')].find((b) => b.textContent?.includes('Clear all'))!.click();

    expect(emitted.at(-1)).toEqual(NO_FILTERS);
  });

  it('does not apply a range where min is above max, and says why', () => {
    const { fixture, element, emitted } = setup();
    [...element.querySelectorAll<HTMLButtonElement>('button')].find((b) => b.textContent?.includes('Ranges'))!.click();
    fixture.detectChanges();

    const set = (name: string, value: string) => {
      const input = element.querySelector<HTMLInputElement>(`input[name="${name}"]`)!;
      input.value = value;
      input.dispatchEvent(new Event('input'));
    };
    set('minStock', '50');
    set('maxStock', '10');
    vi.advanceTimersByTime(500);
    fixture.detectChanges();

    expect(emitted).toEqual([]);
    expect(element.textContent).toContain('Minimum stock is higher than maximum stock.');
  });

  it('hides the product status filter from normal users', () => {
    const { element } = setup(NO_FILTERS, false);

    const labels = [...element.querySelectorAll('mat-label')].map((label) => label.textContent?.trim());
    expect(labels).not.toContain('Status');
    expect(labels).toContain('Stock status');
  });
});
