import { Component } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

// Placeholder: user actions are already recorded in UserMetrics; this page will show them later.
@Component({
  selector: 'app-metrics-page',
  imports: [MatIconModule],
  template: `
    <h1 class="page-title mb-3">Metrics</h1>
    <div class="table-card p-4 p-md-5 text-center">
      <mat-icon class="placeholder-icon">monitoring</mat-icon>
      <h2 class="h5 mt-2">Coming soon</h2>
      <p class="text-muted mb-0">
        Logins and product/category changes are already being recorded. Charts and reports will appear here.
      </p>
    </div>
  `,
  styles: `
    .placeholder-icon {
      font-size: 48px;
      width: 48px;
      height: 48px;
      color: var(--mat-sys-primary);
    }
  `,
})
export class MetricsPage {}
