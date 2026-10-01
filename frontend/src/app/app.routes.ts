import { Routes } from '@angular/router';
import { Permission } from './core/enums/permission';
import { permissionGuard } from './core/guards/permission.guard';

export const routes: Routes = [
  {
    path: '',
    title: 'Products',
    loadComponent: () => import('./features/products/products-page').then((m) => m.ProductsPage),
  },
  {
    path: 'categories',
    title: 'Categories',
    loadComponent: () => import('./features/categories/categories-page').then((m) => m.CategoriesPage),
  },
  {
    path: 'metrics',
    title: 'Metrics',
    canActivate: [permissionGuard(Permission.ViewMetrics)],
    loadComponent: () => import('./features/metrics/metrics-page').then((m) => m.MetricsPage),
  },
  { path: '**', redirectTo: '' },
];
