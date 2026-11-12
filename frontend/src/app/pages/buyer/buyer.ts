import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Product, ProductService } from '../../services/product';

@Component({
  selector: 'app-buyer',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './buyer.html',
  styleUrl: './buyer.css'
})
export class Buyer implements OnInit {
  private productService = inject(ProductService);
  private cdr = inject(ChangeDetectorRef);

  products: Product[] = [];
  categoryFilter = '';
  errorMessage = '';
  orderMessage = '';
  orderErrorMessage = '';
  orderQuantities: { [productId: number]: number } = {};
  isOrderingProductId: number | null = null;

  ngOnInit(): void {
    this.loadProducts();
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

  placeOrder(product: Product): void {
    this.orderMessage = '';
    this.orderErrorMessage = '';

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

    if (product.quantity <= 0) {
      this.orderErrorMessage = `Product '${product.name}' is out of stock.`;
      this.cdr.markForCheck();
      return;
    }

    if (quantity > product.quantity) {
      this.orderErrorMessage = `Requested quantity (${quantity}) exceeds available stock (${product.quantity}) for ${product.name}.`;
      this.cdr.markForCheck();
      return;
    }

    this.isOrderingProductId = product.id;
    this.cdr.markForCheck();

    this.productService.placeOrder(product.id, Number(quantity)).subscribe({
      next: (res: any) => {
        this.isOrderingProductId = null;
        this.orderMessage = res?.message || `Order placed successfully for ${quantity} x ${product.name}!`;
        this.orderErrorMessage = '';
        this.orderQuantities[product.id] = 1;

        if (this.categoryFilter) {
          this.filterProducts();
        } else {
          this.loadProducts();
        }
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.isOrderingProductId = null;
        if (typeof err?.error === 'string') {
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
}