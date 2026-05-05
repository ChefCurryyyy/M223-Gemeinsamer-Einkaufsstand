import { Component, inject, OnInit, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { DatePipe } from '@angular/common';
import { ListService } from '../../core/services/list.service';
import { AuthService } from '../../core/services/auth.service';
import { ShoppingListSummary } from '../../shared/models/models';
import { ListFormDialogComponent } from '../lists/list-form/list-form-dialog.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    RouterLink, DatePipe,
    MatCardModule, MatButtonModule, MatIconModule, MatDialogModule,
    MatProgressSpinnerModule, MatChipsModule, MatDividerModule
  ],
  template: `
    <div class="page-container">
      <!-- Header -->
      <div class="dash-header">
        <div>
          <h2 class="greeting">Hallo, {{ auth.currentUser()?.username }} 👋</h2>
          <p class="subtitle">Deine Einkaufslisten auf einen Blick</p>
        </div>
        <button mat-raised-button color="primary" (click)="openCreate()">
          <mat-icon>add</mat-icon> Neue Liste
        </button>
      </div>

      @if (loading()) {
        <div style="display:flex;justify-content:center;margin-top:80px">
          <mat-spinner diameter="52" />
        </div>
      } @else if (lists().length === 0) {
        <mat-card class="empty-card">
          <mat-card-content>
            <mat-icon class="empty-icon">shopping_basket</mat-icon>
            <h3>Noch keine Listen</h3>
            <p>Erstelle deine erste Einkaufsliste und lade andere ein.</p>
            <button mat-raised-button color="primary" (click)="openCreate()">
              <mat-icon>add</mat-icon> Liste erstellen
            </button>
          </mat-card-content>
        </mat-card>
      } @else {
        <div class="list-grid">
          @for (list of lists(); track list.id) {
            <mat-card class="list-card" (click)="go(list.id)">
              <mat-card-content>
                <div class="card-top">
                  <div class="list-icon-wrap" [class.owned]="isOwner(list)">
                    <mat-icon>{{ isOwner(list) ? 'list_alt' : 'group' }}</mat-icon>
                  </div>
                  <div>
                    <div class="list-title">{{ list.title }}</div>
                    <div class="list-date">{{ list.createdAt | date:'dd.MM.yyyy' }}</div>
                  </div>
                </div>
                <mat-divider style="margin:12px 0 10px" />
                <div class="card-chips">
                  <mat-chip-set>
                    <mat-chip highlighted color="primary">
                      <mat-icon matChipAvatar>check_box</mat-icon>
                      {{ list.itemCount }} Artikel
                    </mat-chip>
                    <mat-chip>
                      <mat-icon matChipAvatar>people</mat-icon>
                      {{ list.memberCount + 1 }}
                    </mat-chip>
                    @if (!isOwner(list)) {
                      <mat-chip highlighted color="accent">Geteilt</mat-chip>
                    }
                  </mat-chip-set>
                </div>
              </mat-card-content>
            </mat-card>
          }
        </div>
      }
    </div>
  `,
  styles: [`
    .dash-header { display:flex; align-items:flex-start; justify-content:space-between; flex-wrap:wrap; gap:16px; margin-bottom:32px; }
    .greeting { font-family:'DM Serif Display',serif; font-size:1.9rem; margin:0 0 4px; }
    .subtitle  { color:var(--muted); margin:0; }

    .empty-card { text-align:center; padding:48px 24px; }
    .empty-icon { font-size:72px; width:72px; height:72px; color:var(--border); margin-bottom:12px; }
    .empty-card h3 { font-size:1.3rem; margin:0 0 8px; }
    .empty-card p  { color:var(--muted); margin:0 0 20px; }

    .list-grid { display:grid; grid-template-columns:repeat(auto-fill,minmax(280px,1fr)); gap:16px; }

    .list-card {
      cursor:pointer;
      transition: box-shadow .2s, border-color .2s;
      border: 1.5px solid var(--border);
    }
    .list-card:hover { box-shadow: var(--shadow-md) !important; border-color: var(--primary); }

    .card-top { display:flex; align-items:center; gap:14px; }
    .list-icon-wrap {
      width:44px; height:44px; border-radius:10px;
      background:var(--bg); display:flex; align-items:center; justify-content:center;
      color:var(--muted); flex-shrink:0;
    }
    .list-icon-wrap.owned { background:var(--primary-light); color:var(--primary); }
    .list-title { font-weight:600; font-size:1rem; }
    .list-date  { font-size:0.78rem; color:var(--muted); }
    .card-chips mat-chip { font-size:0.78rem; }
  `]
})
export class DashboardComponent implements OnInit {
  private listService = inject(ListService);
  auth = inject(AuthService);
  private router = inject(Router);
  private dialog = inject(MatDialog);
  private snack = inject(MatSnackBar);

  lists = signal<ShoppingListSummary[]>([]);
  loading = signal(true);

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.listService.getAll().subscribe({
      next: d => { this.lists.set(d); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  go(id: number) { this.router.navigate(['/lists', id]); }
  isOwner(l: ShoppingListSummary) { return l.ownerId === this.auth.currentUser()?.userId; }

  openCreate() {
    this.dialog.open(ListFormDialogComponent, { width: '420px', data: null, autoFocus: true })
        .afterClosed().subscribe(title => {
      if (!title) return;
      this.listService.create(title).subscribe({
        next: () => { this.load(); this.snack.open('Liste erstellt ✓', '', { duration: 2500, panelClass: 'snack-success' }); },
        error: () => this.snack.open('Fehler', 'OK', { duration: 3000, panelClass: 'snack-error' })
      });
    });
  }
}