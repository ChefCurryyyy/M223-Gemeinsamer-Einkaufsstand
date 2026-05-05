import { Component, inject } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { AuthService } from '../../../core/services/auth.service';

// Must match backend regex: min 8 chars, 1 uppercase, 1 lowercase, 1 digit
function passwordStrength(ctrl: AbstractControl): ValidationErrors | null {
  const v: string = ctrl.value ?? '';
  if (!v) return null;
  if (v.length < 8)           return { minlength: true };
  if (!/[A-Z]/.test(v))       return { noUppercase: true };
  if (!/[a-z]/.test(v))       return { noLowercase: true };
  if (!/[0-9]/.test(v))       return { noDigit: true };
  return null;
}

// Username: only letters, numbers, _ and -
function usernameFormat(ctrl: AbstractControl): ValidationErrors | null {
  const v: string = ctrl.value ?? '';
  if (!v) return null;
  if (!/^[a-zA-Z0-9_\-]+$/.test(v)) return { invalidChars: true };
  return null;
}

@Component({
  selector: 'app-register',
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
          <mat-icon class="auth-icon">person_add</mat-icon>
          <h1>Konto erstellen</h1>
          <p>Starte deine erste gemeinsame Liste</p>
        </div>

        <form [formGroup]="form" (ngSubmit)="submit()">
          <mat-form-field appearance="outline">
            <mat-label>Benutzername</mat-label>
            <input matInput formControlName="username" autocomplete="username">
            <mat-icon matSuffix>person</mat-icon>
            @if (f['username'].touched) {
              @if (f['username'].hasError('required'))     { <mat-error>Benutzername ist erforderlich</mat-error> }
              @if (f['username'].hasError('minlength'))    { <mat-error>Mind. 3 Zeichen</mat-error> }
              @if (f['username'].hasError('invalidChars')) { <mat-error>Nur Buchstaben, Zahlen, _ und -</mat-error> }
            }
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>E-Mail</mat-label>
            <input matInput formControlName="email" type="email" autocomplete="email">
            <mat-icon matSuffix>email</mat-icon>
            @if (f['email'].touched && f['email'].hasError('email')) {
              <mat-error>Ungültige E-Mail-Adresse</mat-error>
            }
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Passwort</mat-label>
            <input matInput formControlName="password" [type]="hide ? 'password' : 'text'" autocomplete="new-password">
            <button mat-icon-button matSuffix type="button" (click)="hide = !hide">
              <mat-icon>{{ hide ? 'visibility_off' : 'visibility' }}</mat-icon>
            </button>
            @if (f['password'].touched) {
              @if (f['password'].hasError('minlength'))   { <mat-error>Mind. 8 Zeichen erforderlich</mat-error> }
              @if (f['password'].hasError('noUppercase')) { <mat-error>Mind. 1 Grossbuchstabe erforderlich</mat-error> }
              @if (f['password'].hasError('noLowercase')) { <mat-error>Mind. 1 Kleinbuchstabe erforderlich</mat-error> }
              @if (f['password'].hasError('noDigit'))     { <mat-error>Mind. 1 Zahl erforderlich</mat-error> }
            }
          </mat-form-field>

          <button mat-raised-button color="primary" type="submit" class="submit-btn" [disabled]="loading">
            @if (loading) { <mat-spinner diameter="20" /> } @else { Registrieren }
          </button>
        </form>

        <p class="auth-link">
          Bereits ein Konto? <a routerLink="/login">Anmelden</a>
        </p>
      </mat-card>
    </div>
  `,
  styles: [`
    .auth-page {
      min-height: calc(100vh - 64px);
      display: flex; align-items: center; justify-content: center;
      padding: 24px 16px; background: var(--bg);
    }
    .auth-card { width: 100%; max-width: 420px; padding: 40px 36px; }
    .auth-header { text-align: center; margin-bottom: 32px; }
    .auth-icon { font-size: 48px; width: 48px; height: 48px; color: var(--primary); }
    .auth-header h1 { font-family: 'DM Serif Display', serif; font-size: 2rem; margin: 8px 0 4px; }
    .auth-header p { color: var(--muted); margin: 0; }
    form { display: flex; flex-direction: column; gap: 4px; }
    .submit-btn { height: 48px; font-size: 1rem; margin-top: 8px; }
    .auth-link { text-align: center; margin-top: 20px; color: var(--muted); font-size: 0.9rem; }
    .auth-link a { color: var(--primary); text-decoration: none; font-weight: 500; }
    .auth-link a:hover { text-decoration: underline; }
  `]
})
export class RegisterComponent {
  private auth  = inject(AuthService);
  private router = inject(Router);
  private snack  = inject(MatSnackBar);
  private fb     = inject(FormBuilder);

  form = this.fb.group({
    username: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(50), usernameFormat]],
    email:    ['', [Validators.required, Validators.email, Validators.maxLength(200)]],
    password: ['', [Validators.required, passwordStrength]],
  });

  get f() { return this.form.controls; }
  hide = true;
  loading = false;

  submit() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.loading = true;
    const { username, email, password } = this.form.value;
    this.auth.register(username!, email!, password!).subscribe({
      next: () => this.router.navigate(['/dashboard']),
      error: (e) => {
        this.loading = false;
        const msg = e.error?.message ?? 'Registrierung fehlgeschlagen';
        this.snack.open(msg, 'OK', { duration: 4000, panelClass: 'snack-error' });
      }
    });
  }
}