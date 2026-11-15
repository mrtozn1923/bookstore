import { Activity, Profiler, useDeferredValue, useState } from 'react';
import { reportRender } from './reportRender';
import { makeStockLots, type StockLot } from './stockLots';
import { StockTable } from './StockTable';

type Tab = 'all' | 'pinned';

export function StockPage() {
  const [lots] = useState(() => makeStockLots(5000));
  const [pinned, setPinned] = useState<ReadonlySet<string>>(() => new Set());
  const [tab, setTab] = useState<Tab>('all');

  function togglePin(lotId: string) {
    setPinned((current) => {
      const next = new Set(current);
      if (next.has(lotId)) {
        next.delete(lotId);
      } else {
        next.add(lotId);
      }
      return next;
    });
  }

  return (
    <>
      <h1>Stock lots</h1>
      <div className="tabs" role="tablist">
        <button type="button" role="tab" aria-selected={tab === 'all'} onClick={() => setTab('all')}>All lots</button>
        <button type="button" role="tab" aria-selected={tab === 'pinned'} onClick={() => setTab('pinned')}>Pinned ({pinned.size})</button>
      </div>
      {/* Hidden tabs keep their state and DOM; their effects are cleaned up until they are shown again */}
      <Activity mode={tab === 'all' ? 'visible' : 'hidden'}>
        <AllLotsTab lots={lots} pinned={pinned} onTogglePin={togglePin} />
      </Activity>
      <Activity mode={tab === 'pinned' ? 'visible' : 'hidden'}>
        <StockTable lots={lots.filter((lot) => pinned.has(lot.lotId))} pinned={pinned} onTogglePin={togglePin} />
      </Activity>
    </>
  );
}

type AllLotsTabProps = {
  lots: StockLot[];
  pinned: ReadonlySet<string>;
  onTogglePin: (lotId: string) => void;
};

function AllLotsTab({ lots, pinned, onTogglePin }: AllLotsTabProps) {
  const [filter, setFilter] = useState('');
  // The input shows `filter` at once; the list follows `deferredFilter`, which may lag behind while React is busy
  const deferredFilter = useDeferredValue(filter);
  const term = deferredFilter.trim().toLowerCase();
  const visible = lots.filter((lot) => lot.title.toLowerCase().includes(term) || lot.author.toLowerCase().includes(term));

  return (
    <>
      <input type="search" aria-label="Filter stock" placeholder="Filter by title or author" value={filter}
        onChange={(e) => setFilter(e.target.value)} />
      <p className="muted" role="status">{visible.length} lot(s){filter !== deferredFilter ? ' · updating…' : ''}</p>
      <Profiler id="StockTable" onRender={reportRender}>
        <StockTable lots={visible} pinned={pinned} onTogglePin={onTogglePin} />
      </Profiler>
    </>
  );
}
