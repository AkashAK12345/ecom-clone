import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'buyer', pathMatch: 'full' },
  {
    path: 'buyer',
    loadComponent: () =>
      import('./pages/buyer/buyer').then((page) => page.Buyer)
  },
  {
    path: 'seller',
    loadComponent: () =>
      import('./pages/seller/seller').then((page) => page.Seller)
  },
  { path: '**', redirectTo: 'buyer' }
];