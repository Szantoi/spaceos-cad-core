import type { Terminal } from '../../types/dashboard';
import { TerminalCard } from './TerminalCard';
import { TERMINAL_CATEGORIES } from '../../types/dashboard';

interface TerminalGridProps {
  terminals: Terminal[];
  onTerminalClick?: (terminal: Terminal) => void;
}

export function TerminalGrid({ terminals, onTerminalClick }: TerminalGridProps) {
  const getTerminalsByCategory = (category: string) => {
    const categoryTerminals = TERMINAL_CATEGORIES[category]?.terminals || [];
    return terminals.filter(t => categoryTerminals.includes(t.id.toLowerCase()));
  };

  return (
    <div className="space-y-8">
      {Object.entries(TERMINAL_CATEGORIES).map(([key, { label }]) => {
        const categoryTerminals = getTerminalsByCategory(key);
        if (categoryTerminals.length === 0) return null;

        return (
          <div key={key}>
            <h2 className="text-lg font-semibold text-[var(--text-primary)] mb-4 flex items-center gap-2">
              <span className={`
                w-2 h-2 rounded-full
                ${key === 'priority' ? 'bg-purple-500' : ''}
                ${key === 'product' ? 'bg-blue-500' : ''}
                ${key === 'support' ? 'bg-gray-500' : ''}
              `} />
              {label} Terminals
              <span className="text-sm font-normal text-[var(--text-secondary)]">
                ({categoryTerminals.length})
              </span>
            </h2>
            <div className={`grid gap-4 ${
              key === 'priority' ? 'grid-cols-2 md:grid-cols-2' :
              key === 'product' ? 'grid-cols-2 md:grid-cols-3 lg:grid-cols-5' :
              'grid-cols-2 md:grid-cols-3 lg:grid-cols-5'
            }`}>
              {categoryTerminals.map(terminal => (
                <TerminalCard
                  key={terminal.id}
                  terminal={terminal}
                  onClick={onTerminalClick}
                />
              ))}
            </div>
          </div>
        );
      })}
    </div>
  );
}
