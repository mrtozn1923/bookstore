import { createBrowserRouter, type RouteObject } from 'react-router';
import { NotFoundPage } from '@/app/NotFoundPage';
import { InitialLoading, RootLayout } from '@/app/RootLayout';
import { RouteErrorPage } from '@/app/RouteErrorPage';
import { requireUser, SignInPage } from '@/features/auth';
import { CartPanel } from '@/features/cart';
import { bookLoader, BookPage, CatalogPage } from '@/features/catalog';
import { orderLoader, OrderPage } from '@/features/orders';

// Exported on their own so tests can mount the same tree in a memory router
export const routes: RouteObject[] = [
  {
    path: '/',
    Component: RootLayout,
    HydrateFallback: InitialLoading,
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
          const { CheckoutPage, checkoutLoader } = await import('@/features/checkout');
          return { Component: CheckoutPage, loader: checkoutLoader };
        },
      },
      { path: 'orders/:orderId', loader: orderLoader, Component: OrderPage },
      { path: 'sign-in', Component: SignInPage },
      {
        path: 'admin/stock',
        lazy: async () => ({ Component: (await import('@/features/admin')).StockPage }),
      },
      { path: '*', Component: NotFoundPage },
    ],
  },
];

export const router = createBrowserRouter(routes);
