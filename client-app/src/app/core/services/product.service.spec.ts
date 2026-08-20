import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';

import { environment } from '../../../environments/environment';
import { Product, ProductRequest } from '../models/product.model';
import { ProductService } from './product.service';

describe('ProductService', () => {
  let service: ProductService;
  let httpMock: HttpTestingController;

  const baseUrl = `${environment.apiUrl}/products`;

  const sampleProduct: Product = {
    id: 100_001,
    name: 'Zeiss Lens Cleaner',
    description: 'Professional lens cleaning solution',
    price: 12.99,
    stock: 150
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });

    service = TestBed.inject(ProductService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('getAll() should GET the products collection', () => {
    service.getAll().subscribe((products) => {
      expect(products).toEqual([sampleProduct]);
    });

    const req = httpMock.expectOne(baseUrl);
    expect(req.request.method).toBe('GET');
    req.flush([sampleProduct]);
  });

  it('getById() should GET a single product by id', () => {
    service.getById(100_001).subscribe((product) => {
      expect(product).toEqual(sampleProduct);
    });

    const req = httpMock.expectOne(`${baseUrl}/100001`);
    expect(req.request.method).toBe('GET');
    req.flush(sampleProduct);
  });

  it('search() should GET with a name query parameter', () => {
    service.search('Lens').subscribe((products) => {
      expect(products).toEqual([sampleProduct]);
    });

    const req = httpMock.expectOne((request) => request.url === baseUrl + '/search' && request.params.get('name') === 'Lens');
    expect(req.request.method).toBe('GET');
    req.flush([sampleProduct]);
  });

  it('getByStockLevel() should GET with min/max query parameters', () => {
    service.getByStockLevel(10, 200).subscribe((products) => {
      expect(products).toEqual([sampleProduct]);
    });

    const req = httpMock.expectOne(
      (request) =>
        request.url === baseUrl + '/stock-level' && request.params.get('min') === '10' && request.params.get('max') === '200'
    );
    expect(req.request.method).toBe('GET');
    req.flush([sampleProduct]);
  });

  it('create() should POST a new product', () => {
    const request: ProductRequest = { name: 'New Product', description: null, price: 9.99, stock: 10 };

    service.create(request).subscribe((product) => {
      expect(product).toEqual(sampleProduct);
    });

    const req = httpMock.expectOne(baseUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush(sampleProduct);
  });

  it('update() should PUT to the product id', () => {
    const request: ProductRequest = { name: 'Updated', description: 'Updated desc', price: 19.99, stock: 5 };

    service.update(100_001, request).subscribe((product) => {
      expect(product).toEqual(sampleProduct);
    });

    const req = httpMock.expectOne(`${baseUrl}/100001`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(request);
    req.flush(sampleProduct);
  });

  it('delete() should DELETE the product by id', () => {
    service.delete(100_001).subscribe();

    const req = httpMock.expectOne(`${baseUrl}/100001`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('addToStock() should POST to the add-to-stock endpoint with the quantity in the path', () => {
    service.addToStock(100_001, 25).subscribe();

    const req = httpMock.expectOne(`${baseUrl}/100001/add-to-stock/25`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toBeNull();
    req.flush(null);
  });

  it('decrementStock() should POST to the decrement-stock endpoint with the quantity in the path', () => {
    service.decrementStock(100_001, 5).subscribe();

    const req = httpMock.expectOne(`${baseUrl}/100001/decrement-stock/5`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toBeNull();
    req.flush(null);
  });
});
