import { useEffect, useEffectEvent } from 'react';

// Calls onPress when the key is pressed anywhere on the page, except while typing in a field
export function useKeyboardShortcut(key: string, onPress: () => void) {
  const handlePress = useEffectEvent(onPress);

  useEffect(() => {
    function handleKeyDown(e: KeyboardEvent) {
      const target = e.target as HTMLElement;
      if (e.key !== key || target.closest('input, textarea, select')) {
        return;
      }
      e.preventDefault();
      handlePress();
    }
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [key]);
}
