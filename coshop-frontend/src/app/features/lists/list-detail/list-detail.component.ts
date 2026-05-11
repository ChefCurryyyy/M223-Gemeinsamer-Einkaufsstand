// KI-generiert (Claude AI) — grösste Komponente (~480 Zeilen): Echtzeit-SignalR-Subscriptions, Optimistic-Locking-Feedback, lokales State-Management
import { Component, inject, OnInit, signal, computed, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { ReactiveFormsModule, FormControl, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDividerModule } from '@angular/material/divider';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatBadgeModule } from '@angular/material/badge';
import { ListService } from '../../../core/services/list.service';
import { ItemService } from '../../../core/services/item.service';
import { AuthService } from '../../../core/services/auth.service';
import { SignalRService } from '../../../core/services/signalr.service';
import { ShoppingListDetail, ItemDto } from '../../../shared/models/models';
import { ListFormDialogComponent } from '../list-form/list-form-dialog.component';
import { ItemFormDialogComponent, ItemFormResult } from '../../items/item-form/item-form-dialog.component';

@Component({
  selector: 'app-list-detail',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatCardModule, MatButtonModule, MatIconModule, MatCheckboxModule,
    MatFormFieldModule, MatInputModule, MatDividerModule,
    MatProgressBarModule, MatProgressSpinnerModule,
    MatDialogModule, MatMenuModule, MatTooltipModule, MatBadgeModule
  ],
  template: `
    <div class="page-container">
      @if (loading()) {
        <div style="display:flex;justify-content:center;margin-top:80px"><mat-spinner diameter="52"/></div>
      } @else if (list()) {

        <!-- Realtime badge -->
        @if (realtimeMsg()) {
          <div class="rt-banner">
            <mat-icon>sync</mat-icon> {{ realtimeMsg() }}
          </div>
        }

        <!-- Header -->
        <div class="detail-header">
          <button mat-icon-button (click)="router.navigate(['/dashboard'])">
            <mat-icon>arrow_back</mat-icon>
          </button>
          <div class="header-info">
            <h2>{{ list()!.title }}</h2>
            <span class="owner-chip">
              <mat-icon>person</mat-icon> {{ list()!.ownerUsername }}
            </span>
          </div>
          <span style="flex:1"></span>
          @if (isOwner()) {
            <button mat-icon-button [matMenuTriggerFor]="listMenu" matTooltip="Optionen">
              <mat-icon>more_vert</mat-icon>
            </button>
            <mat-menu #listMenu="matMenu">
              <button mat-menu-item (click)="renameList()">
                <mat-icon>edit</mat-icon> Umbenennen
              </button>
              <button mat-menu-item (click)="deleteList()" style="color:#c62828">
                <mat-icon color="warn">delete</mat-icon> Liste löschen
              </button>
            </mat-menu>
          }
        </div>

        <!-- Progress bar -->
        @if (totalItems() > 0) {
          <div class="progress-row">
            <mat-progress-bar mode="determinate" [value]="progress()" color="primary" style="border-radius:4px;flex:1" />
            <span class="progress-label">{{ boughtCount() }}/{{ totalItems() }} erledigt</span>
          </div>
        }

        <div class="content-grid">
          <!-- ── Items column ── -->
          <div>
            <div class="section-head">
              <h3>Artikel</h3>
              <button mat-stroked-button color="primary" (click)="addItem()">
                <mat-icon>add</mat-icon> Hinzufügen
              </button>
            </div>

            @if (totalItems() === 0) {
              <mat-card style="text-align:center;padding:32px">
                <mat-icon style="font-size:48px;width:48px;height:48px;color:var(--border)">shopping_basket</mat-icon>
                <p style="color:var(--muted);margin:8px 0 0">Noch keine Artikel</p>
              </mat-card>
            }

            <!-- Pending items -->
            @if (pendingItems().length > 0) {
              <mat-card style="padding:0;overflow:hidden">
                @for (item of pendingItems(); track item.id; let last = $last) {
                  <div class="item-row" [class.locking]="lockedItemIds().has(item.id)">
                    @if (lockedItemIds().has(item.id)) {
                      <mat-spinner diameter="20" style="margin:0 12px 0 4px" />
                    } @else {
                      <mat-checkbox [checked]="item.isBought" color="primary"
                                    (change)="toggleBought(item, $event.checked)" />
                    }
                    <div class="item-text">
                      <span class="item-name">{{ item.name }}</span>
                      <span class="item-meta">{{ item.amount }} {{ item.unit }}</span>
                    </div>
                    @if (lockedItemIds().has(item.id)) {
                      <span class="lock-label" matTooltip="Wird gerade von jemand anderem geändert">
                        <mat-icon>lock</mat-icon>
                      </span>
                    } @else {
                      <button mat-icon-button [matMenuTriggerFor]="itemMenu">
                        <mat-icon>more_vert</mat-icon>
                      </button>
                      <mat-menu #itemMenu="matMenu">
                        <button mat-menu-item (click)="editItem(item)">
                          <mat-icon>edit</mat-icon> Bearbeiten
                        </button>
                        <button mat-menu-item (click)="deleteItem(item)" style="color:#c62828">
                          <mat-icon color="warn">delete</mat-icon> Löschen
                        </button>
                      </mat-menu>
                    }
                  </div>
                  @if (!last) { <mat-divider /> }
                }
              </mat-card>
            }

            <!-- Bought items -->
            @if (boughtItems().length > 0) {
              <div class="bought-section">
                <p class="bought-label">Erledigt ({{ boughtItems().length }})</p>
                <mat-card style="padding:0;overflow:hidden;opacity:.7">
                  @for (item of boughtItems(); track item.id; let last = $last) {
                    <div class="item-row">
                      <mat-checkbox [checked]="true" color="primary"
                                    (change)="toggleBought(item, $event.checked)" />
                      <div class="item-text">
                        <span class="item-name" style="text-decoration:line-through;color:var(--muted)">{{ item.name }}</span>
                        <span class="item-meta">{{ item.amount }} {{ item.unit }}</span>
                      </div>
                    </div>
                    @if (!last) { <mat-divider /> }
                  }
                </mat-card>
              </div>
            }
          </div>

          <!-- ── Members column ── -->
          <div>
            <div class="section-head">
              <h3>Mitglieder</h3>
              @if (isOwner()) {
                <button mat-stroked-button color="primary" (click)="showInvite = !showInvite">
                  <mat-icon>person_add</mat-icon>
                </button>
              }
            </div>

            @if (showInvite) {
              <mat-card style="margin-bottom:12px;padding:16px">
                <mat-form-field appearance="outline" style="width:100%">
                  <mat-label>Benutzername</mat-label>
                  <input matInput [formControl]="inviteCtrl" (keyup.enter)="inviteMember()" />
                  <mat-icon matSuffix>person_search</mat-icon>
                </mat-form-field>
                <button mat-raised-button color="primary" style="width:100%" (click)="inviteMember()">
                  Einladen
                </button>
              </mat-card>
            }

            <mat-card style="padding:0;overflow:hidden">
              <!-- Owner -->
              <div class="member-row">
                <div class="avatar primary-avatar">{{ list()!.ownerUsername[0].toUpperCase() }}</div>
                <div class="member-info">
                  <span class="member-name">{{ list()!.ownerUsername }}</span>
                  <span class="member-role owner">Ersteller</span>
                </div>
              </div>

              @for (m of list()!.members; track m.userId) {
                <mat-divider />
                <div class="member-row">
                  <div class="avatar">{{ m.username[0].toUpperCase() }}</div>
                  <div class="member-info">
                    <span class="member-name">{{ m.username }}</span>
                    <span class="member-role">Mitglied</span>
                  </div>
                  @if (isOwner()) {
                    <button mat-icon-button color="warn" matTooltip="Entfernen"
                            (click)="removeMember(m.userId)">
                      <mat-icon>person_remove</mat-icon>
                    </button>
                  }
                </div>
              }
            </mat-card>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .detail-header { display:flex; align-items:center; gap:8px; margin-bottom:20px; }
    .header-info h2 { margin:0; font-family:'DM Serif Display',serif; font-size:1.6rem; }
    .owner-chip { display:flex;align-items:center;gap:3px;font-size:.8rem;color:var(--muted); }
    .owner-chip mat-icon { font-size:14px;width:14px;height:14px; }

    .progress-row { display:flex; align-items:center; gap:12px; margin-bottom:24px; }
    .progress-label { font-size:.83rem; color:var(--muted); white-space:nowrap; }

    .content-grid {
      display:grid; grid-template-columns:1fr 300px; gap:24px; align-items:start;
    }
    @media(max-width:720px){.content-grid{grid-template-columns:1fr}}

    .section-head { display:flex;align-items:center;justify-content:space-between;margin-bottom:12px; }
    .section-head h3 { margin:0;font-size:1rem;font-weight:600; }

    .item-row { display:flex; align-items:center; gap:10px; padding:10px 14px; transition:background .1s; }
    .item-row:hover { background:var(--bg); }
    .item-row.locking { background:#fff8e1; pointer-events:none; opacity:.8; }
    .item-text { flex:1; }
    .item-name { display:block; font-weight:500; font-size:.95rem; }
    .item-meta { font-size:.78rem; color:var(--muted); }
    .lock-label { display:flex; align-items:center; color:#f57c00; }
    .lock-label mat-icon { font-size:18px;width:18px;height:18px; }

    .bought-section { margin-top:16px; }
    .bought-label { font-size:.75rem;color:var(--muted);margin:0 0 6px 4px;text-transform:uppercase;letter-spacing:.06em; }

    .member-row { display:flex; align-items:center; gap:12px; padding:10px 16px; }
    .avatar {
      width:36px; height:36px; border-radius:50%;
      background:var(--primary-light); color:var(--primary);
      display:flex; align-items:center; justify-content:center;
      font-weight:700; font-size:.95rem; flex-shrink:0;
    }
    .primary-avatar { background:var(--primary); color:white; }
    .member-info { flex:1; }
    .member-name { display:block; font-weight:500; font-size:.9rem; }
    .member-role { font-size:.73rem; color:var(--muted); }
    .member-role.owner { color:var(--primary); font-weight:600; }
  `]
})
export class ListDetailComponent implements OnInit {
  private route  = inject(ActivatedRoute);
  private listSvc = inject(ListService);
  private itemSvc = inject(ItemService);
  private auth   = inject(AuthService);
  private hub    = inject(SignalRService);
  private dialog = inject(MatDialog);
  private snack  = inject(MatSnackBar);
  router = inject(Router);

