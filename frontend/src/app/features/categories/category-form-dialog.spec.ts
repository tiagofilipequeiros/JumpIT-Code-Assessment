import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { environment } from '../../../environments/environment';
import { apiErrorInterceptor } from '../../core/interceptors/api-error.interceptor';
import { CategoryFormDialog } from './category-form-dialog';

function setup() {
  const dialogRef = { close: vi.fn(), disableClose: false };
  TestBed.configureTestingModule({
    imports: [CategoryFormDialog],
    providers: [
      provideHttpClient(withInterceptors([apiErrorInterceptor])),
      provideHttpClientTesting(),
      { provide: MAT_DIALOG_DATA, useValue: null },
      { provide: MatDialogRef, useValue: dialogRef },
    ],
  });
  const fixture = TestBed.createComponent(CategoryFormDialog);
  fixture.detectChanges();
  const element = fixture.nativeElement as HTMLElement;
  const input = element.querySelector<HTMLInputElement>('input[name="name"]')!;
  const submit = () => element.querySelector<HTMLButtonElement>('button[type="submit"]')!.click();
  return {
    fixture,
    element,
    dialogRef,
    input,
    submit,
    http: TestBed.inject(HttpTestingController),
  };
}

describe('CategoryFormDialog', () => {
  it('saves through the API when the form is submitted (no page reload)', () => {
    const { input, submit, http, dialogRef } = setup();
    input.value = 'Filters';
    input.dispatchEvent(new Event('input'));

    submit();

    const request = http.expectOne(`${environment.apiUrl}/categories`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ name: 'Filters' });
    expect(dialogRef.disableClose).toBe(true);

    request.flush({ id: 9, name: 'Filters' });
    expect(dialogRef.close).toHaveBeenCalledWith({ id: 9, name: 'Filters' });
  });

  it('does not call the API when the name is too short', () => {
    const { input, submit, http } = setup();
    input.value = 'F';
    input.dispatchEvent(new Event('input'));

    submit();

    http.expectNone(`${environment.apiUrl}/categories`);
  });

  it('shows "name taken" on the field and can be closed again', () => {
    const { fixture, element, input, submit, http, dialogRef } = setup();
    input.value = 'Objectives';
    input.dispatchEvent(new Event('input'));

    submit();
    http
      .expectOne(`${environment.apiUrl}/categories`)
      .flush({ code: 'CategoryNameTaken' }, { status: 409, statusText: 'Conflict' });
    fixture.detectChanges();

    expect(element.querySelector('mat-error')?.textContent).toContain('already exists');
    expect(dialogRef.disableClose).toBe(false);
    expect(dialogRef.close).not.toHaveBeenCalled();
  });
});
