import { Component, inject, ChangeDetectorRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.css'
})
export class Register {
  private authService = inject(AuthService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  email = '';
  password = '';
  role = 'Buyer'; // Default to 'Buyer'
  errorMessage = '';
  isLoading = false;

  onSubmit(): void {
    this.errorMessage = '';

    if (!this.email || !this.password) {
      this.errorMessage = 'Please enter both email and password.';
      return;
    }

    if (this.password.length < 6) {
      this.errorMessage = 'Password must be at least 6 characters long.';
      return;
    }

    if (this.role !== 'Buyer' && this.role !== 'Seller') {
      this.errorMessage = 'Please select a valid role (Buyer or Seller).';
      return;
    }

    this.isLoading = true;
    this.cdr.markForCheck();

    this.authService.register({
      email: this.email,
      password: this.password,
      role: this.role
    }).subscribe({
      next: (res) => {
        this.isLoading = false;
        this.cdr.markForCheck();

        if (res.role === 'Seller') {
          this.router.navigate(['/seller']);
        } else {
          this.router.navigate(['/buyer']);
        }
      },
      error: (err) => {
        this.isLoading = false;
        if (typeof err?.error === 'string') {
          this.errorMessage = err.error;
        } else if (err?.error?.message) {
          this.errorMessage = err.error.message;
        } else if (err?.status === 409) {
          this.errorMessage = 'An account with this email already exists.';
        } else if (err?.error?.errors) {
          // Validation dictionary
          const firstKey = Object.keys(err.error.errors)[0];
          this.errorMessage = err.error.errors[firstKey][0] || 'Registration validation failed.';
        } else {
          this.errorMessage = 'Could not complete registration. Please try again.';
        }
        this.cdr.markForCheck();
      }
    });
  }
}

