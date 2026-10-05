import { AbstractControl } from '@angular/forms';

// Validator details, e.g. { requiredLength: 2 } for minlength; a plain message for "server".
type ErrorDetail = Record<string, unknown> | string;

const detail = (error: ErrorDetail, key: string) =>
  typeof error === 'string' ? '' : String(error[key]);

// One place for form error messages. "server" holds messages returned by the API.
const MESSAGES: Record<string, (error: ErrorDetail) => string> = {
  required: () => 'This field is required.',
  minlength: (e) => `Use at least ${detail(e, 'requiredLength')} characters.`,
  maxlength: (e) => `Use at most ${detail(e, 'requiredLength')} characters.`,
  min: (e) => `Must be at least ${detail(e, 'min')}.`,
  max: (e) => `Must be at most ${detail(e, 'max')}.`,
  integer: () => 'Must be a whole number.',
  decimals: (e) => `Use at most ${detail(e, 'max')} decimal places.`,
  server: (e) => String(e),
};

export function errorMessage(control: AbstractControl | null): string {
  const errors = control?.errors;
  if (!errors) {
    return '';
  }

  const [key, value] = Object.entries(errors)[0];
  return MESSAGES[key]?.(value) ?? 'Invalid value.';
}
