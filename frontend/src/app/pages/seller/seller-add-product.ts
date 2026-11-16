import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { Product, ProductService } from '../../services/product';

@Component({
  selector: 'app-seller-add-product',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './seller-add-product.html',
  styleUrl: './seller-add-product.css'
})
export class SellerAddProduct implements OnInit {
  private productService = inject(ProductService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private cdr = inject(ChangeDetectorRef);

  editingProductId: number | null = null;
  editingProductCurrentImageUrl: string | null = null;
  selectedFile: File | null = null;
  message = '';
  isSuccess = false;
  isSaving = false;

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
    this.route.queryParams.subscribe(params => {
      const editId = params['edit'];
      if (editId) {
        this.editingProductId = Number(editId);
        this.loadProductForEditing(this.editingProductId);
      } else {
        this.resetForm();
      }
    });
  }

  loadProductForEditing(productId: number): void {
    this.message = '';
    this.productService.getSellerProducts().subscribe({
      next: (products) => {
        const product = products.find(p => p.id === productId);
        if (product) {
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
          this.cdr.markForCheck();
        } else {
          this.message = 'Product not found.';
          this.isSuccess = false;
          this.cdr.markForCheck();
        }
      },
      error: () => {
        this.message = 'Could not load product details for editing.';
        this.isSuccess = false;
        this.cdr.markForCheck();
      }
    });
  }

  resetForm(fileInput?: HTMLInputElement): void {
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
    this.cdr.markForCheck();
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
      this.isSuccess = false;
      this.selectedFile = null;
      input.value = '';
      this.cdr.markForCheck();
      return;
    }

    const maxSizeBytes = 5 * 1024 * 1024; // 5 MB
    if (file.size > maxSizeBytes) {
      this.message = 'Image size exceeds the 5 MB limit.';
      this.isSuccess = false;
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
      this.isSuccess = false;
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
      this.isSuccess = false;
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

  cancelEdit(fileInput?: HTMLInputElement): void {
    this.resetForm(fileInput);
    this.router.navigate(['/seller/products']);
  }

  saveProduct(fileInput?: HTMLInputElement): void {
    this.message = '';

    if (!this.sizeOptions || this.sizeOptions.length === 0) {
      this.message = 'Please specify at least one size option with its stock quantity.';
      this.isSuccess = false;
      this.cdr.markForCheck();
      return;
    }

    for (const opt of this.sizeOptions) {
      const trimmedSize = opt.size ? opt.size.trim() : '';
      if (!trimmedSize) {
        this.message = 'Every size option must have a size name.';
        this.isSuccess = false;
        this.cdr.markForCheck();
        return;
      }
      if (trimmedSize.length > 20) {
        this.message = `Size name '${trimmedSize}' cannot exceed 20 characters.`;
        this.isSuccess = false;
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
        this.isSuccess = false;
        this.cdr.markForCheck();
        return;
      }
    }

    const seen = new Set<string>();
    for (const opt of this.sizeOptions) {
      const key = opt.size.trim().toUpperCase();
      if (seen.has(key)) {
        this.message = `Duplicate size '${opt.size.trim()}' is not allowed. Each size must be unique.`;
        this.isSuccess = false;
        this.cdr.markForCheck();
        return;
      }
      seen.add(key);
    }

    const totalStock = this.getTotalStock();
    this.newProduct.quantity = totalStock;

    if (!this.editingProductId && !this.selectedFile) {
      this.message = 'Please select a product image.';
      this.isSuccess = false;
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

    this.isSaving = true;
    this.cdr.markForCheck();

    if (this.editingProductId) {
      this.productService.updateProduct(this.editingProductId, formData).subscribe({
        next: () => {
          this.isSaving = false;
          this.isSuccess = true;
          this.message = 'Product updated successfully.';
          if (fileInput) {
            fileInput.value = '';
          }
          this.cdr.markForCheck();
        },
        error: (err) => {
          this.isSaving = false;
          this.isSuccess = false;
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
          this.isSaving = false;
          this.isSuccess = true;
          this.message = 'Product added successfully.';
          this.resetForm(fileInput);
          this.cdr.markForCheck();
        },
        error: (err) => {
          this.isSaving = false;
          this.isSuccess = false;
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
}

