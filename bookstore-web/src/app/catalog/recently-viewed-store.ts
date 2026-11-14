import { patchState, signalMethod, signalStore, type, withMethods } from '@ngrx/signals';
import { entityConfig, prependEntity, removeEntities, removeEntity, withEntities } from '@ngrx/signals/entities';
import type { Book } from '../models';

// Named collection: viewedEntities, viewedIds and viewedEntityMap instead of entities, ids and entityMap
const viewedConfig = entityConfig({
  entity: type<Book>(),
  collection: 'viewed',
  selectId: (book) => book.isbn,
});

export const RecentlyViewedStore = signalStore(
  { providedIn: 'root' },
  withEntities(viewedConfig),
  withMethods((store) => ({
    // Takes a value, a signal or a computation; a synchronous update needs no RxJS
    track: signalMethod<Book | undefined>((book) => {
      if (!book) return;
      patchState(store, removeEntity(book.isbn, viewedConfig), prependEntity(book, viewedConfig));
      patchState(store, removeEntities(store.viewedIds().slice(3), viewedConfig));
    }),
  })),
);
