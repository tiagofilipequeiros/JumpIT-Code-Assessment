import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { Product, ProductVersion } from '../../core/models/product';
import { NotificationService } from '../../core/services/notification.service';
import { ProductsService } from '../../core/services/products.service';

// Every version of a product (from the SQL Server temporal table), newest first.
@Component({
  selector: 'app-history-dialog',
  imports: [MatDialogModule, MatTableModule, MatButtonModule, MatProgressBarModule, DatePipe, CurrencyPipe],
  template: `
    <h2 mat-dialog-title>History · {{ product.name }}</h2>
    <mat-dialog-content>
      @if (loading()) {
        <mat-progress-bar mode="indeterminate" />
      }
      <div class="table-responsive">
        <table mat-table [dataSource]="versions()" class="w-100">
          <ng-container matColumnDef="validFrom">
            <th mat-header-cell *matHeaderCellDef>From</th>
            <td mat-cell *matCellDef="let v">{{ v.validFrom | date: 'short' }}</td>
          </ng-container>
          <ng-container matColumnDef="price">
            <th mat-header-cell *matHeaderCellDef class="text-end">Price</th>
            <td mat-cell *matCellDef="let v" class="text-end">{{ v.price | currency: 'EUR' }}</td>
          </ng-container>
          <ng-container matColumnDef="stock">
            <th mat-header-cell *matHeaderCellDef class="text-end">Stock</th>
            <td mat-cell *matCellDef="let v" class="text-end">{{ v.stock }}</td>
          </ng-container>
          <ng-container matColumnDef="status">
            <th mat-header-cell *matHeaderCellDef>Status</th>
            <td mat-cell *matCellDef="let v">{{ v.isActive ? 'Active' : 'Disabled' }}</td>
          </ng-container>
          <ng-container matColumnDef="by">
            <th mat-header-cell *matHeaderCellDef>By</th>
            <td mat-cell *matCellDef="let v">{{ v.updatedByName ?? 'Seed data' }}</td>
          </ng-container>
          <tr mat-header-row *matHeaderRowDef="columns"></tr>
          <tr mat-row *matRowDef="let row; columns: columns"></tr>
        </table>
      </div>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Close</button>
    </mat-dialog-actions>
  `,
})
export class HistoryDialog {
  protected readonly product = inject<Product>(MAT_DIALOG_DATA);
  private readonly productsService = inject(ProductsService);
  private readonly notifications = inject(NotificationService);

  protected readonly columns = ['validFrom', 'price', 'stock', 'status', 'by'];
  protected readonly versions = signal<ProductVersion[]>([]);
  protected readonly loading = signal(true);

  constructor() {
    this.productsService.getHistory(this.product.id).subscribe({
      next: (versions) => {
        this.versions.set([...versions].reverse());
        this.loading.set(false);
      },
      error: (error) => {
        this.loading.set(false);
        this.notifications.error(error);
      },
    });
  }
}
