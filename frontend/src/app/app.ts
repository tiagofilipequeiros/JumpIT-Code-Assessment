import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Header } from './layout/header/header';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Header],
  template: `
    <app-header />
    <main class="container-xl py-3 py-md-4">
      <router-outlet />
    </main>
  `,
})
export class App {}
