import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ErrorCode } from '../enums/error-code';
import { ProductStatus, StockStatus } from '../enums/product-status';
import { Permission, Role } from '../enums/permission';
import { apiErrorInterceptor } from '../interceptors/api-error.interceptor';
import { userIdInterceptor } from '../interceptors/user-id.interceptor';
import { ApiError } from '../models/api-error';
import { ProductsService } from './products.service';
import { SessionStore } from './session.store';

describe('ProductsService', () => {
  let service: ProductsService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([userIdInterceptor, apiErrorInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(ProductsService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('sends every filter in one request, lists as repeated parameters', () => {
    service
      .getAll({
        search: ' lens ',
        categoryIds: [2, 3],
        statuses: [ProductStatus.Active, ProductStatus.Disabled],
        stockStatuses: [StockStatus.LowStock],
        minStock: 0,
        maxStock: null,
        minPrice: 10,
        maxPrice: 300,
      })
      .subscribe();

    const request = http.expectOne((r) => r.url === `${environment.apiUrl}/products`);
    const params = request.request.params;
    expect(params.get('search')).toBe('lens');
    expect(params.getAll('categoryIds')).toEqual(['2', '3']);
    expect(params.getAll('statuses')).toEqual(['Active', 'Disabled']);
    expect(params.getAll('stockStatuses')).toEqual(['LowStock']);
    expect(params.get('minStock')).toBe('0');
    expect(params.has('maxStock')).toBe(false);
    expect(params.get('maxPrice')).toBe('300');
    request.flush([]);
  });

  it('sends no parameters when nothing is filtered', () => {
    service.getAll().subscribe();

    const request = http.expectOne((r) => r.url === `${environment.apiUrl}/products`);
    expect(request.request.params.keys()).toEqual([]);
    request.flush([]);
  });

  it('uses the decrement-stock endpoint with the quantity in the URL', () => {
    service.decrementStock(100000, 3).subscribe();

    const request = http.expectOne(`${environment.apiUrl}/products/100000/decrement-stock/3`);
    expect(request.request.method).toBe('POST');
    request.flush({});
  });

  it('sends the selected user in the X-User-Id header', () => {
    TestBed.inject(SessionStore).setUser({
      id: 3,
      name: 'Sam User',
      email: 'sam@example.com',
      role: Role.User,
      permissions: [Permission.ChangeStock],
    });

    service.getAll().subscribe();

    const request = http.expectOne((r) => r.url === `${environment.apiUrl}/products`);
    expect(request.request.headers.get('X-User-Id')).toBe('3');
    request.flush([]);
  });

  it('turns error responses into ApiError with a typed code', async () => {
    const result = firstValueFrom(service.addToStock(1, 1));

    http
      .expectOne(`${environment.apiUrl}/products/1/add-to-stock/1`)
      .flush({ code: 'ProductNotFound' }, { status: 404, statusText: 'Not Found' });

    const error = await result.catch((e) => e);
    expect(error).toBeInstanceOf(ApiError);
    expect(error.code).toBe(ErrorCode.ProductNotFound);
  });
});
