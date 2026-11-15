import { CartService } from './cart.service';

describe('CartService', () => {
  let service: CartService;

  beforeEach(() => {
    service = new CartService();
  });

  it('should return 0 for an empty cart', () => {
    expect(service.getTotal()).toBe(0);
  });

  it('should add up the prices of the books in the cart', () => {
    service.add({ title: 'Dune', price: 450 });
    service.add({ title: 'Emma', price: 320 });

    expect(service.getTotal()).toBe(770);
  });

  it('should remove a line by its id', () => {
    service.add({ title: 'Dune', price: 450 });
    service.add({ title: 'Dune', price: 450 });
    service.remove(1);

    expect(service.getTotal()).toBe(450);
  });
});
