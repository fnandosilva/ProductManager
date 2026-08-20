import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { of, throwError } from 'rxjs';

import { Product } from '../../../core/models/product.model';
import { ProductService } from '../../../core/services/product.service';
import { ProductList } from './product-list';

describe('ProductList', () => {
  let productService: {
    getAll: ReturnType<typeof vi.fn>;
    search: ReturnType<typeof vi.fn>;
    getByStockLevel: ReturnType<typeof vi.fn>;
    delete: ReturnType<typeof vi.fn>;
    addToStock: ReturnType<typeof vi.fn>;
    decrementStock: ReturnType<typeof vi.fn>;
  };
  let dialog: { open: ReturnType<typeof vi.fn> };
  let snackBar: { open: ReturnType<typeof vi.fn> };

  const products: Product[] = [
    { id: 100_001, name: 'Zeiss Lens Cleaner', description: 'Cleaner', price: 12.99, stock: 150 },
    { id: 100_002, name: 'Premium Eyeglass Case', description: 'Case', price: 24.5, stock: 5 }
  ];

  function createComponent(): ProductList {
    return TestBed.createComponent(ProductList).componentInstance;
  }

  function openDialogReturning(value: unknown) {
    return { afterClosed: () => of(value) };
  }

  beforeEach(async () => {
    productService = {
      getAll: vi.fn().mockReturnValue(of(products)),
      search: vi.fn().mockReturnValue(of(products)),
      getByStockLevel: vi.fn().mockReturnValue(of(products)),
      delete: vi.fn().mockReturnValue(of(undefined)),
      addToStock: vi.fn().mockReturnValue(of(undefined)),
      decrementStock: vi.fn().mockReturnValue(of(undefined))
    };
    dialog = { open: vi.fn() };
    snackBar = { open: vi.fn() };

    // MatDialog/MatSnackBar are also (re-)provided by the MatDialogModule/MatSnackBarModule that
    // ProductList imports directly, which shadows a plain `providers: [{ provide: ... }]` entry
    // here. `overrideProvider` patches the provider definition itself, so it wins regardless.
    TestBed.configureTestingModule({
      imports: [ProductList],
      providers: [{ provide: ProductService, useValue: productService }]
    });
    TestBed.overrideProvider(MatDialog, { useValue: dialog });
    TestBed.overrideProvider(MatSnackBar, { useValue: snackBar });
    await TestBed.compileComponents();
  });

  it('ngOnInit() should load all products', () => {
    const component = createComponent();

    component.ngOnInit();

    expect(productService.getAll).toHaveBeenCalledTimes(1);
    expect(component.products()).toEqual(products);
    expect(component.isLoading()).toBe(false);
    expect(component.activeFilter()).toBeNull();
  });

  it('loadAll() should surface an error via the snackbar and clear the loading state', () => {
    productService.getAll.mockReturnValue(throwError(() => ({ status: 500, error: {} })));
    const component = createComponent();

    component.loadAll();

    expect(component.isLoading()).toBe(false);
    expect(snackBar.open).toHaveBeenCalledWith('Could not load products.', 'Dismiss', { duration: 4000 });
  });

  it('search() with a blank name should fall back to loading all products', () => {
    const component = createComponent();
    component.searchForm.setValue({ name: '   ' });

    component.search();

    expect(productService.search).not.toHaveBeenCalled();
    expect(productService.getAll).toHaveBeenCalledTimes(1);
  });

  it('search() with a name should call the search endpoint and set an active filter', () => {
    productService.search.mockReturnValue(of([products[0]]));
    const component = createComponent();
    component.searchForm.setValue({ name: 'Lens' });

    component.search();

    expect(productService.search).toHaveBeenCalledWith('Lens');
    expect(component.products()).toEqual([products[0]]);
    expect(component.activeFilter()).toBe('Search: "Lens"');
    expect(component.isLoading()).toBe(false);
  });

  it('filterByStock() should call the stock-level endpoint with the form values and set an active filter', () => {
    productService.getByStockLevel.mockReturnValue(of([products[1]]));
    const component = createComponent();
    component.stockFilterForm.setValue({ min: 0, max: 10 });

    component.filterByStock();

    expect(productService.getByStockLevel).toHaveBeenCalledWith(0, 10);
    expect(component.products()).toEqual([products[1]]);
    expect(component.activeFilter()).toBe('Stock between 0 and 10');
  });

  it('filterByStock() with a cleared max field should request an unbounded upper limit, not 0', () => {
    productService.getByStockLevel.mockReturnValue(of(products));
    const component = createComponent();
    // Angular's number input yields null (not undefined) once cleared.
    component.stockFilterForm.setValue({ min: 5, max: null });

    component.filterByStock();

    expect(productService.getByStockLevel).toHaveBeenCalledWith(5, 2_147_483_647);
    expect(component.activeFilter()).toBe('Stock 5+');
  });

  it('clearFilters() should reset both forms and reload all products', () => {
    const component = createComponent();
    component.searchForm.setValue({ name: 'something' });
    component.stockFilterForm.setValue({ min: 5, max: 50 });

    component.clearFilters();

    expect(component.searchForm.getRawValue()).toEqual({ name: '' });
    expect(component.stockFilterForm.getRawValue()).toEqual({ min: 0, max: 1000 });
    expect(productService.getAll).toHaveBeenCalledTimes(1);
  });

  it('isLowStock() should flag products below the low-stock threshold', () => {
    const component = createComponent();

    expect(component.isLowStock({ ...products[0], stock: 19 })).toBe(true);
    expect(component.isLowStock({ ...products[0], stock: 20 })).toBe(false);
    expect(component.isLowStock({ ...products[0], stock: 21 })).toBe(false);
  });

  it('computed signals should reflect totals and low-stock count from the loaded products', () => {
    const component = createComponent();

    component.ngOnInit();

    expect(component.totalProducts()).toBe(2);
    expect(component.totalStockUnits()).toBe(155);
    expect(component.lowStockCount()).toBe(1);
  });

  it('openCreateDialog() should reload and notify on a created product', () => {
    const created = { ...products[0], id: 100_003, name: 'Brand New' };
    dialog.open.mockReturnValue(openDialogReturning(created));
    const component = createComponent();

    component.openCreateDialog();

    expect(snackBar.open).toHaveBeenCalledWith('Product "Brand New" created.', 'Dismiss', { duration: 3000 });
    expect(productService.getAll).toHaveBeenCalledTimes(1);
  });

  it('openCreateDialog() should do nothing when the dialog is dismissed without a result', () => {
    dialog.open.mockReturnValue(openDialogReturning(undefined));
    const component = createComponent();

    component.openCreateDialog();

    expect(snackBar.open).not.toHaveBeenCalled();
    expect(productService.getAll).not.toHaveBeenCalled();
  });

  it('openEditDialog() should reload and notify on an updated product', () => {
    const updated = { ...products[0], name: 'Updated Name' };
    dialog.open.mockReturnValue(openDialogReturning(updated));
    const component = createComponent();

    component.openEditDialog(products[0]);

    expect(snackBar.open).toHaveBeenCalledWith('Product "Updated Name" updated.', 'Dismiss', { duration: 3000 });
    expect(productService.getAll).toHaveBeenCalledTimes(1);
  });

  it('openDeleteDialog() should delete and reload when the user confirms', () => {
    dialog.open.mockReturnValue(openDialogReturning(true));
    const component = createComponent();

    component.openDeleteDialog(products[0]);

    expect(productService.delete).toHaveBeenCalledWith(products[0].id);
    expect(snackBar.open).toHaveBeenCalledWith('Product "Zeiss Lens Cleaner" deleted.', 'Dismiss', { duration: 3000 });
    expect(productService.getAll).toHaveBeenCalledTimes(1);
  });

  it('openDeleteDialog() should not delete anything when the user cancels', () => {
    dialog.open.mockReturnValue(openDialogReturning(false));
    const component = createComponent();

    component.openDeleteDialog(products[0]);

    expect(productService.delete).not.toHaveBeenCalled();
  });

  it('openDeleteDialog() should show an error and not reload when deletion fails', () => {
    dialog.open.mockReturnValue(openDialogReturning(true));
    productService.delete.mockReturnValue(throwError(() => ({ status: 500, error: {} })));
    const component = createComponent();

    component.openDeleteDialog(products[0]);

    expect(snackBar.open).toHaveBeenCalledWith('Could not delete the product.', 'Dismiss', { duration: 4000 });
    expect(productService.getAll).not.toHaveBeenCalled();
  });

  it('openStockDialog() in "add" mode should call addToStock with the chosen quantity', () => {
    dialog.open.mockReturnValue(openDialogReturning(10));
    const component = createComponent();

    component.openStockDialog(products[0], 'add');

    expect(productService.addToStock).toHaveBeenCalledWith(products[0].id, 10);
    expect(productService.decrementStock).not.toHaveBeenCalled();
    expect(snackBar.open).toHaveBeenCalledWith('10 unit(s) added to "Zeiss Lens Cleaner" stock.', 'Dismiss', {
      duration: 3000
    });
  });

  it('openStockDialog() in "decrement" mode should call decrementStock with the chosen quantity', () => {
    dialog.open.mockReturnValue(openDialogReturning(3));
    const component = createComponent();

    component.openStockDialog(products[0], 'decrement');

    expect(productService.decrementStock).toHaveBeenCalledWith(products[0].id, 3);
    expect(productService.addToStock).not.toHaveBeenCalled();
    expect(snackBar.open).toHaveBeenCalledWith('3 unit(s) removed from "Zeiss Lens Cleaner" stock.', 'Dismiss', {
      duration: 3000
    });
  });

  it('openStockDialog() should do nothing when the dialog is dismissed with no quantity', () => {
    dialog.open.mockReturnValue(openDialogReturning(undefined));
    const component = createComponent();

    component.openStockDialog(products[0], 'add');

    expect(productService.addToStock).not.toHaveBeenCalled();
    expect(productService.decrementStock).not.toHaveBeenCalled();
  });
});
