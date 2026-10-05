import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { environment } from '../../../environments/environment';
import { Permission, Role } from '../../core/enums/permission';
import { User } from '../../core/models/user';
import { SessionStore } from '../../core/services/session.store';
import { UsersService } from '../../core/services/users.service';
import { Header } from './header';

const admin: User = {
  id: 1,
  name: 'Alex Admin',
  email: 'admin@example.com',
  role: Role.Admin,
  permissions: Object.values(Permission),
};
const sam: User = {
  id: 3,
  name: 'Sam User',
  email: 'sam@example.com',
  role: Role.User,
  permissions: [Permission.ChangeStock],
};

function setup() {
  TestBed.configureTestingModule({
    providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
  });
  TestBed.inject(UsersService).users.set([admin, sam]);
  TestBed.inject(SessionStore).setUser(admin);
  const fixture = TestBed.createComponent(Header);
  fixture.detectChanges();
  return {
    fixture,
    element: fixture.nativeElement as HTMLElement,
    http: TestBed.inject(HttpTestingController),
  };
}

const links = (element: HTMLElement) =>
  [...element.querySelectorAll('nav a')].map((a) => a.textContent?.trim());

describe('Header', () => {
  it('shows Metrics only to users who may see it', () => {
    const { element } = setup();

    expect(links(element).some((l) => l?.includes('Metrics'))).toBe(true);
  });

  it('switching user logs in as that user and updates the navigation', async () => {
    const { fixture, element, http } = setup();

    (fixture.componentInstance as unknown as { selectUser(id: number): void }).selectUser(sam.id);
    const request = http.expectOne(`${environment.apiUrl}/auth/login`);
    expect(request.request.body).toEqual({ email: 'sam@example.com' });
    request.flush(sam);
    await fixture.whenStable();
    fixture.detectChanges();

    expect(TestBed.inject(SessionStore).user()?.id).toBe(sam.id);
    expect(links(element).some((l) => l?.includes('Metrics'))).toBe(false);
  });
});
