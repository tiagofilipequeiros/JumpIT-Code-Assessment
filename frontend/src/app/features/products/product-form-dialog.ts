import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { ErrorCode } from '../../core/enums/error-code';
import { FeedbackMessage } from '../../core/enums/feedback-message';
import { ProductLimits } from '../../core/enums/limits';
import { ApiError } from '../../core/models/api-error';
import { Category } from '../../core/models/category';
import { Product, ProductRequest } from '../../core/models/product';
import { NotificationService } from '../../core/services/notification.service';
import { ProductsService } from '../../core/services/products.service';
import { applyServerErrors } from '../../core/utils/apply-server-errors';
import { errorMessage } from '../../core/utils/form-errors';
import { integer, maxDecimals } from '../../core/utils/validators';

export interface ProductFormData {
  product: Product | null;
  categories: Category[];
}

// Create or edit a product. Closes with the saved product, or 'reload' after a conflict.
@Component({
  selector: 'app-product-form-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatProgressBarModule,
  ],
  templateUrl: './product-form-dialog.html',
})
export class ProductFormDialog {
  private readonly data = inject<ProductFormData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<ProductFormDialog>);
  private readonly productsService = inject(ProductsService);
  private readonly notifications = inject(NotificationService);

  protected readonly limits = ProductLimits;
  protected readonly errorMessage = errorMessage;
  protected readonly product = this.data.product;
  protected readonly saving = signal(false);

  // A product may stay in its current (e.g. disabled) category, but new choices are active ones only.
  protected readonly categories = this.data.categories.filter(
    (c) => (c.isActive && !c.isProtected) || c.id === this.product?.categoryId,
  );

  protected readonly form = new FormGroup({
    name: new FormControl(this.product?.name ?? '', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.minLength(ProductLimits.NameMinLength),
        Validators.maxLength(ProductLimits.NameMaxLength),
      ],
    }),
    description: new FormControl(this.product?.description ?? '', {
      nonNullable: true,
      validators: [Validators.maxLength(ProductLimits.DescriptionMaxLength)],
    }),
    price: new FormControl<number | null>(this.product?.price ?? null, [
      Validators.required,
      Validators.min(0),
      Validators.max(ProductLimits.PriceMax),
      maxDecimals(2),
    ]),
    stock: new FormControl<number | null>(this.product?.stock ?? 0, [
      Validators.required,
      Validators.min(0),
      Validators.max(ProductLimits.StockMax),
      integer,
    ]),
    categoryId: new FormControl<number | null>(this.product?.categoryId ?? null, [
      Validators.required,
    ]),
  });

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const request: ProductRequest = {
      name: value.name.trim(),
      description: value.description.trim() || null,
      price: Number(value.price),
      stock: Number(value.stock),
      categoryId: Number(value.categoryId),
    };

    this.setSaving(true);
    const save$ = this.product
      ? this.productsService.update(this.product.id, {
          ...request,
          rowVersion: this.product.rowVersion,
        })
      : this.productsService.create(request);

    save$.subscribe({
      next: (saved) => {
        this.notifications.success(
          this.product ? FeedbackMessage.ProductUpdated : FeedbackMessage.ProductCreated,
        );
        this.dialogRef.close(saved);
      },
      error: (error: unknown) => {
        this.setSaving(false);
        this.notifications.error(error);
        if (error instanceof ApiError && error.code === ErrorCode.ConcurrencyConflict) {
          this.dialogRef.close('reload');
          return;
        }
        applyServerErrors(this.form, error);
      },
    });
  }

  // While saving, the dialog can't be closed: the page must hear about the result to refresh the list.
  private setSaving(saving: boolean): void {
    this.saving.set(saving);
    this.dialogRef.disableClose = saving;
  }
}
