import { createContext, use } from 'react';

export type Theme = 'light' | 'dark';

export type ThemeContextValue = {
  theme: Theme;
  toggleTheme: () => void;
};

// A meaningful default: components still render (light) without a provider
export const ThemeContext = createContext<ThemeContextValue>({ theme: 'light', toggleTheme: () => {} });

export function useTheme(): ThemeContextValue {
  return use(ThemeContext);
}
