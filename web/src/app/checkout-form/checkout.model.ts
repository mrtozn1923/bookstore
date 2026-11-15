export type ShippingMethod = 'standard' | 'express' | 'pickup';

export interface CheckoutLine {
  isbn: string;
  title: string;
  quantity: number;
}

export interface CheckoutModel {
  fullName: string;
  email: string;
  shipping: ShippingMethod;
  address: { city: string; street: string; zip: string };
  giftWrap: boolean;
  institutional: boolean;
  institutionName: string;
  couponCode: string;
  lines: CheckoutLine[];
}

export function emptyCheckout(): CheckoutModel {
  return {
    fullName: '',
    email: '',
    shipping: 'standard',
    address: { city: '', street: '', zip: '' },
    giftWrap: false,
    institutional: false,
    institutionName: '',
    couponCode: '',
    lines: [
      { isbn: '9780441172719', title: 'Dune', quantity: 1 },
      { isbn: '9780141439587', title: 'Emma', quantity: 1 }
    ]
  };
}
