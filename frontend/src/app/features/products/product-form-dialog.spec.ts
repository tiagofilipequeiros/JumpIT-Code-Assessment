import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { environment } from '../../../environments/environment';
import { ProductStatus, StockStatus } from '../../core/enums/product-status';
import { apiErrorInterceptor } from '../../core/interceptors/api-error.interceptor';
import { Product } from '../../core/models/product';
import { ProductFormData, ProductFormDialog } from './product-form-dialog';

const product: Product = {
  id: 100000,
  name: 'Lens',
  description: null,
  price: 10,
  stock: 5,
  categoryId: 2,
  categoryName: 'Objectives',
  isActive: true,
  status: ProductStatus.Active,
  stockStatus: StockStatus.LowStock,
  createdAt: '',
  updatedAt: '',
  updatedByName: null,
  rowVersion: 'AAAAAAAAB9E=',
};

function setup(data: ProductFormData) {
  const dialogRef = { close: vi.fn(), disableClose: false };
  TestBed.configureTestingModule({
    imports: [ProductFormDialog],
    providers: [
      provideHttpClient(withInterceptors([apiErrorInterceptor])),
      provideHttpClientTesting(),
      { provide: MAT_DIALOG_DATA, useValue: data },
      { provide: MatDialogRef, useValue: dialogRef },
    ],
  });
  const fixture = TestBed.createComponent(ProductFormDialog);
  fixture.detectChanges();
  const element = fixture.nativeElement as HTMLElement;
  const submit = () => element.querySelector<HTMLButtonElement>('button[type="submit"]')!.click();
  return { fixture, element, dialogRef, submit, http: TestBed.inject(HttpTestingController) };
}

const categories = [
  {
    id: 2,
    name: 'Objectives',
    isActive: true,
    isProtected: false,
    productCount: 1,
    createdAt: '',
    updatedAt: '',
    rowVersion: '',
  },
];

describe('ProductFormDialog', () => {
  it('sends the version it loaded, so the API can detect conflicting edits', () => {
    const { submit, http } = setup({ product, categories });

    submit();

    const request = http.expectOne(`${environment.apiUrl}/products/100000`);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body.rowVersion).toBe('AAAAAAAAB9E=');
    request.flush(product);
  });

  it('closes and asks the page to reload when someone else changed the product', () => {
    const { submit, http, dialogRef } = setup({ product, categories });

    submit();
    http
      .expectOne(`${environment.apiUrl}/products/100000`)
      .flush({ code: 'ConcurrencyConflict' }, { status: 409, statusText: 'Conflict' });

    expect(dialogRef.close).toHaveBeenCalledWith('reload');
  });

  it('shows the API validation messages under the matching fields', () => {
    const { fixture, element, submit, http, dialogRef } = setup({ product, categories });

    submit();
    http
      .expectOne(`${environment.apiUrl}/products/100000`)
      .flush(
        { code: 'ValidationFailed', errors: { name: ['Name is not allowed.'] } },
        { status: 400, statusText: 'Bad Request' },
      );
    fixture.detectChanges();

    expect(element.querySelector('mat-error')?.textContent).toContain('Name is not allowed.');
    expect(dialogRef.close).not.toHaveBeenCalled();
    expect(dialogRef.disableClose).toBe(false);
  });
});