  private destroyRef = inject(DestroyRef);

  list        = signal<ShoppingListDetail | null>(null);
  loading     = signal(true);
  realtimeMsg = signal('');
  lockedItemIds = signal(new Set<number>());
  showInvite  = false;
  inviteCtrl  = new FormControl('', Validators.required);

  pendingItems = computed(() => this.list()?.items.filter(i => !i.isBought) ?? []);
  boughtItems  = computed(() => this.list()?.items.filter(i => i.isBought) ?? []);
  totalItems   = computed(() => this.list()?.items.length ?? 0);
  boughtCount  = computed(() => this.boughtItems().length);
  progress     = computed(() =>
      this.totalItems() === 0 ? 0 : Math.round(this.boughtCount() / this.totalItems() * 100));

  isOwner() { return this.list()?.ownerId === this.auth.currentUser()?.userId; }
  private get listId() { return Number(this.route.snapshot.paramMap.get('id')); }
  private get myId()   { return this.auth.currentUser()!.userId; }

  async ngOnInit() {
    this.load();
    this.subscribeToRealtime();
    await this.hub.connectToList(this.listId);
    this.destroyRef.onDestroy(() => this.hub.leaveList(this.listId));
  }

  load() {
    this.loading.set(true);
    this.listSvc.getById(this.listId).subscribe({
      next: d => { this.list.set(d); this.loading.set(false); },
      error: () => { this.loading.set(false); this.router.navigate(['/dashboard']); }
    });
  }

