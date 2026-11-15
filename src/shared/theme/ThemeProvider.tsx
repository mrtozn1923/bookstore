import { useEffect, useState, type ReactNode } from 'react';
import { ThemeContext, type Theme } from '@/shared/theme/ThemeContext';
import { usePrefersDark } from '@/shared/usePrefersDark';

export function ThemeProvider({ children }: { children: ReactNode }) {
  const prefersDark = usePrefersDark();
  // null = follow the system setting until the user picks a theme
  const [choice, setChoice] = useState<Theme | null>(null);
  const theme: Theme = choice ?? (prefersDark ? 'dark' : 'light');

  // <html data-theme> lives outside React's tree: an effect keeps it in sync
  useEffect(() => {
    document.documentElement.dataset.theme = theme;
  }, [theme]);

  const value = { theme, toggleTheme: () => setChoice(theme === 'dark' ? 'light' : 'dark') };
  return <ThemeContext value={value}>{children}</ThemeContext>;
}
