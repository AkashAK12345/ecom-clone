import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { Product, ProductService, Review, OrderHistoryItem } from '../../services/product';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-buyer',
  standalone: true,
  imports: [FormsModule, DatePipe],
  templateUrl: './buyer.html',
  styleUrl: './buyer.css'
})
export class Buyer implements OnInit {
  private productService = inject(ProductService);
  public authService = inject(AuthService);
  private cdr = inject(ChangeDetectorRef);

  products: Product[] = [];
  categoryFilter = '';
  errorMessage = '';
  orderMessage = '';
  orderErrorMessage = '';
  orderQuantities: { [productId: number]: number } = {};
  selectedSizes: { [productId: number]: string } = {};
  isOrderingProductId: number | null = null;

  myOrders: OrderHistoryItem[] = [];
  loadingOrders = false;
  ordersErrorMessage = '';

  expandedReviews: { [productId: number]: boolean } = {};
  productReviews: { [productId: number]: Review[] } = {};
  loadingReviews: { [productId: number]: boolean } = {};
  newReview: { [productId: number]: { rating: number; comment: string } } = {};
  submittingReview: { [productId: number]: boolean } = {};
  reviewMessage: { [productId: number]: string } = {};
  reviewErrorMessage: { [productId: number]: string } = {};

  ngOnInit(): void {
    this.loadProducts();
    this.loadMyOrders();
  }

  loadProducts(): void {
    this.productService.getProducts().subscribe({
      next: (products) => {
        this.products = products;
        for (const product of products) {
          if (!this.orderQuantities[product.id]) {
            this.orderQuantities[product.id] = 1;
          }
        }
        this.cdr.markForCheck();
      },
      error: () => {
        this.errorMessage = 'Could not load products.';
        this.cdr.markForCheck();
      }
    });
  }

  filterProducts(): void {
    if (!this.categoryFilter) {
      this.loadProducts();
      return;
    }

    this.productService.getProductsByCategory(this.categoryFilter).subscribe({
      next: (products) => {
        this.products = products;
        for (const product of products) {
          if (!this.orderQuantities[product.id]) {
            this.orderQuantities[product.id] = 1;
          }
        }
        this.cdr.markForCheck();
      },
      error: () => {
        this.errorMessage = 'Could not filter products.';
        this.cdr.markForCheck();
      }
    });
  }

  getImageUrl(imagePath: string | null | undefined): string {
    return this.productService.getImageUrl(imagePath);
  }

  isProductInStock(product: Product): boolean {
    if (product.sizes && product.sizes.length > 0) {
      return product.sizes.some(s => s.quantity > 0);
    }
    return product.quantity > 0;
  }

  getSelectedSizeStock(product: Product): number {
    const sizeName = this.selectedSizes[product.id];
    if (!sizeName || !product.sizes) {
      return 0;
    }
    const option = product.sizes.find(s => s.size === sizeName);
    return option ? option.quantity : 0;
  }

  selectSize(product: Product, sizeName: string): void {
    this.selectedSizes[product.id] = sizeName;
    const stock = this.getSelectedSizeStock(product);
    if (!this.orderQuantities[product.id] || this.orderQuantities[product.id] > stock) {
      this.orderQuantities[product.id] = stock > 0 ? 1 : 0;
    }
    this.orderErrorMessage = '';
    this.cdr.markForCheck();
  }

  onSizeChange(product: Product): void {
    const stock = this.getSelectedSizeStock(product);
    if (!this.orderQuantities[product.id] || this.orderQuantities[product.id] > stock) {
      this.orderQuantities[product.id] = stock > 0 ? 1 : 0;
    }
    this.orderErrorMessage = '';
    this.cdr.markForCheck();
  }

  placeOrder(product: Product): void {
    this.orderMessage = '';
    this.orderErrorMessage = '';

    if (!this.authService.isLoggedIn()) {
      this.orderErrorMessage = 'Please log in as a Buyer to place an order.';
      this.cdr.markForCheck();
      return;
    }

    if (this.authService.getRole() !== 'Buyer') {
      this.orderErrorMessage = 'Only buyers are permitted to place orders.';
      this.cdr.markForCheck();
      return;
    }

    const selectedSize = this.selectedSizes[product.id];
    if (!selectedSize) {
      this.orderErrorMessage = `Please select an available size for ${product.name} before placing an order.`;
      this.cdr.markForCheck();
      return;
    }

    const sizeOption = product.sizes?.find(s => s.size === selectedSize);
    if (!sizeOption || sizeOption.quantity <= 0) {
      this.orderErrorMessage = `Size '${selectedSize}' for ${product.name} is out of stock.`;
      this.cdr.markForCheck();
      return;
    }

    const quantity = this.orderQuantities[product.id];
    if (
      quantity === null ||
      quantity === undefined ||
      isNaN(quantity) ||
      !Number.isInteger(Number(quantity)) ||
      quantity <= 0
    ) {
      this.orderErrorMessage = `Please enter a valid whole number quantity of at least 1 for ${product.name}.`;
      this.cdr.markForCheck();
      return;
    }

    if (quantity > sizeOption.quantity) {
      this.orderErrorMessage = `Requested quantity (${quantity}) exceeds available stock (${sizeOption.quantity}) for size '${selectedSize}'.`;
      this.cdr.markForCheck();
      return;
    }

    this.isOrderingProductId = product.id;
    this.cdr.markForCheck();

    this.productService.placeOrder(product.id, Number(quantity), selectedSize).subscribe({
      next: (res: any) => {
        this.isOrderingProductId = null;
        this.orderMessage = res?.message || `Order placed successfully for ${quantity} x ${product.name} (Size: ${selectedSize})!`;
        this.orderErrorMessage = '';
        this.orderQuantities[product.id] = 1;

        if (this.categoryFilter) {
          this.filterProducts();
        } else {
          this.loadProducts();
        }
        this.loadMyOrders();
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.isOrderingProductId = null;
        if (err?.status === 401) {
          this.orderErrorMessage = 'You must be logged in as a Buyer to place an order.';
        } else if (err?.status === 403) {
          this.orderErrorMessage = 'Only buyers are authorized to place orders.';
        } else if (typeof err?.error === 'string') {
          this.orderErrorMessage = err.error;
        } else if (err?.error?.message) {
          this.orderErrorMessage = err.error.message;
        } else if (err?.error?.title) {
          this.orderErrorMessage = err.error.title;
        } else {
          this.orderErrorMessage = 'Could not place order.';
        }
        this.orderMessage = '';
        this.cdr.markForCheck();
      }
    });
  }

