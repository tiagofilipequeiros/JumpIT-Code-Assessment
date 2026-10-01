import { FormControl, Validators } from '@angular/forms';
import { errorMessage } from './form-errors';
import { integer, maxDecimals } from './validators';

describe('form errors and validators', () => {
  it('returns no message for a valid control', () => {
    expect(errorMessage(new FormControl('ok', Validators.required))).toBe('');
  });

  it('describes length errors with the limit', () => {
    const control = new FormControl('a', Validators.minLength(2));

    expect(errorMessage(control)).toBe('Use at least 2 characters.');
  });

  it('shows server messages as they are', () => {
    const control = new FormControl('x');
    control.setErrors({ server: 'Name already exists.' });

    expect(errorMessage(control)).toBe('Name already exists.');
  });

  it('rejects more than 2 decimal places', () => {
    expect(maxDecimals(2)(new FormControl(1.999))).toEqual({ decimals: { max: 2 } });
    expect(maxDecimals(2)(new FormControl(1.99))).toBeNull();
  });

  it('accepts only whole numbers for integer fields', () => {
    expect(integer(new FormControl(2.5))).toEqual({ integer: true });
    expect(integer(new FormControl(3))).toBeNull();
  });
});
