import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ProductService } from '../../services/product';

@Component({
  selector: 'app-seller',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './seller.html',
  styleUrl: './seller.css'
})
export class Seller {
  private productService = inject(ProductService);

  message = '';
  selectedFile: File | null = null;

  newProduct = {
    name: '',
    description: '',
    category: '',
    price: 0,
    quantity: 0
  };

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
      return;
    }

    const maxSizeBytes = 5 * 1024 * 1024; // 5 MB
    if (file.size > maxSizeBytes) {
      this.message = 'Image size exceeds the 5 MB limit.';
      this.selectedFile = null;
      input.value = '';
      return;
    }

    this.selectedFile = file;
    this.message = '';
  }

  addProduct(fileInput?: HTMLInputElement): void {
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

    if (!this.selectedFile) {
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
    formData.append('image', this.selectedFile);

    this.productService.createProduct(formData).subscribe({
      next: () => {
        this.message = 'Product added successfully.';
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
      },
      error: (err) => {
        if (typeof err?.error === 'string') {
          this.message = err.error;
        } else if (err?.error?.title) {
          this.message = err.error.title;
        } else {
          this.message = 'Could not add the product.';
        }
      }
    });
  }
}