import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ProductService } from '../../services/product';

@Component({
  selector: 'app-seller-dashboard',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './seller-dashboard.html',
  styleUrl: './seller-dashboard.css'
})
export class SellerDashboard implements OnInit {
  private productService = inject(ProductService);
  private cdr = inject(ChangeDetectorRef);

  productCount = 0;
  totalStock = 0;
  orderCount = 0;
  isLoadingStats = true;

  ngOnInit(): void {
    this.loadStats();
  }

  loadStats(): void {
    this.isLoadingStats = true;
    this.cdr.markForCheck();

    this.productService.getSellerProducts().subscribe({
      next: (products) => {
        this.productCount = products.length;
        this.totalStock = products.reduce((sum, p) => sum + (p.quantity || 0), 0);
        this.isLoadingStats = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.isLoadingStats = false;
        this.cdr.markForCheck();
      }
    });

    this.productService.getSellerOrders().subscribe({
      next: (orders) => {
        this.orderCount = orders.length;
        this.cdr.markForCheck();
      },
      error: () => {
        this.cdr.markForCheck();
      }
    });
  }
}
