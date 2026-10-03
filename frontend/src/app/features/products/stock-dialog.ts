import { Component, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { Observable } from 'rxjs';
import { FeedbackMessage } from '../../core/enums/feedback-message';
import { ProductLimits } from '../../core/enums/limits';
import { Product } from '../../core/models/product';
import { NotificationService } from '../../core/services/notification.service';
import { ProductsService } from '../../core/services/products.service';
import { errorMessage } from '../../core/utils/form-errors';
import { integer } from '../../core/utils/validators';

// Add or remove stock. Closes with the updated product.
@Component({
  selector: 'app-stock-dialog',
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule],
  template: `
    <h2 mat-dialog-title>Adjust stock</h2>
    <mat-dialog-content>
      <p>
        <strong>{{ product.name }}</strong><br />
        Current stock: <strong>{{ product.stock }}</strong>
      </p>
      <mat-form-field class="w-100">
        <mat-label>Quantity</mat-label>
        <input matInput type="number" [formControl]="quantity" name="quantity" autocomplete="off" min="1" step="1" inputmode="numeric" cdkFocusInitial />
        <mat-error>{{ errorMessage(quantity) }}</mat-error>
      </mat-form-field>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-stroked-button (click)="change(-1)" [disabled]="saving()">
        <mat-icon>remove</mat-icon> Remove
      </button>
      <button mat-flat-button (click)="change(1)" [disabled]="saving()">
        <mat-icon>add</mat-icon> Add
      </button>
    </mat-dialog-actions>
  `,
})
export class StockDialog {
  protected readonly product = inject<Product>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<StockDialog>);
  private readonly productsService = inject(ProductsService);
  private readonly notifications = inject(NotificationService);

  protected readonly errorMessage = errorMessage;
  protected readonly saving = signal(false);
  protected readonly quantity = new FormControl<number | null>(1, [
    Validators.required,
    Validators.min(ProductLimits.QuantityMin),
    Validators.max(ProductLimits.QuantityMax),
    integer,
  ]);

  protected change(direction: 1 | -1): void {
    if (this.quantity.invalid) {
      this.quantity.markAsTouched();
      return;
    }

    const quantity = Number(this.quantity.value);
    const request$: Observable<Product> =
      direction > 0
        ? this.productsService.addToStock(this.product.id, quantity)
        : this.productsService.decrementStock(this.product.id, quantity);

    this.saving.set(true);
    request$.subscribe({
      next: (updated) => {
        this.notifications.success(direction > 0 ? FeedbackMessage.StockAdded : FeedbackMessage.StockRemoved);
        this.dialogRef.close(updated);
      },
      error: (error) => {
        this.saving.set(false);
        this.notifications.error(error);
      },
    });
  }
}
