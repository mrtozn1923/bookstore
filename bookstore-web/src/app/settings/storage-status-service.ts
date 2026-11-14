import { Service } from '@angular/core';

export interface StorageStatus {
  usageMb: number;
  quotaMb: number;
  persisted: boolean;
}

@Service()
export class StorageStatusService {
  async status(): Promise<StorageStatus> {
    const { usage = 0, quota = 0 } = await navigator.storage.estimate();
    return {
      usageMb: Math.round(usage / 1024 / 1024),
      quotaMb: Math.round(quota / 1024 / 1024),
      persisted: await navigator.storage.persisted(),
    };
  }

  // call it from a click handler, when the user asked to keep offline data
  requestPersistence(): Promise<boolean> {
    return navigator.storage.persist();
  }
}
