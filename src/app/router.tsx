import { createBrowserRouter } from 'react-router';
import { NotFoundPage } from '@/app/NotFoundPage';
import { RootLayout } from '@/app/RootLayout';
import { RouteErrorPage } from '@/app/RouteErrorPage';
import { requireUser } from '@/features/auth/requireUser';
import { SignInPage } from '@/features/auth/SignInPage';
import { CartPanel } from '@/features/cart/CartPanel';
import { bookLoader } from '@/features/catalog/bookLoader';
import { BookPage } from '@/features/catalog/BookPage';
import { CatalogPage } from '@/features/catalog/CatalogPage';
import { orderLoader } from '@/features/orders/orderLoader';
import { OrderPage } from '@/features/orders/OrderPage';

export const router = createBrowserRouter([
  {
    path: '/',
    Component: RootLayout,
    ErrorBoundary: RouteErrorPage,
    children: [
      { index: true, Component: CatalogPage },
      { path: 'books/:isbn', loader: bookLoader, Component: BookPage },
      { path: 'cart', Component: CartPanel },
      {
        path: 'checkout',
        middleware: [requireUser],
        // Code splitting: the checkout chunk is downloaded on the first visit to /checkout
        lazy: async () => {
          const [{ CheckoutPage }, { checkoutLoader }] = await Promise.all([
            import('@/features/checkout/CheckoutPage'),
            import('@/features/checkout/checkoutLoader'),
          ]);
          return { Component: CheckoutPage, loader: checkoutLoader };
        },
      },
      { path: 'orders/:orderId', loader: orderLoader, Component: OrderPage },
      { path: 'sign-in', Component: SignInPage },
      { path: '*', Component: NotFoundPage },
    ],
  },
]);
