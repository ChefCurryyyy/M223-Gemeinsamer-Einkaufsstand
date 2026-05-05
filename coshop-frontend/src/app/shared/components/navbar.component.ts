import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterLink, MatToolbarModule, MatButtonModule, MatIconModule, MatMenuModule, MatChipsModule, MatDividerModule],
  template: `
    <mat-toolbar color="primary" style="position:sticky;top:0;z-index:200;box-shadow:0 2px 8px rgba(0,0,0,.18)">
      <a routerLink="/dashboard" class="brand">
        <mat-icon>shopping_cart</mat-icon>
        <span>Co-Shop</span>
      </a>

      <span style="flex:1"></span>

      @if (auth.isLoggedIn()) {
        <!-- Admin badge -->
        @if (auth.isAdmin()) {
          <span class="admin-badge">Admin</span>
        }

        <button mat-button [matMenuTriggerFor]="menu">
          <mat-icon>account_circle</mat-icon>
          {{ auth.currentUser()?.username }}
          <mat-icon>arrow_drop_down</mat-icon>
        </button>
        <mat-menu #menu="matMenu">
          <div class="menu-user-info">
            <span class="menu-username">{{ auth.currentUser()?.username }}</span>
            <span class="menu-role">{{ auth.currentUser()?.role }}</span>
          </div>
          <mat-divider />
          <button mat-menu-item (click)="auth.logout()">
            <mat-icon>logout</mat-icon> Abmelden
          </button>
        </mat-menu>
      }
    </mat-toolbar>
  `,
  styles: [`
    .brand {
      display: flex; align-items: center; gap: 8px;
      text-decoration: none; color: white;
      font-family: 'DM Serif Display', serif; font-size: 1.3rem;
    }
    .admin-badge {
      background: rgba(255,255,255,.25);
      color: white;
      font-size: 0.7rem;
      font-weight: 700;
      letter-spacing: .08em;
      text-transform: uppercase;
      padding: 2px 10px;
      border-radius: 20px;
      margin-right: 8px;
    }
    .menu-user-info {
      padding: 10px 16px 8px;
      display: flex;
      flex-direction: column;
    }
    .menu-username { font-weight: 600; font-size: 0.9rem; }
    .menu-role     { font-size: 0.75rem; color: var(--muted); }
  `]
})
export class NavbarComponent {
  auth = inject(AuthService);
}