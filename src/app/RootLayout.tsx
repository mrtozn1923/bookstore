import { NavLink, Outlet, useNavigation } from 'react-router';
import { UserMenu } from '@/features/auth/UserMenu';
import { CartSummary } from '@/features/cart/CartSummary';
import { ThemeToggle } from '@/shared/theme/ThemeToggle';

// The layout route: header stays, <Outlet /> is replaced by the matching child route (like <router-outlet>)
export function RootLayout() {
  const navigation = useNavigation();

  return (
    <>
      <header className="site-header">
        <strong>Bookstore</strong>
        <nav className="main-nav">
          <NavLink to="/" end>Catalog</NavLink>
          <NavLink to="/cart">Cart</NavLink>
        </nav>
        <CartSummary />
        <ThemeToggle />
        <UserMenu />
      </header>
      {navigation.state !== 'idle' && <p className="progress" role="status">Loading page…</p>}
      <main>
        <Outlet />
      </main>
    </>
  );
}
