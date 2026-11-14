import { computed, type Signal } from '@angular/core';
import { patchState, signalStoreFeature, withComputed, withLinkedState, withMethods } from '@ngrx/signals';

// Arrow-key selection for any list; the list comes from the store through withFeature
export function withKeyboardSelection<T>(items: Signal<T[]>) {
  return signalStoreFeature(
    // Linked state: back to the first item whenever the list changes, but still writable
    withLinkedState(() => ({ highlighted: (): number => (items().length ? 0 : -1) })),
    withComputed(({ highlighted }) => ({
      highlightedItem: computed(() => items()[highlighted()] ?? null),
    })),
    withMethods((store) => ({
      move(step: 1 | -1): void {
        const count = items().length;
        if (count) patchState(store, { highlighted: (store.highlighted() + step + count) % count });
      },
    })),
  );
}
