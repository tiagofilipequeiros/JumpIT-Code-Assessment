import { ERROR_MESSAGES, ErrorCode } from './error-code';

describe('ErrorCode', () => {
  it('has a user-facing message for every code', () => {
    for (const code of Object.values(ErrorCode)) {
      expect(ERROR_MESSAGES[code]).toBeTruthy();
    }
  });
});
