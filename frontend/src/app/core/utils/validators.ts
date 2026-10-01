import { ValidatorFn } from '@angular/forms';

export const integer: ValidatorFn = (control) =>
  control.value === null || control.value === '' || Number.isInteger(Number(control.value)) ? null : { integer: true };

export const maxDecimals =
  (max: number): ValidatorFn =>
  (control) => {
    if (control.value === null || control.value === '') {
      return null;
    }
    const decimals = String(control.value).split('.')[1]?.length ?? 0;
    return decimals <= max ? null : { decimals: { max } };
  };
