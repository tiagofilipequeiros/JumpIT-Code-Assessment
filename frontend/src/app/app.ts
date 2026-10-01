import { Component, inject, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { HealthService } from './health.service';

@Component({
  imports: [MatCardModule],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  protected readonly backendStatus = signal('loading...');

  constructor() {
    inject(HealthService).getHealth().subscribe({
      next: (response) => this.backendStatus.set(response.status),
      error: () => this.backendStatus.set('unavailable'),
    });
  }
}
