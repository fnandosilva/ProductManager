import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { Product } from '../../../core/models/product.model';
import { ProductService } from '../../../core/services/product.service';
import { extractErrorMessage } from '../../../core/utils/api-error.util';

export interface ProductFormDialogData {
  product?: Product;
}

@Component({
  selector: 'app-product-form-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './product-form-dialog.html',
  styleUrl: './product-form-dialog.scss'
})
export class ProductFormDialog {
  private readonly fb = inject(FormBuilder);
  private readonly productService = inject(ProductService);
  private readonly dialogRef = inject<MatDialogRef<ProductFormDialog, Product>>(MatDialogRef);
  readonly data = inject<ProductFormDialogData>(MAT_DIALOG_DATA);

  readonly isEditMode = !!this.data.product;
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.fb.group({
    name: [this.data.product?.name ?? '', [Validators.required, Validators.maxLength(200)]],
    description: [this.data.product?.description ?? '', [Validators.maxLength(1000)]],
    price: [this.data.product?.price ?? null, [Validators.required, Validators.min(0.01)]],
    stock: [this.data.product?.stock ?? null, [Validators.required, Validators.min(0)]]
  });

  cancel(): void {
    this.dialogRef.close();
  }

  submit(): void {
    if (this.form.invalid || this.isSubmitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.errorMessage.set(null);
    this.isSubmitting.set(true);

    const raw = this.form.getRawValue();
    const request = {
      name: raw.name!.trim(),
      description: raw.description?.trim() ? raw.description.trim() : null,
      price: raw.price!,
      stock: raw.stock!
    };

    const request$ = this.isEditMode
      ? this.productService.update(this.data.product!.id, request)
      : this.productService.create(request);

    request$.subscribe({
      next: (product) => {
        this.isSubmitting.set(false);
        this.dialogRef.close(product);
      },
      error: (error) => {
        this.isSubmitting.set(false);
        this.errorMessage.set(extractErrorMessage(error, 'Could not save the product.'));
      }
    });
  }
}
