import { Component, Input, Output, EventEmitter, HostListener } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-modal',
  standalone: true,
  imports: [MatIconModule, MatButtonModule],
  template: `
    <div class="modal-backdrop" (click)="onBackdropClick($event)">
      <div class="modal-box" (click)="$event.stopPropagation()">
        <h2>{{ title }}</h2>
        <ng-content />
        <div class="modal-actions">
          <button mat-button (click)="cancel.emit()">Abbrechen</button>
          <button mat-raised-button color="primary" (click)="confirm.emit()">
            {{ confirmLabel }}
          </button>
        </div>
      </div>
    </div>
  `
})
export class ModalComponent {
  @Input() title = '';
  @Input() confirmLabel = 'Speichern';
  @Output() cancel = new EventEmitter<void>();
  @Output() confirm = new EventEmitter<void>();

  onBackdropClick(e: Event) {
    if ((e.target as HTMLElement).classList.contains('modal-backdrop')) {
      this.cancel.emit();
    }
  }

  @HostListener('document:keydown.escape')
  onEsc() { this.cancel.emit(); }
}