  toggleReviews(productId: number): void {
    this.expandedReviews[productId] = !this.expandedReviews[productId];
    if (this.expandedReviews[productId]) {
      this.loadProductReviews(productId);
      if (!this.newReview[productId]) {
        this.newReview[productId] = { rating: 5, comment: '' };
      }
    }
    this.cdr.markForCheck();
  }

  loadProductReviews(productId: number): void {
    this.loadingReviews[productId] = true;
    this.productService.getProductReviews(productId).subscribe({
      next: (reviews) => {
        this.loadingReviews[productId] = false;
        this.productReviews[productId] = reviews;
        this.cdr.markForCheck();
      },
      error: () => {
        this.loadingReviews[productId] = false;
        this.cdr.markForCheck();
      }
    });
  }

  submitReview(productId: number): void {
    this.reviewMessage[productId] = '';
    this.reviewErrorMessage[productId] = '';

    if (!this.authService.isLoggedIn()) {
      this.reviewErrorMessage[productId] = 'Please log in as a Buyer to submit a review.';
      this.cdr.markForCheck();
      return;
    }

    if (this.authService.getRole() !== 'Buyer') {
      this.reviewErrorMessage[productId] = 'Only buyers can submit reviews.';
      this.cdr.markForCheck();
      return;
    }

    const reviewData = this.newReview[productId];
    if (!reviewData || !reviewData.comment || !reviewData.comment.trim()) {
      this.reviewErrorMessage[productId] = 'Please enter a review comment.';
      this.cdr.markForCheck();
      return;
    }

    if (reviewData.rating < 1 || reviewData.rating > 5) {
      this.reviewErrorMessage[productId] = 'Rating must be between 1 and 5.';
      this.cdr.markForCheck();
      return;
    }

    this.submittingReview[productId] = true;
    this.cdr.markForCheck();

    this.productService.createReview({
      productId,
      rating: Number(reviewData.rating),
      comment: reviewData.comment.trim()
    }).subscribe({
      next: () => {
        this.submittingReview[productId] = false;
        this.reviewMessage[productId] = 'Review submitted successfully!';
        this.newReview[productId] = { rating: 5, comment: '' };
        this.loadProductReviews(productId);
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.submittingReview[productId] = false;
        if (err?.status === 409) {
          this.reviewErrorMessage[productId] = 'You have already reviewed this product.';
        } else if (err?.status === 401) {
          this.reviewErrorMessage[productId] = 'You must be logged in as a Buyer to submit a review.';
        } else if (typeof err?.error === 'string') {
          this.reviewErrorMessage[productId] = err.error;
        } else if (err?.error?.message) {
          this.reviewErrorMessage[productId] = err.error.message;
        } else {
          this.reviewErrorMessage[productId] = 'Could not submit review.';
        }
        this.cdr.markForCheck();
      }
    });
  }

  hasBuyerReviewed(productId: number): boolean {
    if (!this.authService.isLoggedIn()) {
      return false;
    }
    const currentEmail = this.authService.getEmail();
    const reviews = this.productReviews[productId] || [];
    return reviews.some(r => r.buyerEmail === currentEmail);
  }

  getStars(rating: number): string {
    const full = Math.min(Math.max(rating, 1), 5);
    return '★'.repeat(full) + '☆'.repeat(5 - full);
  }

  loadMyOrders(): void {
    if (!this.authService.isLoggedIn() || this.authService.getRole() !== 'Buyer') {
      this.myOrders = [];
      return;
    }
    this.loadingOrders = true;
    this.ordersErrorMessage = '';
    this.productService.getBuyerOrders().subscribe({
      next: (orders) => {
        this.loadingOrders = false;
        this.myOrders = orders;
        this.cdr.markForCheck();
      },
      error: () => {
        this.loadingOrders = false;
        this.ordersErrorMessage = 'Could not load order history.';
        this.cdr.markForCheck();
      }
    });
  }
}