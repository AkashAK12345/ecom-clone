import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ProductSize {
  id?: number;
  productId?: number;
  size: string;
  quantity: number;
}

export interface Product {
  id: number;
  name: string;
  description: string | null;
  category: string;
  price: number;
  quantity: number;
  imageUrl?: string | null;
  sellerId?: number | null;
  sizes?: ProductSize[];
}

export interface Review {
  id: number;
  productId: number;
  productName: string;
  buyerId: number;
  buyerEmail: string;
  rating: number;
  comment: string;
  createdAt: string;
}

export interface CreateReviewRequest {
  productId: number;
  rating: number;
  comment: string;
}

export interface OrderHistoryItem {
  id: number;
  productId: number;
  productName: string;
  size?: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
  orderDate: string;
}

@Injectable({ providedIn: 'root' })
export class ProductService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5000/api/products';

  getProducts(): Observable<Product[]> {
    return this.http.get<Product[]>(this.apiUrl);
  }

  getSellerProducts(): Observable<Product[]> {
    return this.http.get<Product[]>(`${this.apiUrl}/seller`);
  }

  createProduct(product: FormData | Omit<Product, 'id'>): Observable<Product> {
    return this.http.post<Product>(this.apiUrl, product);
  }

  updateProduct(id: number, product: FormData): Observable<Product> {
    return this.http.put<Product>(`${this.apiUrl}/${id}`, product);
  }

  placeOrder(productId: number, quantity: number, size: string): Observable<any> {
    const ordersUrl = this.apiUrl.replace(/\/products\/?$/, '/orders');
    return this.http.post(ordersUrl, { productId, quantity, size });
  }

  getBuyerOrders(): Observable<OrderHistoryItem[]> {
    const ordersUrl = this.apiUrl.replace(/\/products\/?$/, '/orders/buyer');
    return this.http.get<OrderHistoryItem[]>(ordersUrl);
  }

  getSellerOrders(): Observable<OrderHistoryItem[]> {
    const ordersUrl = this.apiUrl.replace(/\/products\/?$/, '/orders/seller');
    return this.http.get<OrderHistoryItem[]>(ordersUrl);
  }

  getProductReviews(productId: number): Observable<Review[]> {
    return this.http.get<Review[]>(`http://localhost:5000/api/reviews/product/${productId}`);
  }

  createReview(review: CreateReviewRequest): Observable<Review> {
    return this.http.post<Review>(`http://localhost:5000/api/reviews`, review);
  }

  getSellerProductReviews(productId: number): Observable<Review[]> {
    return this.http.get<Review[]>(`http://localhost:5000/api/reviews/seller/${productId}`);
  }

  getProductsByCategory(category: string): Observable<Product[]> {
    return this.http.get<Product[]>(
      `${this.apiUrl}/category/${encodeURIComponent(category)}`
    );
  }

  getImageUrl(imagePath: string | null | undefined): string {
    if (!imagePath) {
      return '';
    }
    if (imagePath.startsWith('http://') || imagePath.startsWith('https://')) {
      return imagePath;
    }
    const baseUrl = this.apiUrl.replace(/\/api\/products\/?$/, '');
    const cleanPath = imagePath.startsWith('/') ? imagePath : `/${imagePath}`;
    return `${baseUrl}${cleanPath}`;
  }
}