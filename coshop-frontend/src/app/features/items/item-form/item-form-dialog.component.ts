import { Component, inject, Inject } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { ItemDto } from '../../../shared/models/models';

export interface ItemFormResult { name: string; amount: number; unit: string; }

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>{{ data?.item ? 'Artikel bearbeiten' : 'Artikel hinzufügen' }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form" style="display:flex;flex-direction:column;gap:4px;margin-top:12px;min-width:320px">
        <mat-form-field appearance="outline">
          <mat-label>Artikelname</mat-label>
          <input matInput formControlName="name" placeholder="z.B. Milch" cdkFocusInitial (keyup.enter)="submit()">
          @if (form.get('name')?.hasError('required') && form.get('name')?.touched) {
            <mat-error>Name ist erforderlich</mat-error>
          }
        </mat-form-field>
        <div style="display:flex;gap:12px">
          <mat-form-field appearance="outline" style="flex:1">
            <mat-label>Menge</mat-label>
            <input matInput formControlName="amount" type="number" min="0" step="0.1">
          </mat-form-field>
          <mat-form-field appearance="outline" style="flex:1">
            <mat-label>Einheit</mat-label>
            <input matInput formControlName="unit" placeholder="kg, L, Stk">
          </mat-form-field>
        </div>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Abbrechen</button>
      <button mat-raised-button color="primary" (click)="submit()">Speichern</button>
    </mat-dialog-actions>
  `
})
export class ItemFormDialogComponent {
  private ref = inject(MatDialogRef<ItemFormDialogComponent>);
  private fb = inject(FormBuilder);
  form = this.fb.group({
    name:   [this.data?.item?.name ?? '', Validators.required],
    amount: [this.data?.item?.amount ?? 1, [Validators.required, Validators.min(0)]],
    unit:   [this.data?.item?.unit ?? 'Stk'],
  });
  constructor(@Inject(MAT_DIALOG_DATA) public data: { item?: ItemDto } | null) {}
  submit() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    const v = this.form.value;
    this.ref.close({ name: v.name!.trim(), amount: Number(v.amount), unit: v.unit ?? '' } as ItemFormResult);
  }
}