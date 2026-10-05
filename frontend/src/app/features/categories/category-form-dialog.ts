import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { ErrorCode } from '../../core/enums/error-code';
import { FeedbackMessage } from '../../core/enums/feedback-message';
import { CategoryLimits } from '../../core/enums/limits';
import { ApiError } from '../../core/models/api-error';
import { Category } from '../../core/models/category';
import { CategoriesService } from '../../core/services/categories.service';
import { NotificationService } from '../../core/services/notification.service';
import { errorMessage } from '../../core/utils/form-errors';

// Create or rename a category. Closes with the saved category, or 'reload' after a conflict.
@Component({
  selector: 'app-category-form-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
  ],
  template: `
    <h2 mat-dialog-title>{{ category ? 'Rename category' : 'New category' }}</h2>
    <form [formGroup]="form" (ngSubmit)="save()" novalidate>
      <mat-dialog-content>
        <mat-form-field class="w-100">
          <mat-label>Name</mat-label>
          <input
            matInput
            formControlName="name"
            name="name"
            autocomplete="off"
            [maxlength]="limits.NameMaxLength"
            cdkFocusInitial
          />
          <mat-hint align="end">{{ name.value.length }} / {{ limits.NameMaxLength }}</mat-hint>
          <mat-error>{{ errorMessage(name) }}</mat-error>
        </mat-form-field>
      </mat-dialog-content>
      <mat-dialog-actions align="end">
        <button mat-button type="button" mat-dialog-close>Cancel</button>
        <button mat-flat-button type="submit" [disabled]="saving()">Save</button>
      </mat-dialog-actions>
    </form>
  `,
})
export class CategoryFormDialog {
  protected readonly category = inject<Category | null>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<CategoryFormDialog>);
  private readonly categoriesService = inject(CategoriesService);
  private readonly notifications = inject(NotificationService);

  protected readonly limits = CategoryLimits;
  protected readonly errorMessage = errorMessage;
  protected readonly saving = signal(false);
  // A FormGroup bound with [formGroup], so (ngSubmit) fires instead of a native page submit.
  protected readonly form = new FormGroup({
    name: new FormControl(this.category?.name ?? '', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.minLength(CategoryLimits.NameMinLength),
        Validators.maxLength(CategoryLimits.NameMaxLength),
      ],
    }),
  });
  protected readonly name = this.form.controls.name;

  protected save(): void {
    if (this.name.invalid) {
      this.name.markAsTouched();
      return;
    }

    const name = this.name.value.trim();
    const save$ = this.category
      ? this.categoriesService.update(this.category.id, name, this.category.rowVersion)
      : this.categoriesService.create(name);

    this.setSaving(true);
    save$.subscribe({
      next: (saved) => {
        this.notifications.success(
          this.category ? FeedbackMessage.CategoryUpdated : FeedbackMessage.CategoryCreated,
        );
        this.dialogRef.close(saved);
      },
      error: (error: unknown) => {
        this.setSaving(false);
        this.notifications.error(error);
        if (error instanceof ApiError && error.code === ErrorCode.ConcurrencyConflict) {
          this.dialogRef.close('reload');
        } else if (error instanceof ApiError && error.code === ErrorCode.CategoryNameTaken) {
          this.name.setErrors({ server: error.message });
        }
      },
    });
  }

  // While saving, the dialog can't be closed: the page must hear about the result to refresh the list.
  private setSaving(saving: boolean): void {
    this.saving.set(saving);
    this.dialogRef.disableClose = saving;
  }
}
