import { useSyncExternalStore } from 'react';

const query = '(prefers-color-scheme: dark)';

// The OS/browser color scheme is an external store: React subscribes and reads a snapshot
function subscribe(onChange: () => void) {
  const media = window.matchMedia(query);
  media.addEventListener('change', onChange);
  return () => media.removeEventListener('change', onChange);
}

function getSnapshot() {
  return window.matchMedia(query).matches;
}

export function usePrefersDark(): boolean {
  return useSyncExternalStore(subscribe, getSnapshot);
}
