import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { Product, ProductService, Review } from '../../services/product';

@Component({
  selector: 'app-seller-products',
  standalone: true,
  imports: [DatePipe, RouterLink],
  templateUrl: './seller-products.html',
  styleUrl: './seller-products.css'
})
export class SellerProducts implements OnInit {
  private productService = inject(ProductService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  products: Product[] = [];
  isLoading = true;
  message = '';

  expandedReviews: { [productId: number]: boolean } = {};
  productReviews: { [productId: number]: Review[] } = {};
  loadingReviews: { [productId: number]: boolean } = {};
  reviewError: { [productId: number]: string } = {};

  ngOnInit(): void {
    this.loadProducts();
  }

  loadProducts(): void {
    this.isLoading = true;
    this.message = '';
    this.cdr.markForCheck();

    this.productService.getSellerProducts().subscribe({
      next: (products) => {
        this.products = products;
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.isLoading = false;
        if (err?.status === 401 || err?.status === 403) {
          this.message = 'Please log in with a Seller account to view your products.';
        } else {
          this.message = 'Could not load products.';
        }
        this.cdr.markForCheck();
      }
    });
  }

  getImageUrl(imagePath: string | null | undefined): string {
    return this.productService.getImageUrl(imagePath);
  }

  editProduct(product: Product): void {
    this.router.navigate(['/seller/add-product'], { queryParams: { edit: product.id } });
  }

  toggleReviews(productId: number): void {
    this.expandedReviews[productId] = !this.expandedReviews[productId];
    if (this.expandedReviews[productId] && !this.productReviews[productId]) {
      this.loadReviews(productId);
    }
    this.cdr.markForCheck();
  }

  loadReviews(productId: number): void {
    this.loadingReviews[productId] = true;
    this.reviewError[productId] = '';
    this.productService.getSellerProductReviews(productId).subscribe({
      next: (reviews) => {
        this.loadingReviews[productId] = false;
        this.productReviews[productId] = reviews;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.loadingReviews[productId] = false;
        if (err?.status === 403) {
          this.reviewError[productId] = 'You do not have permission to view reviews for this product.';
        } else {
          this.reviewError[productId] = 'Could not load reviews.';
        }
        this.cdr.markForCheck();
      }
    });
  }

  getStars(rating: number): string {
    const full = Math.min(Math.max(rating, 1), 5);
    return '★'.repeat(full) + '☆'.repeat(5 - full);
  }
}

