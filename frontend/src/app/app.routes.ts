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
      import('./pages/seller/seller-layout').then((m) => m.SellerLayout),
    children: [
      {
        path: '',
        pathMatch: 'full',
        canActivate: [sellerGuard],
        loadComponent: () =>
          import('./pages/seller/seller-dashboard').then((m) => m.SellerDashboard)
      },
      {
        path: 'add-product',
        canActivate: [sellerGuard],
        loadComponent: () =>
          import('./pages/seller/seller-add-product').then((m) => m.SellerAddProduct)
      },
      {
        path: 'products',
        canActivate: [sellerGuard],
        loadComponent: () =>
          import('./pages/seller/seller-products').then((m) => m.SellerProducts)
      },
      {
        path: 'orders',
        canActivate: [sellerGuard],
        loadComponent: () =>
          import('./pages/seller/seller-orders').then((m) => m.SellerOrders)
      },
      { path: '**', redirectTo: '' }
    ]
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
