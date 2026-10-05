import { TestBed } from '@angular/core/testing';
import { Permission, Role } from '../enums/permission';
import { User } from '../models/user';
import { SessionStore } from './session.store';

const editor: User = {
  id: 2,
  name: 'Erin Editor',
  email: 'editor@example.com',
  role: Role.Editor,
  permissions: [
    Permission.ChangeStock,
    Permission.Edit,
    Permission.ToggleActive,
    Permission.ViewHidden,
  ],
};

describe('SessionStore', () => {
  it('has no permissions before a user is selected', () => {
    const session = TestBed.inject(SessionStore);

    expect(session.can(Permission.ChangeStock)).toBe(false);
  });

  it('uses the permissions sent by the API', () => {
    const session = TestBed.inject(SessionStore);
    session.setUser(editor);

    expect(session.can(Permission.Edit)).toBe(true);
    expect(session.can(Permission.Delete)).toBe(false);
  });
});
