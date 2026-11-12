import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Product, ProductService } from '../../services/product';

@Component({
  selector: 'app-seller',
  standalone: true,
  imports: [FormsModule],
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

  newProduct = {
    name: '',
    description: '',
    category: '',
    price: 0,
    quantity: 0
  };

  ngOnInit(): void {
    this.loadProducts();
  }

  loadProducts(): void {
    this.productService.getProducts().subscribe({
      next: (products) => {
        this.products = products;
        this.cdr.markForCheck();
      },
      error: () => {
        this.message = 'Could not load products.';
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
    this.selectedFile = null;
    if (fileInput) {
      fileInput.value = '';
    }
    this.message = '';
    this.cdr.markForCheck();
  }

  saveProduct(fileInput?: HTMLInputElement): void {
    this.message = '';

    if (
      this.newProduct.quantity === null ||
      this.newProduct.quantity === undefined ||
      isNaN(this.newProduct.quantity) ||
      !Number.isInteger(Number(this.newProduct.quantity)) ||
      this.newProduct.quantity < 0 ||
      this.newProduct.quantity > 100000
    ) {
      this.message = 'Stock quantity must be a whole number between 0 and 100,000.';
      return;
    }

    if (!this.editingProductId && !this.selectedFile) {
      this.message = 'Please select a product image.';
      return;
    }

    const formData = new FormData();
    formData.append('name', this.newProduct.name);
    if (this.newProduct.description) {
      formData.append('description', this.newProduct.description);
    }
    formData.append('category', this.newProduct.category);
    formData.append('price', this.newProduct.price.toString());
    formData.append('quantity', this.newProduct.quantity.toString());
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
          if (typeof err?.error === 'string') {
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
          if (typeof err?.error === 'string') {
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
}