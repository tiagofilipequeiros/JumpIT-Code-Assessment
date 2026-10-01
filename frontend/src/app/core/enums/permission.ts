// Same values as the backend Permission enum. The API sends each user's permissions on login.
export enum Permission {
  ChangeStock = 'ChangeStock',
  Edit = 'Edit',
  ToggleActive = 'ToggleActive',
  ViewHidden = 'ViewHidden',
  Delete = 'Delete',
  ViewMetrics = 'ViewMetrics',
}

export enum Role {
  User = 'User',
  Editor = 'Editor',
  Admin = 'Admin',
}
