import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { environment } from '../../../environments/environment';
import { Permission, Role } from '../../core/enums/permission';
import { ProductStatus, StockStatus } from '../../core/enums/product-status';
import { Product } from '../../core/models/product';
import { User } from '../../core/models/user';
import { SessionStore } from '../../core/services/session.store';
import { ProductsPage } from './products-page';

const product: Product = {
  id: 100000,
  name: 'Microscope Objective 10x',
  description: null,
  price: 249.9,
  stock: 0,
  categoryId: 2,
  categoryName: 'Objectives',
  isActive: true,
  status: ProductStatus.Active,
  stockStatus: StockStatus.OutOfStock,
  createdAt: '2026-01-01T00:00:00Z',
  updatedAt: '2026-01-01T00:00:00Z',
  updatedByName: null,
  rowVersion: 'AAAAAAAAB9E=',
};

const user = (role: Role, permissions: Permission[]): User => ({
  id: 1,
  name: role,
  email: `${role}@example.com`,
  role,
  permissions,
});

async function render(signedIn: User) {
  TestBed.configureTestingModule({
    imports: [ProductsPage],
    providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
  });
  TestBed.inject(SessionStore).setUser(signedIn);

  const fixture = TestBed.createComponent(ProductsPage);
  fixture.detectChanges();
  await fixture.whenStable();

  const http = TestBed.inject(HttpTestingController);
  http.expectOne((r) => r.url === `${environment.apiUrl}/products`).flush([product]);
  http.expectOne((r) => r.url === `${environment.apiUrl}/categories`).flush([]);
  await fixture.whenStable();
  fixture.detectChanges();

  return fixture.nativeElement as HTMLElement;
}

const filterLabels = (page: HTMLElement) =>
  [...page.querySelectorAll('app-product-filters mat-label')].map((label) =>
    label.textContent?.trim(),
  );

describe('ProductsPage', () => {
  it('shows the products returned by the API with their stock', async () => {
    const page = await render(user(Role.User, [Permission.ChangeStock]));

    expect(page.textContent).toContain('Microscope Objective 10x');
    expect(page.querySelector('.stock-out')?.textContent?.trim()).toBe('0');
  });

  it('hides editing for normal users', async () => {
    const page = await render(user(Role.User, [Permission.ChangeStock]));

    expect(page.textContent).not.toContain('New product');
    expect(filterLabels(page)).not.toContain('Status');
  });

  it('shows editing and the hidden toggle for editors', async () => {
    const page = await render(
      user(Role.Editor, [
        Permission.ChangeStock,
        Permission.Edit,
        Permission.ToggleActive,
        Permission.ViewHidden,
      ]),
    );

    expect(page.textContent).toContain('New product');
    expect(filterLabels(page)).toContain('Status');
  });
});
