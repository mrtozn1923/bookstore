import { Service, signal } from '@angular/core';

// One tab of Bookstore is the leader: it holds a Web Lock for as long as it lives.
// When it closes (or crashes), the browser releases the lock and the next waiting tab gets it.
@Service()
export class TabLeader {
  readonly isLeader = signal(false);

  constructor() {
    void navigator.locks.request('bookstore-leader', () => {
      this.isLeader.set(true);
      console.log('leader: this tab is the leader');
      return new Promise<never>(() => {}); // never resolves: keep the lock until the tab goes away
    });
  }
}
