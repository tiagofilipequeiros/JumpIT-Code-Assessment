import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component, input } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { environment } from '../../../environments/environment';
import { Permission, Role } from '../../core/enums/permission';
import { ProductMetrics } from '../../core/models/metrics';
import { SessionStore } from '../../core/services/session.store';
import { Chart } from '../../shared/chart/chart';
import { ChartConfig } from '../../shared/chart/chart-config';
import { MetricsPage } from './metrics-page';

// Stands in for <app-chart>: the test checks the page, not ApexCharts drawing SVG.
@Component({ selector: 'app-chart', template: '<h2>{{ title() }}</h2>' })
class ChartStub {
  readonly title = input('');
  readonly subtitle = input<string>();
  readonly config = input<ChartConfig | null>(null);
  readonly loading = input(false);
  readonly emptyMessage = input('');
}

const metrics: ProductMetrics = {
  kpis: { inventoryValue: 1000, outOfStock: 2, lowStock: 1, unitsAdded: 10, unitsRemoved: 25 },
  movementsPerDay: [],
  topRemoved: [{ productId: 100000, name: 'Lens', units: 25 }],
};

async function render(role: Role, permissions: Permission[]) {
  TestBed.configureTestingModule({
    providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
  });
  TestBed.overrideComponent(MetricsPage, {
    remove: { imports: [Chart] },
    add: { imports: [ChartStub] },
  });
  TestBed.inject(SessionStore).setUser({
    id: 1,
    name: role,
    email: 'x@example.com',
    role,
    permissions,
  });

  const fixture = TestBed.createComponent(MetricsPage);
  fixture.detectChanges();
  await fixture.whenStable();

  const http = TestBed.inject(HttpTestingController);
  http.expectOne((r) => r.url === `${environment.apiUrl}/products`).flush([]);
  http.expectOne((r) => r.url === `${environment.apiUrl}/metrics/products`).flush(metrics);
  await fixture.whenStable();
  fixture.detectChanges();
  // The first visit selects the top product for the stock chart.
  http.expectOne((r) => r.url === `${environment.apiUrl}/metrics/products/stock-history`).flush([]);
  await fixture.whenStable();
  fixture.detectChanges();

  return fixture.nativeElement as HTMLElement;
}

describe('MetricsPage', () => {
  it('leaves the page when the new user may not see metrics', async () => {
    const page = await render(Role.Admin, [
      Permission.ViewProductMetrics,
      Permission.ViewUserMetrics,
    ]);
    const router = TestBed.inject(Router);
    const navigate = vi.spyOn(router, 'navigate');

    TestBed.inject(SessionStore).setUser({
      id: 3,
      name: 'Sam',
      email: 'sam@example.com',
      role: Role.User,
      permissions: [Permission.ChangeStock],
    });
    TestBed.tick();

    expect(navigate).toHaveBeenCalledWith(['/']);
    expect(page).toBeTruthy();
  });

  it('shows product KPIs and charts', async () => {
    const page = await render(Role.Editor, [Permission.ViewProductMetrics]);

    expect(page.textContent).toContain('Out of stock');
    expect(page.textContent).toContain('Stock level over time');
    expect(page.textContent).toContain('25');
  });

  it('hides the Users view from editors (personal activity is admin only)', async () => {
    const page = await render(Role.Editor, [Permission.ViewProductMetrics]);

    expect(page.textContent).not.toContain('Users');
  });

  it('offers the Users view to admins', async () => {
    const page = await render(Role.Admin, [
      Permission.ViewProductMetrics,
      Permission.ViewUserMetrics,
    ]);

    expect(page.textContent).toContain('Users');
  });
});
