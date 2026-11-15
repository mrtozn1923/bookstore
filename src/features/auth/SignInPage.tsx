import { useNavigate, useSearchParams } from 'react-router';
import { useAuthStore } from '@/features/auth/authStore';

export function SignInPage() {
  const signIn = useAuthStore((state) => state.signIn);
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  // Only allow paths inside this app as a target (no open redirect to another site)
  const target = searchParams.get('redirectTo') ?? '/';
  const redirectTo = target.startsWith('/') && !target.startsWith('//') ? target : '/';

  return (
    <>
      <h1>Sign in</h1>
      <p className="muted">Demo sign-in: no password, the user lives in memory.</p>
      <button type="button" onClick={() => {
        signIn({ name: 'Ada Lovelace', email: 'ada@example.com' });
        navigate(redirectTo, { replace: true });
      }}>Sign in as Ada</button>
    </>
  );
}
