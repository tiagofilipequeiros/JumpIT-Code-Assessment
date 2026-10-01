import { AbstractControl } from '@angular/forms';

// One place for form error messages. "server" holds messages returned by the API.
const MESSAGES: Record<string, (error: any) => string> = {
  required: () => 'This field is required.',
  minlength: (e) => `Use at least ${e.requiredLength} characters.`,
  maxlength: (e) => `Use at most ${e.requiredLength} characters.`,
  min: (e) => `Must be at least ${e.min}.`,
  max: (e) => `Must be at most ${e.max}.`,
  integer: () => 'Must be a whole number.',
  decimals: (e) => `Use at most ${e.max} decimal places.`,
  server: (e) => e,
};

export function errorMessage(control: AbstractControl | null): string {
  const errors = control?.errors;
  if (!errors) {
    return '';
  }

  const [key, value] = Object.entries(errors)[0];
  return MESSAGES[key]?.(value) ?? 'Invalid value.';
}
