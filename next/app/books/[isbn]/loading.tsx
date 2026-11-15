// Shown while the page segment renders for the first time (Next.js wraps page.tsx in a Suspense boundary)
export default function BookLoading() {
  return <p className="muted">Loading book…</p>;
}
