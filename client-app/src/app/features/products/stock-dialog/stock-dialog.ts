import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';

import { Product } from '../../../core/models/product.model';

export type StockAdjustmentMode = 'add' | 'decrement';

export interface StockDialogData {
  product: Product;
  mode: StockAdjustmentMode;
}

@Component({
  selector: 'app-stock-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule
  ],
  templateUrl: './stock-dialog.html',
  styleUrl: './stock-dialog.scss'
})
export class StockDialog {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject<MatDialogRef<StockDialog, number>>(MatDialogRef);
  readonly data = inject<StockDialogData>(MAT_DIALOG_DATA);

  readonly form = this.fb.group({
    quantity: [1, [Validators.required, Validators.min(1)]]
  });

  get isAdd(): boolean {
    return this.data.mode === 'add';
  }

  cancel(): void {
    this.dialogRef.close();
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.dialogRef.close(this.form.getRawValue().quantity!);
  }
}
