import { useTheme } from '@/shared/theme/ThemeContext';

export function ThemeToggle() {
  const { theme, toggleTheme } = useTheme();
  return (
    <button type="button" aria-pressed={theme === 'dark'} onClick={toggleTheme}>
      {theme === 'dark' ? 'Light theme' : 'Dark theme'}
    </button>
  );
}