  // KI-generiert — 8 typisierte SignalR-Subscriptions mit takeUntilDestroyed und skipSelf-Filter
  // ── Realtime handlers ─────────────────────────────────────────────────────
  private subscribeToRealtime() {
    const skipSelf = (actorId: number) => actorId !== this.myId;

    this.hub.itemCreated$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(e => {
      if (skipSelf(e.actorUserId)) {
        this.addItemLocally({ id: e.itemId, name: e.name, amount: e.amount, unit: e.unit,
          isBought: false, listId: e.listId, lastModifiedByUserId: e.actorUserId,
          lastModifiedByUsername: e.actorUsername });
        this.flash(`${e.actorUsername} hat "${e.name}" hinzugefügt`);
      }
    });

    this.hub.itemUpdated$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(e => {
      if (skipSelf(e.actorUserId)) {
        this.updateItemLocally(e.itemId, { name: e.name, amount: e.amount, unit: e.unit });
        this.flash(`${e.actorUsername} hat "${e.name}" bearbeitet`);
      }
    });

    this.hub.itemDeleted$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(e => {
      if (skipSelf(e.actorUserId)) {
        this.removeItemLocally(e.itemId);
        this.flash('Ein Artikel wurde gelöscht');
      }
    });

    this.hub.itemBought$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(e => {
      if (skipSelf(e.actorUserId)) {
        // Lock the item briefly to prevent double-toggle race
        this.lockItem(e.itemId);
        this.updateItemLocally(e.itemId, { isBought: e.isBought });
        setTimeout(() => this.unlockItem(e.itemId), 800);
        this.flash(`${e.actorUsername} hat "${this.findItem(e.itemId)?.name ?? 'Artikel'}" ${e.isBought ? 'gekauft ✓' : 'zurückgesetzt'}`);
      }
    });

    this.hub.listRenamed$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(e => {
      if (skipSelf(e.actorUserId)) {
        this.list.update(l => l ? { ...l, title: e.newTitle } : l);
        this.flash(`Liste wurde umbenannt zu "${e.newTitle}"`);
      }
    });

    this.hub.listDeleted$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(e => {
      if (skipSelf(e.actorUserId)) {
        this.snack.open('Diese Liste wurde vom Ersteller gelöscht.', 'OK', { duration: 5000, panelClass: 'snack-info' });
        this.router.navigate(['/dashboard']);
      }
    });

    this.hub.memberAdded$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(e => {
      if (skipSelf(e.userId)) {
        this.list.update(l => l ? { ...l, members: [...l.members, { userId: e.userId, username: e.username, joinedAt: new Date().toISOString() }] } : l);
        this.flash(`${e.username} ist der Liste beigetreten`);
      }
    });

    this.hub.memberRemoved$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(e => {
      if (e.userId === this.myId) {
        this.snack.open('Du wurdest von der Liste entfernt.', 'OK', { duration: 5000, panelClass: 'snack-info' });
        this.router.navigate(['/dashboard']);
      } else {
        this.list.update(l => l ? { ...l, members: l.members.filter(m => m.userId !== e.userId) } : l);
      }
    });
  }

