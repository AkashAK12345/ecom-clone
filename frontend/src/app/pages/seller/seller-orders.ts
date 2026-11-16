import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ProductService, OrderHistoryItem } from '../../services/product';

@Component({
  selector: 'app-seller-orders',
  standalone: true,
  imports: [DatePipe, RouterLink],
  templateUrl: './seller-orders.html',
  styleUrl: './seller-orders.css'
})
export class SellerOrders implements OnInit {
  private productService = inject(ProductService);
  private cdr = inject(ChangeDetectorRef);

  sellerOrders: OrderHistoryItem[] = [];
  loadingOrders = true;
  ordersErrorMessage = '';

  ngOnInit(): void {
    this.loadSellerOrders();
  }

  loadSellerOrders(): void {
    this.loadingOrders = true;
    this.ordersErrorMessage = '';
    this.cdr.markForCheck();

    this.productService.getSellerOrders().subscribe({
      next: (orders) => {
        this.loadingOrders = false;
        this.sellerOrders = orders;
        this.cdr.markForCheck();
      },
      error: () => {
        this.loadingOrders = false;
        this.ordersErrorMessage = 'Could not load orders for your products.';
        this.cdr.markForCheck();
      }
    });
  }
}
