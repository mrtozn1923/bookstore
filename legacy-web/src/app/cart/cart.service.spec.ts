import { CartService } from './cart.service';

describe('CartService', () => {
  let service: CartService;

  beforeEach(() => {
    service = new CartService();
  });

  it('should return 0 for an empty cart', () => {
    expect(service.getTotal()).toBe(0);
  });

  it('should calculate the total from price and quantity', () => {
    service.add({ title: 'Dune', price: 450, quantity: 2 });
    service.add({ title: 'Emma', price: 320, quantity: 1 });

    expect(service.getTotal()).toBe(1220);
  });
});
