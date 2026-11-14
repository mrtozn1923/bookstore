import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { Customer, CustomerService } from './customer.service';

describe('CustomerService', () => {
  let service: CustomerService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
    imports: [],
    providers: [provideHttpClient(withInterceptorsFromDi()), provideHttpClientTesting()]
});
    service = TestBed.inject(CustomerService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should fetch customers with a GET request', () => {
    const fakeCustomers: Customer[] = [
      { id: 1, name: 'Ada Lovelace', email: 'ada@example.com' },
      { id: 2, name: 'Alan Turing', email: 'alan@example.com' }
    ];

    service.getCustomers().subscribe(customers => {
      expect(customers.length).toBe(2);
      expect(customers[0].name).toBe('Ada Lovelace');
    });

    const req = httpMock.expectOne('http://localhost:3000/customers');
    expect(req.request.method).toBe('GET');
    req.flush(fakeCustomers);
  });

  it('should report the error after two retries', () => {
    spyOn(console, 'error');
    let status = 0;

    service.getCustomers().subscribe({
      next: () => fail('expected an error'),
      error: (error: HttpErrorResponse) => status = error.status
    });

    for (let i = 0; i < 3; i++) {
      httpMock.expectOne('http://localhost:3000/customers')
        .flush('Server error', { status: 500, statusText: 'Server Error' });
    }

    expect(status).toBe(500);
    expect(console.error).toHaveBeenCalledWith('Could not load customers:', 500, jasmine.any(String));
  });
});
