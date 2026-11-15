import { useVirtualizer } from '@tanstack/react-virtual';
import { useRef } from 'react';
import { StockRow } from './StockRow';
import type { StockLot } from './stockLots';

type StockTableProps = {
  lots: StockLot[];
  pinned: ReadonlySet<string>;
  onTogglePin: (lotId: string) => void;
};

const rowHeight = 30;

// Virtualized: only the rows inside the scroll box (plus a few extra) are in the DOM
export function StockTable({ lots, pinned, onTogglePin }: StockTableProps) {
  const scrollBox = useRef<HTMLDivElement>(null);
  // Known and accepted: the compiler skips this small component; StockRow below is still compiled
  // oxlint-disable-next-line react/incompatible-library
  const virtualizer = useVirtualizer({
    count: lots.length,
    getScrollElement: () => scrollBox.current,
    estimateSize: () => rowHeight,
    overscan: 10,
  });
  const items = virtualizer.getVirtualItems();
  // Empty rows above and below keep the scrollbar the size of the full list
  const before = items.length > 0 ? items[0].start : 0;
  const after = items.length > 0 ? virtualizer.getTotalSize() - items[items.length - 1].end : 0;

  return (
    <div ref={scrollBox} className="stock-scroll">
      <table className="stock">
        <thead>
          <tr><th>Lot</th><th>Title</th><th>Author</th><th>Warehouse</th><th>On hand</th><th /></tr>
        </thead>
        <tbody>
          {before > 0 && <tr style={{ height: before }} aria-hidden="true" />}
          {items.map((item) => {
            const lot = lots[item.index];
            return <StockRow key={lot.lotId} lot={lot} pinned={pinned.has(lot.lotId)} onTogglePin={onTogglePin} />;
          })}
          {after > 0 && <tr style={{ height: after }} aria-hidden="true" />}
        </tbody>
      </table>
    </div>
  );
}
