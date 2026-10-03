import { Component, ElementRef, viewChild } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Header } from './layout/header/header';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Header],
  template: `
    <a class="skip-link" href="#main" (click)="skipToMain($event)">Skip to main content</a>
    <app-header />
    <main #main id="main" tabindex="-1" class="container-xl py-3 py-md-4">
      <router-outlet />
    </main>
  `,
})
export class App {
  private readonly main = viewChild.required<ElementRef<HTMLElement>>('main');

  // Focus the main area directly: a plain #main link would make the router navigate to the home page.
  protected skipToMain(event: Event): void {
    event.preventDefault();
    this.main().nativeElement.focus();
  }
}
