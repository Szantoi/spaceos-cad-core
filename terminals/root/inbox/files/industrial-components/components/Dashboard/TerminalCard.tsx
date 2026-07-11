import type { Terminal, TerminalState } from '../../types/dashboard';

interface TerminalCardProps {
  terminal: Terminal;
  onClick?: (terminal: Terminal) => void;
}

const stateColors: Record<TerminalState, { bg: string; dot: string; text: string }> = {
  WORKING: {
    bg: 'bg-emerald-500/10 border-emerald-500/30',
    dot: 'bg-emerald-500',
    text: 'text-emerald-400',
  },
  IDLE: {
    bg: 'bg-amber-500/10 border-amber-500/30',
    dot: 'bg-amber-500',
    text: 'text-amber-400',
  },
  OFFLINE: {
    bg: 'bg-zinc-500/10 border-zinc-500/30',
    dot: 'bg-zinc-500',
    text: 'text-zinc-400',
  },
};

export function TerminalCard({ terminal, onClick }: TerminalCardProps) {
  const colors = stateColors[terminal.state];

  return (
    <div
      onClick={() => onClick?.(terminal)}
      className={`
        relative p-4 rounded-lg border cursor-pointer
        transition-all duration-200 hover:scale-[1.02]
        ${colors.bg}
      `}
    >
      {/* Status dot */}
      <div className="absolute top-3 right-3 flex items-center gap-2">
        {terminal.unreadCount > 0 && (
          <span className="px-2 py-0.5 text-xs font-medium bg-[var(--accent)] text-white rounded-full">
            {terminal.unreadCount}
          </span>
        )}
        <span className={`w-2.5 h-2.5 rounded-full ${colors.dot} animate-pulse`} />
      </div>

      {/* Terminal name */}
      <h3 className="text-lg font-semibold text-[var(--text-primary)] uppercase">
        {terminal.name}
      </h3>

      {/* State */}
      <p className={`text-sm ${colors.text} mt-1`}>
        {terminal.state}
        {terminal.taskId && (
          <span className="text-[var(--text-muted)] ml-2">
            ({terminal.taskId})
          </span>
        )}
      </p>

      {/* Type badge */}
      <div className="mt-3">
        <span className={`
          text-xs px-2 py-1 rounded
          ${terminal.type === 'priority' ? 'bg-purple-500/20 text-purple-300' : ''}
          ${terminal.type === 'product' ? 'bg-blue-500/20 text-blue-300' : ''}
          ${terminal.type === 'support' ? 'bg-gray-500/20 text-gray-300' : ''}
        `}>
          {terminal.type}
        </span>
      </div>
    </div>
  );
}
