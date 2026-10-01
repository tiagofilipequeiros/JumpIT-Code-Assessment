import { Permission, Role } from '../enums/permission';

export interface User {
  id: number;
  name: string;
  email: string;
  role: Role;
  permissions: Permission[];
}
