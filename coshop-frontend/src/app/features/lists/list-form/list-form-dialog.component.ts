import { Component, inject, Inject } from '@angular/core';
import { FormControl, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>{{ data?.title ? 'Liste umbenennen' : 'Neue Liste erstellen' }}</h2>
    <mat-dialog-content>
      <mat-form-field appearance="outline" style="width:100%;margin-top:12px">
        <mat-label>Listenname</mat-label>
        <input matInput [formControl]="ctrl" (keyup.enter)="submit()" placeholder="z.B. Wocheneinkauf" cdkFocusInitial>
        @if (ctrl.hasError('required') && ctrl.touched) {
          <mat-error>Name ist erforderlich</mat-error>
        }
      </mat-form-field>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Abbrechen</button>
      <button mat-raised-button color="primary" (click)="submit()">Speichern</button>
    </mat-dialog-actions>
  `
})
export class ListFormDialogComponent {
  private ref = inject(MatDialogRef<ListFormDialogComponent>);
  ctrl: FormControl;
  constructor(@Inject(MAT_DIALOG_DATA) public data: { title?: string } | null) {
    this.ctrl = new FormControl(data?.title ?? '', Validators.required);
  }
  submit() {
    if (this.ctrl.invalid) { this.ctrl.markAsTouched(); return; }
    this.ref.close(this.ctrl.value.trim());
  }
}