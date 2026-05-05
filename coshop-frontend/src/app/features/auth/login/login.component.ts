import { Component, inject } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    ReactiveFormsModule, RouterLink,
    MatCardModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatSnackBarModule
  ],
  template: `
    <div class="auth-page">
      <mat-card class="auth-card">
        <div class="auth-header">
          <mat-icon class="auth-icon">shopping_cart</mat-icon>
          <h1>Co-Shop</h1>
          <p>Anmelden und gemeinsam einkaufen</p>
        </div>

        <form [formGroup]="form" (ngSubmit)="submit()">
          <mat-form-field appearance="outline">
            <mat-label>E-Mail</mat-label>
            <input matInput formControlName="email" type="email" autocomplete="email">
            <mat-icon matSuffix>email</mat-icon>
            @if (form.get('email')?.hasError('required') && form.get('email')?.touched) {
              <mat-error>E-Mail ist erforderlich</mat-error>
            }
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Passwort</mat-label>
            <input matInput formControlName="password" [type]="hide ? 'password' : 'text'" autocomplete="current-password">
            <button mat-icon-button matSuffix type="button" (click)="hide = !hide">
              <mat-icon>{{ hide ? 'visibility_off' : 'visibility' }}</mat-icon>
            </button>
            @if (form.get('password')?.hasError('required') && form.get('password')?.touched) {
              <mat-error>Passwort ist erforderlich</mat-error>
            }
          </mat-form-field>

          <button mat-raised-button color="primary" type="submit"
                  class="submit-btn" [disabled]="loading">
            @if (loading) {
              <mat-spinner diameter="20" />
            } @else {
              Anmelden
            }
          </button>
        </form>

        <p class="auth-link">
          Noch kein Konto? <a routerLink="/register">Registrieren</a>
        </p>
      </mat-card>
    </div>
  `,
  styles: [`
    .auth-page {
      min-height: calc(100vh - 64px);
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 24px 16px;
      background: var(--bg);
    }
    .auth-card {
      width: 100%;
      max-width: 420px;
      padding: 40px 36px;
    }
    .auth-header {
      text-align: center;
      margin-bottom: 32px;
    }
    .auth-icon {
      font-size: 48px;
      width: 48px;
      height: 48px;
      color: var(--primary);
    }
    .auth-header h1 {
      font-family: 'DM Serif Display', serif;
      font-size: 2rem;
      margin: 8px 0 4px;
      color: var(--text);
    }
    .auth-header p {
      color: var(--muted);
      margin: 0;
    }
    form {
      display: flex;
      flex-direction: column;
      gap: 4px;
    }
    .submit-btn {
      height: 48px;
      font-size: 1rem;
      margin-top: 8px;
      border-radius: 8px !important;
    }
    .auth-link {
      text-align: center;
      margin-top: 20px;
      color: var(--muted);
      font-size: 0.9rem;
    }
    .auth-link a { color: var(--primary); text-decoration: none; font-weight: 500; }
    .auth-link a:hover { text-decoration: underline; }
  `]
})
export class LoginComponent {
  private auth = inject(AuthService);
  private router = inject(Router);
  private snack = inject(MatSnackBar);
  private fb = inject(FormBuilder);

  form = this.fb.group({
    email:    ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  hide = true;
  loading = false;

  submit() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.loading = true;
    const { email, password } = this.form.value;
    this.auth.login(email!, password!).subscribe({
      next: () => this.router.navigate(['/dashboard']),
      error: (e) => {
        this.loading = false;
        this.snack.open(e.error?.message ?? 'Anmeldung fehlgeschlagen', 'OK',
            { duration: 4000, panelClass: 'snack-error' });
      }
    });
  }
}