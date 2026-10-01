import { Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTableModule } from '@angular/material/table';
import { filter, switchMap } from 'rxjs';
import { FeedbackMessage } from '../../core/enums/feedback-message';
import { Permission } from '../../core/enums/permission';
import { Category } from '../../core/models/category';
import { CategoriesService } from '../../core/services/categories.service';
import { NotificationService } from '../../core/services/notification.service';
import { SessionStore } from '../../core/services/session.store';
import { ConfirmDialog, ConfirmDialogData } from '../../shared/confirm-dialog/confirm-dialog';
import { CategoryFormDialog } from './category-form-dialog';

@Component({
  selector: 'app-categories-page',
  imports: [MatTableModule, MatButtonModule, MatIconModule, MatMenuModule, MatProgressBarModule, MatSlideToggleModule],
  templateUrl: './categories-page.html',
})
export class CategoriesPage {
  private readonly categoriesService = inject(CategoriesService);
  private readonly notifications = inject(NotificationService);
  private readonly dialog = inject(MatDialog);
  private readonly session = inject(SessionStore);

  protected readonly categories = signal<Category[]>([]);
  protected readonly loading = signal(false);
  protected readonly includeHidden = signal(false);

  protected readonly canEdit = computed(() => this.session.can(Permission.Edit));
  protected readonly canToggle = computed(() => this.session.can(Permission.ToggleActive));
  protected readonly canDelete = computed(() => this.session.can(Permission.Delete));
  protected readonly canViewHidden = computed(() => this.session.can(Permission.ViewHidden));
  protected readonly hasActions = computed(() => this.canEdit() || this.canToggle() || this.canDelete());
  protected readonly columns = computed(() => [
    'name',
    'products',
    ...(this.canViewHidden() ? ['status'] : []),
    ...(this.hasActions() ? ['actions'] : []),
  ]);

  constructor() {
    effect(() => {
      this.session.user();
      untracked(() => {
        if (!this.canViewHidden()) {
          this.includeHidden.set(false);
        }
        this.load();
      });
    });
  }

  protected load(): void {
    this.loading.set(true);
    this.categoriesService.getAll(this.includeHidden()).subscribe({
      next: (categories) => {
        this.categories.set(categories);
        this.loading.set(false);
      },
      error: (error) => {
        this.loading.set(false);
        this.notifications.error(error);
      },
    });
  }

  protected setIncludeHidden(value: boolean): void {
    this.includeHidden.set(value);
    this.load();
  }

  protected openForm(category: Category | null = null): void {
    this.dialog
      .open(CategoryFormDialog, { data: category, width: '420px', maxWidth: '95vw' })
      .afterClosed()
      .pipe(filter(Boolean))
      .subscribe(() => this.load());
  }

  protected toggleActive(category: Category): void {
    const isActive = !category.isActive;
    this.categoriesService.setActive(category.id, isActive).subscribe({
      next: () => {
        this.notifications.success(isActive ? FeedbackMessage.CategoryEnabled : FeedbackMessage.CategoryDisabled);
        this.load();
      },
      error: (error) => this.notifications.error(error),
    });
  }

  protected delete(category: Category): void {
    const impact =
      category.productCount > 0
        ? `Its ${category.productCount} product(s) will be moved to Uncategorized and hidden from normal users.`
        : 'It has no products.';
    const data: ConfirmDialogData = {
      title: 'Delete category?',
      message: `"${category.name}" will be permanently deleted. ${impact}`,
      confirmLabel: 'Delete',
      destructive: true,
    };

    this.dialog
      .open(ConfirmDialog, { data, maxWidth: '95vw' })
      .afterClosed()
      .pipe(
        filter(Boolean),
        switchMap(() => this.categoriesService.delete(category.id)),
      )
      .subscribe({
        next: () => {
          this.notifications.success(FeedbackMessage.CategoryDeleted);
          this.load();
        },
        error: (error) => this.notifications.error(error),
      });
  }
}
