import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { Product, ProductService, Review, OrderHistoryItem } from '../../services/product';

@Component({
  selector: 'app-seller',
  standalone: true,
  imports: [FormsModule, DatePipe],
  templateUrl: './seller.html',
  styleUrl: './seller.css'
})
export class Seller implements OnInit {
  private productService = inject(ProductService);
  private cdr = inject(ChangeDetectorRef);

  products: Product[] = [];
  message = '';
  selectedFile: File | null = null;
  editingProductId: number | null = null;
  editingProductCurrentImageUrl: string | null = null;

  expandedReviews: { [productId: number]: boolean } = {};
  productReviews: { [productId: number]: Review[] } = {};
  loadingReviews: { [productId: number]: boolean } = {};
  reviewError: { [productId: number]: string } = {};

  sellerOrders: OrderHistoryItem[] = [];
  loadingOrders = false;
  ordersErrorMessage = '';

  presetSizes = ['XS', 'S', 'M', 'L', 'XL', 'XXL', 'One Size'];
  customSizeName = '';
  sizeOptions: { size: string; quantity: number }[] = [
    { size: 'S', quantity: 10 },
    { size: 'M', quantity: 10 },
    { size: 'L', quantity: 10 },
    { size: 'XL', quantity: 10 }
  ];

  newProduct = {
    name: '',
    description: '',
    category: '',
    price: 0,
    quantity: 0
  };

  ngOnInit(): void {
    this.loadProducts();
    this.loadSellerOrders();
  }

