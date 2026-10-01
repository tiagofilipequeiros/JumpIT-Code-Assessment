import { FormGroup } from '@angular/forms';
import { ApiError } from '../models/api-error';

// Shows the API's validation messages under the matching form fields.
export function applyServerErrors(form: FormGroup, error: unknown): boolean {
  if (!(error instanceof ApiError)) {
    return false;
  }

  let applied = false;
  for (const [field, messages] of Object.entries(error.fieldErrors)) {
    const control = form.get(field);
    if (control) {
      control.setErrors({ server: messages[0] });
      control.markAsTouched();
      applied = true;
    }
  }
  return applied;
}
