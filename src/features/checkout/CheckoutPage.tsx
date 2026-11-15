import { Form, useActionData, useNavigation } from 'react-router';
import { useCart } from '@/features/cart/useCart';
import type { checkoutAction } from '@/features/checkout/checkoutAction';
import { formatPrice } from '@/shared/format';

export function CheckoutPage() {
  const { lines, total } = useCart();
  const result = useActionData<typeof checkoutAction>();
  const navigation = useNavigation();
  const placing = navigation.state === 'submitting';

  return (
    <>
      <h1>Checkout</h1>
      <ul>
        {lines.map((line) => <li key={line.isbn}>{line.quantity} × {line.title} · {formatPrice(line.lineTotal)}</li>)}
      </ul>
      <p className="total">Total: {formatPrice(total)}</p>
      {result?.error && <p className="error" role="alert">Order failed: {result.error}</p>}
      <Form method="post">
        <button type="submit" disabled={placing || lines.length === 0}>{placing ? 'Placing order…' : 'Place order'}</button>
      </Form>
    </>
  );
}
