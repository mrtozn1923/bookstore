import { UserMenu } from '@/features/auth/UserMenu';
import { CartPanel } from '@/features/cart/CartPanel';
import { CartSummary } from '@/features/cart/CartSummary';
import { CatalogPage } from '@/features/catalog/CatalogPage';
import { Layout } from '@/shared/Layout';
import { ThemeToggle } from '@/shared/theme/ThemeToggle';

// No cart props any more: CatalogPage, CartPanel and CartSummary read the cart from context
export default function App() {
  return (
    <Layout title="Bookstore" headerExtra={<><CartSummary /><ThemeToggle /><UserMenu /></>}>
      <div className="page">
        <div>
          <CatalogPage />
        </div>
        <CartPanel />
      </div>
    </Layout>
  );
}
