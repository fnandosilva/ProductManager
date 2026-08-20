import { CurrencyPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';

import { Product } from '../../../core/models/product.model';
import { ProductService } from '../../../core/services/product.service';
import { extractErrorMessage } from '../../../core/utils/api-error.util';
import { ConfirmDialog, ConfirmDialogData } from '../../../shared/confirm-dialog/confirm-dialog';
import { ProductFormDialog, ProductFormDialogData } from '../product-form-dialog/product-form-dialog';
import { StockDialog, StockDialogData } from '../stock-dialog/stock-dialog';

const LOW_STOCK_THRESHOLD = 20;

// Sentinel for "no upper bound" when the max-stock filter field is cleared (Angular's number
// input yields `null`, not `undefined`, so `max ?? 0` was silently turning a cleared max into an
// upper bound of 0 — filtering out every product instead of removing the upper bound). Capped at
// Int32.MaxValue since the backend's Stock/GetProductsByStockLevelQuery.Max is a 32-bit int.
const UNBOUNDED_MAX_STOCK = 2_147_483_647;

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [
    CurrencyPipe,
    ReactiveFormsModule,
    MatButtonModule,
    MatChipsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatTableModule,
    MatTooltipModule
  ],
  templateUrl: './product-list.html',
  styleUrl: './product-list.scss'
})
export class ProductList implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly productService = inject(ProductService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly displayedColumns = ['id', 'name', 'description', 'price', 'stock', 'actions'];
  readonly products = signal<Product[]>([]);
  readonly isLoading = signal(false);
  readonly activeFilter = signal<string | null>(null);
  readonly lowStockThreshold = LOW_STOCK_THRESHOLD;

  readonly searchForm = this.fb.group({ name: [''] });
  readonly stockFilterForm = this.fb.group({ min: [0], max: [1000] });

  readonly totalProducts = computed(() => this.products().length);
  readonly totalStockUnits = computed(() => this.products().reduce((sum, product) => sum + product.stock, 0));
  readonly lowStockCount = computed(() => this.products().filter((product) => this.isLowStock(product)).length);

  ngOnInit(): void {
    this.loadAll();
  }

  loadAll(): void {
    this.activeFilter.set(null);
    this.isLoading.set(true);
    this.productService.getAll().subscribe({
      next: (products) => {
        this.products.set(products);
        this.isLoading.set(false);
      },
      error: (error) => this.handleLoadError(error)
    });
  }

  search(): void {
    const name = this.searchForm.getRawValue().name?.trim();
    if (!name) {
      this.loadAll();
      return;
    }

    this.isLoading.set(true);
    this.productService.search(name).subscribe({
      next: (products) => {
        this.products.set(products);
        this.activeFilter.set(`Search: "${name}"`);
        this.isLoading.set(false);
      },
      error: (error) => this.handleLoadError(error)
    });
  }

  filterByStock(): void {
    const { min, max } = this.stockFilterForm.getRawValue();
    const effectiveMin = min ?? 0;
    const effectiveMax = max ?? UNBOUNDED_MAX_STOCK;

    this.isLoading.set(true);
    this.productService.getByStockLevel(effectiveMin, effectiveMax).subscribe({
      next: (products) => {
        this.products.set(products);
        this.activeFilter.set(
          max == null ? `Stock ${effectiveMin}+` : `Stock between ${effectiveMin} and ${effectiveMax}`
        );
        this.isLoading.set(false);
      },
      error: (error) => this.handleLoadError(error)
    });
  }

  clearFilters(): void {
    this.searchForm.reset({ name: '' });
    this.stockFilterForm.reset({ min: 0, max: 1000 });
    this.loadAll();
  }

  isLowStock(product: Product): boolean {
    return product.stock < this.lowStockThreshold;
  }

  openCreateDialog(): void {
    const ref = this.dialog.open<ProductFormDialog, ProductFormDialogData, Product>(ProductFormDialog, {
      width: '480px',
      panelClass: 'app-rounded-dialog',
      data: {}
    });

    ref.afterClosed().subscribe((product) => {
      if (product) {
        this.snackBar.open(`Product "${product.name}" created.`, 'Dismiss', { duration: 3000 });
        this.loadAll();
      }
    });
  }

  openEditDialog(product: Product): void {
    const ref = this.dialog.open<ProductFormDialog, ProductFormDialogData, Product>(ProductFormDialog, {
      width: '480px',
      panelClass: 'app-rounded-dialog',
      data: { product }
    });

    ref.afterClosed().subscribe((updated) => {
      if (updated) {
        this.snackBar.open(`Product "${updated.name}" updated.`, 'Dismiss', { duration: 3000 });
        this.loadAll();
      }
    });
  }

  openDeleteDialog(product: Product): void {
    const data: ConfirmDialogData = {
      title: 'Delete product',
      message: `Are you sure you want to delete "${product.name}"? This action cannot be undone.`,
      confirmText: 'Delete',
      destructive: true
    };

    const ref = this.dialog.open<ConfirmDialog, ConfirmDialogData, boolean>(ConfirmDialog, {
      width: '400px',
      panelClass: 'app-rounded-dialog',
      data
    });

    ref.afterClosed().subscribe((confirmed) => {
      if (!confirmed) {
        return;
      }

      this.productService.delete(product.id).subscribe({
        next: () => {
          this.snackBar.open(`Product "${product.name}" deleted.`, 'Dismiss', { duration: 3000 });
          this.loadAll();
        },
        error: (error) => this.showError(error, 'Could not delete the product.')
      });
    });
  }

  openStockDialog(product: Product, mode: 'add' | 'decrement'): void {
    const data: StockDialogData = { product, mode };

    const ref = this.dialog.open<StockDialog, StockDialogData, number>(StockDialog, {
      width: '360px',
      panelClass: 'app-rounded-dialog',
      data
    });

    ref.afterClosed().subscribe((quantity) => {
      if (!quantity) {
        return;
      }

      const request$ =
        mode === 'add'
          ? this.productService.addToStock(product.id, quantity)
          : this.productService.decrementStock(product.id, quantity);

      request$.subscribe({
        next: () => {
          const verb = mode === 'add' ? 'added to' : 'removed from';
          this.snackBar.open(`${quantity} unit(s) ${verb} "${product.name}" stock.`, 'Dismiss', {
            duration: 3000
          });
          this.loadAll();
        },
        error: (error) => this.showError(error, 'Could not update stock.')
      });
    });
  }

  private handleLoadError(error: unknown): void {
    this.isLoading.set(false);
    this.showError(error, 'Could not load products.');
  }

  private showError(error: unknown, fallback: string): void {
    this.snackBar.open(extractErrorMessage(error, fallback), 'Dismiss', { duration: 4000 });
  }
}
