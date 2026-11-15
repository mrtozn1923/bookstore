import { useSearchParams } from 'react-router';

export function SignInPage() {
  const [searchParams] = useSearchParams();
  // Only allow paths inside this app as a target (no open redirect to another site); the BFF checks it again
  const target = searchParams.get('redirectTo') ?? '/';
  const redirectTo = target.startsWith('/') && !target.startsWith('//') ? target : '/';

  return (
    <>
      <h1>Sign in</h1>
      <p className="muted">You go to the sign-in page and come back here signed in.</p>
      {/* A full page navigation, not fetch: the BFF answers with a redirect and sets the session cookie */}
      <a className="button" href={`/bff/login?returnUrl=${encodeURIComponent(redirectTo)}`}>Sign in</a>
    </>
  );
}
