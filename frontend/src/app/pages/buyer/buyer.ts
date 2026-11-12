import { Component, OnInit, inject } from '@angular/core';
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

  products: Product[] = [];
  categoryFilter = '';
  errorMessage = '';

  ngOnInit(): void {
    this.loadProducts();
  }

  loadProducts(): void {
    this.productService.getProducts().subscribe({
      next: (products) => this.products = products,
      error: () => this.errorMessage = 'Could not load products.'
    });
  }

  filterProducts(): void {
    if (!this.categoryFilter) {
      this.loadProducts();
      return;
    }

    this.productService.getProductsByCategory(this.categoryFilter).subscribe({
      next: (products) => this.products = products,
      error: () => this.errorMessage = 'Could not filter products.'
    });
  }

  getImageUrl(imagePath: string | null | undefined): string {
    return this.productService.getImageUrl(imagePath);
  }
}