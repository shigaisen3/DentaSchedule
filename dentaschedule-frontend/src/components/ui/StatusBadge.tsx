import { cn } from '../../utils/cn';

const styles: Record<string, string> = {
  Pending: 'bg-yellow-100 text-yellow-800',
  Approved: 'bg-green-100 text-green-800',
  Cancelled: 'bg-red-100 text-red-800',
};

export default function StatusBadge({ status }: { status: string }) {
  return (
    <span className={cn('inline-block rounded-full px-2.5 py-0.5 text-xs font-medium', styles[status] ?? 'bg-gray-100 text-gray-800')}>
      {status}
    </span>
  );
}