  // ── Local state helpers ────────────────────────────────────────────────────
  private addItemLocally(item: ItemDto) {
    this.list.update(l => l ? { ...l, items: [...l.items, item] } : l);
  }

  private updateItemLocally(itemId: number, patch: Partial<ItemDto>) {
    this.list.update(l => l ? {
      ...l, items: l.items.map(i => i.id === itemId ? { ...i, ...patch } : i)
    } : l);
  }

  private removeItemLocally(itemId: number) {
    this.list.update(l => l ? { ...l, items: l.items.filter(i => i.id !== itemId) } : l);
  }

  private findItem(id: number) { return this.list()?.items.find(i => i.id === id); }

  private lockItem(id: number) {
    this.lockedItemIds.update(s => { const n = new Set(s); n.add(id); return n; });
  }
  private unlockItem(id: number) {
    this.lockedItemIds.update(s => { const n = new Set(s); n.delete(id); return n; });
  }

  private rtTimer: ReturnType<typeof setTimeout> | undefined;
  private flash(msg: string) {
    this.realtimeMsg.set(msg);
    clearTimeout(this.rtTimer);
    this.rtTimer = setTimeout(() => this.realtimeMsg.set(''), 3500);
  }

  // ── Items ─────────────────────────────────────────────────────────────────
  addItem() {
    this.dialog.open(ItemFormDialogComponent, { width: '420px', data: null })
        .afterClosed().subscribe((r: ItemFormResult) => {
      if (!r) return;
      this.itemSvc.create(this.listId, r.name, r.amount, r.unit).subscribe({
        next: item => { this.addItemLocally(item); this.snack.open('Hinzugefügt ✓', '', { duration: 2000, panelClass: 'snack-success' }); },
        error: () => this.snack.open('Fehler', 'OK', { duration: 3000, panelClass: 'snack-error' })
      });
    });
  }

