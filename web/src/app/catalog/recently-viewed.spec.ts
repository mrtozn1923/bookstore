import { TestBed } from '@angular/core/testing';
import { RecentlyViewed } from './recently-viewed';

describe('RecentlyViewed', () => {
  beforeEach(() => localStorage.clear());

  it('puts the latest book first', () => {
    const recent = TestBed.inject(RecentlyViewed);

    recent.mark('9780441013593'); // Dune
    recent.mark('9780141439587'); // Emma

    expect(recent.list()).toEqual(['9780141439587', '9780441013593']);
  });

  it('keeps only the last three books', () => {
    const recent = TestBed.inject(RecentlyViewed);

    ['9780261103344', '9780141439549', '9780141439587', '9780441013593'].forEach((isbn) => recent.mark(isbn));

    expect(recent.list()).toEqual(['9780441013593', '9780141439587', '9780141439549']);
  });

  it('saves the list to localStorage', () => {
    const recent = TestBed.inject(RecentlyViewed);

    recent.mark('9780441013593');
    expect(localStorage.getItem('bookstore.recentlyViewed')).toBeNull();

    TestBed.tick();
    expect(localStorage.getItem('bookstore.recentlyViewed')).toBe('["9780441013593"]');
  });

  it('restores the list saved by an earlier visit', () => {
    localStorage.setItem('bookstore.recentlyViewed', '["9780141439587"]');

    const recent = TestBed.inject(RecentlyViewed);

    expect(recent.list()).toEqual(['9780141439587']);
  });
});