  loadProducts(): void {
    this.productService.getSellerProducts().subscribe({
      next: (products) => {
        this.products = products;
        this.cdr.markForCheck();
      },
      error: (err) => {
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

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) {
      this.selectedFile = null;
      return;
    }

    const file = input.files[0];
    const allowedExtensions = ['.jpg', '.jpeg', '.png', '.webp', '.gif'];
    const fileName = file.name.toLowerCase();
    const hasAllowedExtension = allowedExtensions.some(ext => fileName.endsWith(ext));
    const isImageMime = file.type.startsWith('image/');

    if (!isImageMime && !hasAllowedExtension) {
      this.message = 'Invalid image format. Allowed formats: .jpg, .jpeg, .png, .webp, .gif.';
      this.selectedFile = null;
      input.value = '';
      this.cdr.markForCheck();
      return;
    }

    const maxSizeBytes = 5 * 1024 * 1024; // 5 MB
    if (file.size > maxSizeBytes) {
      this.message = 'Image size exceeds the 5 MB limit.';
      this.selectedFile = null;
      input.value = '';
      this.cdr.markForCheck();
      return;
    }

    this.selectedFile = file;
    this.message = '';
    this.cdr.markForCheck();
  }

  hasSize(sizeName: string): boolean {
    const key = sizeName.trim().toUpperCase();
    return this.sizeOptions.some(s => s.size.trim().toUpperCase() === key);
  }

  addSizeOption(preset?: string): void {
    const sizeName = (preset || '').trim();
    if (sizeName && this.hasSize(sizeName)) {
      this.message = `Size '${sizeName}' is already in the list.`;
      this.cdr.markForCheck();
      return;
    }

    this.sizeOptions.push({
      size: sizeName || '',
      quantity: 10
    });
    this.message = '';
    this.cdr.markForCheck();
  }

  removeSizeOption(index: number): void {
    if (this.sizeOptions.length > 1) {
      this.sizeOptions.splice(index, 1);
      this.cdr.markForCheck();
    }
  }

  addCustomSize(): void {
    const trimmed = this.customSizeName.trim();
    if (!trimmed) {
      return;
    }
    if (this.hasSize(trimmed)) {
      this.message = `Size '${trimmed}' is already in the list.`;
      this.cdr.markForCheck();
      return;
    }

    this.sizeOptions.push({
      size: trimmed,
      quantity: 10
    });
    this.customSizeName = '';
    this.message = '';
    this.cdr.markForCheck();
  }

  getTotalStock(): number {
    return this.sizeOptions.reduce((sum, item) => sum + (Number(item.quantity) || 0), 0);
  }

  editProduct(product: Product, fileInput?: HTMLInputElement): void {
    this.message = '';
    this.editingProductId = product.id;
    this.editingProductCurrentImageUrl = product.imageUrl ?? null;
    this.newProduct = {
      name: product.name,
      description: product.description ?? '',
      category: product.category,
      price: product.price,
      quantity: product.quantity
    };

    if (product.sizes && product.sizes.length > 0) {
      this.sizeOptions = product.sizes.map(s => ({ size: s.size, quantity: s.quantity }));
    } else {
      this.sizeOptions = [{ size: 'One Size', quantity: product.quantity }];
    }

    this.selectedFile = null;
    if (fileInput) {
      fileInput.value = '';
    }
    this.cdr.markForCheck();
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  cancelEdit(fileInput?: HTMLInputElement): void {
    this.editingProductId = null;
    this.editingProductCurrentImageUrl = null;
    this.newProduct = {
      name: '',
      description: '',
      category: '',
      price: 0,
      quantity: 0
    };
    this.sizeOptions = [
      { size: 'S', quantity: 10 },
      { size: 'M', quantity: 10 },
      { size: 'L', quantity: 10 },
      { size: 'XL', quantity: 10 }
    ];
    this.customSizeName = '';
    this.selectedFile = null;
    if (fileInput) {
      fileInput.value = '';
    }
    this.message = '';
    this.cdr.markForCheck();
  }

  saveProduct(fileInput?: HTMLInputElement): void {
    this.message = '';

    if (!this.sizeOptions || this.sizeOptions.length === 0) {
      this.message = 'Please specify at least one size option with its stock quantity.';
      this.cdr.markForCheck();
      return;
    }

    for (const opt of this.sizeOptions) {
      const trimmedSize = opt.size ? opt.size.trim() : '';
      if (!trimmedSize) {
        this.message = 'Every size option must have a size name.';
        this.cdr.markForCheck();
        return;
      }
      if (trimmedSize.length > 20) {
        this.message = `Size name '${trimmedSize}' cannot exceed 20 characters.`;
        this.cdr.markForCheck();
        return;
      }
      const qty = Number(opt.quantity);
      if (
        opt.quantity === null ||
        opt.quantity === undefined ||
        isNaN(qty) ||
        !Number.isInteger(qty) ||
        qty < 0 ||
        qty > 100000
      ) {
        this.message = `Stock quantity for size '${trimmedSize}' must be a whole number between 0 and 100,000.`;
        this.cdr.markForCheck();
        return;
      }
    }

    const seen = new Set<string>();
    for (const opt of this.sizeOptions) {
      const key = opt.size.trim().toUpperCase();
      if (seen.has(key)) {
        this.message = `Duplicate size '${opt.size.trim()}' is not allowed. Each size must be unique.`;
        this.cdr.markForCheck();
        return;
      }
      seen.add(key);
    }

    const totalStock = this.getTotalStock();
    this.newProduct.quantity = totalStock;

    if (!this.editingProductId && !this.selectedFile) {
      this.message = 'Please select a product image.';
      this.cdr.markForCheck();
      return;
    }

    const formData = new FormData();
    formData.append('name', this.newProduct.name);
    if (this.newProduct.description) {
      formData.append('description', this.newProduct.description);
    }
    formData.append('category', this.newProduct.category);
    formData.append('price', this.newProduct.price.toString());
    formData.append('quantity', totalStock.toString());

    const cleanSizes = this.sizeOptions.map(s => ({
      size: s.size.trim(),
      quantity: Number(s.quantity)
    }));
    formData.append('sizesJson', JSON.stringify(cleanSizes));

    if (this.selectedFile) {
      formData.append('image', this.selectedFile);
    }

    if (this.editingProductId) {
      this.productService.updateProduct(this.editingProductId, formData).subscribe({
        next: () => {
          this.cancelEdit(fileInput);
          this.message = 'Product updated successfully.';
          this.loadProducts();
          this.cdr.markForCheck();
        },
        error: (err) => {
          if (err?.status === 403) {
            this.message = typeof err?.error === 'string' ? err.error : 'You do not have permission to edit this product.';
          } else if (err?.status === 401) {
            this.message = 'You must be logged in as a Seller to update products.';
          } else if (typeof err?.error === 'string') {
            this.message = err.error;
          } else if (err?.error?.title) {
            this.message = err.error.title;
          } else {
            this.message = 'Could not update the product.';
          }
          this.cdr.markForCheck();
        }
      });
    } else {
      this.productService.createProduct(formData).subscribe({
        next: () => {
          this.cancelEdit(fileInput);
          this.message = 'Product added successfully.';
          this.loadProducts();
          this.cdr.markForCheck();
        },
        error: (err) => {
          if (err?.status === 401 || err?.status === 403) {
            this.message = 'You must be logged in as a Seller to add products.';
          } else if (typeof err?.error === 'string') {
            this.message = err.error;
          } else if (err?.error?.title) {
            this.message = err.error.title;
          } else {
            this.message = 'Could not add the product.';
          }
          this.cdr.markForCheck();
        }
      });
    }
  }

  addProduct(fileInput?: HTMLInputElement): void {
    this.saveProduct(fileInput);
  }

  toggleReviews(productId: number): void {
    this.expandedReviews[productId] = !this.expandedReviews[productId];
    if (this.expandedReviews[productId]) {
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

  loadSellerOrders(): void {
    this.loadingOrders = true;
    this.ordersErrorMessage = '';
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