  editItem(item: ItemDto) {
    this.dialog.open(ItemFormDialogComponent, { width: '420px', data: { item } })
        .afterClosed().subscribe((r: ItemFormResult) => {
      if (!r) return;
      this.itemSvc.update(this.listId, item.id, r.name, r.amount, r.unit).subscribe({
        next: updated => { this.updateItemLocally(item.id, updated); this.snack.open('Gespeichert ✓', '', { duration: 2000, panelClass: 'snack-success' }); },
        error: () => this.snack.open('Fehler', 'OK', { duration: 3000, panelClass: 'snack-error' })
      });
    });
  }

  toggleBought(item: ItemDto, isBought: boolean) {
    if (this.lockedItemIds().has(item.id)) return;
    this.lockItem(item.id);
    this.itemSvc.toggleBought(this.listId, item.id, isBought).subscribe({
      next: updated => { this.unlockItem(item.id); this.updateItemLocally(item.id, { isBought: updated.isBought }); },
      error: e => {
        this.unlockItem(item.id);
        const msg = e.status === 409 ? 'Konflikt: Artikel wurde gleichzeitig geändert!' : 'Fehler';
        this.snack.open(msg, 'OK', { duration: 4000, panelClass: 'snack-error' });
      }
    });
  }

  deleteItem(item: ItemDto) {
    this.itemSvc.delete(this.listId, item.id).subscribe({
      next: () => { this.removeItemLocally(item.id); this.snack.open('Gelöscht', '', { duration: 2000 }); }
    });
  }

  // ── List ──────────────────────────────────────────────────────────────────
  renameList() {
    this.dialog.open(ListFormDialogComponent, { width: '420px', data: { title: this.list()!.title } })
        .afterClosed().subscribe(title => {
      if (!title) return;
      this.listSvc.update(this.listId, title).subscribe({
        next: () => { this.list.update(l => l ? { ...l, title } : l); this.snack.open('Umbenannt ✓', '', { duration: 2000, panelClass: 'snack-success' }); }
      });
    });
  }

  deleteList() {
    if (!confirm(`Liste "${this.list()!.title}" wirklich löschen?`)) return;
    this.listSvc.delete(this.listId).subscribe({
      next: () => { this.router.navigate(['/dashboard']); this.snack.open('Liste gelöscht', '', { duration: 2500 }); }
    });
  }

  // ── Members ───────────────────────────────────────────────────────────────
  inviteMember() {
    if (this.inviteCtrl.invalid) return;
    this.listSvc.inviteMember(this.listId, this.inviteCtrl.value!).subscribe({
      next: m => {
        this.showInvite = false; this.inviteCtrl.reset();
        this.list.update(l => l ? { ...l, members: [...l.members, m] } : l);
        this.snack.open(`${m.username} eingeladen ✓`, '', { duration: 2500, panelClass: 'snack-success' });
      },
      error: e => this.snack.open(e.error?.message ?? 'Fehler', 'OK', { duration: 3000, panelClass: 'snack-error' })
    });
  }

  removeMember(memberId: number) {
    this.listSvc.removeMember(this.listId, memberId).subscribe({
      next: () => {
        this.list.update(l => l ? { ...l, members: l.members.filter(m => m.userId !== memberId) } : l);
        this.snack.open('Mitglied entfernt', '', { duration: 2000 });
      }
    });
  }
}