import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth';

export const sellerGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isLoggedIn()) {
    return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
  }

  if (authService.getRole() !== 'Seller') {
    return router.createUrlTree(['/buyer']);
  }

  return true;
};

export const buyerGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Sellers should be redirected to the seller dashboard
  if (authService.isLoggedIn() && authService.getRole() === 'Seller') {
    return router.createUrlTree(['/seller']);
  }

  // Guests and buyers are welcome to browse products
  return true;
};

