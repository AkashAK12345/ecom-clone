import { Routes } from '@angular/router';
import { buyerGuard, sellerGuard } from './guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'buyer', pathMatch: 'full' },
  {
    path: 'buyer',
    canActivate: [buyerGuard],
    loadComponent: () =>
      import('./pages/buyer/buyer').then((page) => page.Buyer)
  },
  {
    path: 'seller',
    canActivate: [sellerGuard],
    loadComponent: () =>
      import('./pages/seller/seller').then((page) => page.Seller)
  },
  {
    path: 'login',
    loadComponent: () =>
      import('./pages/login/login').then((page) => page.Login)
  },
  {
    path: 'register',
    loadComponent: () =>
      import('./pages/register/register').then((page) => page.Register)
  },
  { path: '**', redirectTo: 'buyer' }
];