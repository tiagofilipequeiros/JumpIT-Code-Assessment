import { Component, computed, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatSelectModule } from '@angular/material/select';
import { MatToolbarModule } from '@angular/material/toolbar';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { FeedbackMessage } from '../../core/enums/feedback-message';
import { Permission } from '../../core/enums/permission';
import { NotificationService } from '../../core/services/notification.service';
import { SessionStore } from '../../core/services/session.store';
import { UsersService } from '../../core/services/users.service';

interface NavLink {
  path: string;
  label: string;
  icon: string;
  permission?: Permission;
}

const LINKS: NavLink[] = [
  { path: '/', label: 'Products', icon: 'inventory_2' },
  { path: '/categories', label: 'Categories', icon: 'category' },
  {
    path: '/metrics',
    label: 'Metrics',
    icon: 'monitoring',
    permission: Permission.ViewProductMetrics,
  },
];

@Component({
  selector: 'app-header',
  imports: [
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatSelectModule,
    MatFormFieldModule,
    RouterLink,
    RouterLinkActive,
  ],
  templateUrl: './header.html',
  styleUrl: './header.css',
})
export class Header {
  private readonly session = inject(SessionStore);
  private readonly usersService = inject(UsersService);
  private readonly notifications = inject(NotificationService);

  protected readonly users = this.usersService.users;
  protected readonly currentUser = this.session.user;
  protected readonly links = computed(() =>
    LINKS.filter((link) => !link.permission || this.session.can(link.permission)),
  );

  protected selectUser(userId: number): void {
    const user = this.users().find((u) => u.id === userId);
    if (!user) {
      return;
    }

    this.usersService.login(user.email).subscribe({
      next: (signedIn) =>
        this.notifications.success(
          FeedbackMessage.SignedIn,
          `${signedIn.name} (${signedIn.role}).`,
        ),
      error: (error) => this.notifications.error(error),
    });
  }
}
