import type { ReactNode } from 'react';

type LayoutProps = {
  title: string;
  // Like a named ng-content slot: the caller decides what goes into the header
  headerExtra?: ReactNode;
  children: ReactNode;
};

export function Layout({ title, headerExtra, children }: LayoutProps) {
  return (
    <>
      <header className="site-header">
        <strong>{title}</strong>
        {headerExtra}
      </header>
      <main>{children}</main>
    </>
  );
}
