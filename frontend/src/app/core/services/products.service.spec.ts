import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ErrorCode } from '../enums/error-code';
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

  it('calls the search endpoint with the name', () => {
    service.search('lens', false).subscribe();

    const request = http.expectOne((r) => r.url === `${environment.apiUrl}/products/search`);
    expect(request.request.params.get('name')).toBe('lens');
    request.flush([]);
  });

  it('only sends the stock limits that are set', () => {
    service.getByStockLevel(null, 5, false).subscribe();

    const request = http.expectOne((r) => r.url === `${environment.apiUrl}/products/stock-level`);
    expect(request.request.params.has('min')).toBe(false);
    expect(request.request.params.get('max')).toBe('5');
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

    service.getAll(false).subscribe();

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
