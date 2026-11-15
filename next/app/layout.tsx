import Link from 'next/link';
import type { ReactNode } from 'react';
import './globals.css';

export const metadata = { title: 'Bookstore', description: 'Bookstore, server-rendered with Next.js' };

// The root layout: rendered on the server, stays in place across navigations (like RootLayout in the SPA)
export default function RootLayout({ children }: { children: ReactNode }) {
  return (
    <html lang="en">
      <body>
        <header className="site-header"><Link href="/">Bookstore</Link></header>
        <main>{children}</main>
      </body>
    </html>
  );
}
