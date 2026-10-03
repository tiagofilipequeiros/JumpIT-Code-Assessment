import { Component, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

// A single headline number with its label. Usage: <app-stat-tile label="Out of stock" [value]="2" hint="Active products" />
@Component({
  selector: 'app-stat-tile',
  imports: [MatIconModule],
  template: `
    <div class="tile">
      <div class="tile-label">
        @if (icon()) {
          <mat-icon aria-hidden="true">{{ icon() }}</mat-icon>
        }
        {{ label() }}
      </div>
      <div class="tile-value">{{ value() ?? '–' }}</div>
      @if (hint()) {
        <div class="tile-hint">{{ hint() }}</div>
      }
    </div>
  `,
  styles: `
    :host {
      display: block;
      height: 100%;
    }
    .tile {
      height: 100%;
      padding: 16px 20px;
      background: var(--mat-sys-surface);
      border: 1px solid var(--mat-sys-outline-variant);
      border-radius: 12px;
    }
    .tile-label {
      display: flex;
      align-items: center;
      gap: 6px;
      font: var(--mat-sys-label-large);
      color: var(--mat-sys-on-surface-variant);
    }
    .tile-label mat-icon {
      font-size: 18px;
      width: 18px;
      height: 18px;
    }
    .tile-value {
      margin-top: 6px;
      font: var(--mat-sys-headline-medium);
      color: var(--mat-sys-on-surface);
    }
    .tile-hint {
      margin-top: 2px;
      font: var(--mat-sys-body-small);
      color: var(--mat-sys-on-surface-variant);
    }
  `,
})
export class StatTile {
  readonly label = input.required<string>();
  readonly value = input<string | number | null>(null);
  readonly hint = input<string>();
  readonly icon = input<string>();
}
