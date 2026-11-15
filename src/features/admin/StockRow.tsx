import type { StockLot } from '@/features/admin/stockLots';

type StockRowProps = {
  lot: StockLot;
  pinned: boolean;
  onTogglePin: (lotId: string) => void;
};

export function StockRow({ lot, pinned, onTogglePin }: StockRowProps) {
  return (
    <tr className={pinned ? 'pinned' : undefined}>
      <td>{lot.lotId}</td>
      <td>{lot.title}</td>
      <td>{lot.author}</td>
      <td>{lot.warehouse}</td>
      <td className="num">{lot.onHand}</td>
      <td>
        <button type="button" aria-pressed={pinned} aria-label={`Pin ${lot.lotId}`} onClick={() => onTogglePin(lot.lotId)}>
          {pinned ? 'Unpin' : 'Pin'}
        </button>
      </td>
    </tr>
  );
}
