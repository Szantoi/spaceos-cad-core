import { useState, useCallback } from 'react';
import { JogWheel } from './JogWheel';
import type { Terminal, TerminalState } from '../../types/dashboard';

interface TerminalRackProps {
  terminals: Terminal[];
  onTerminalSelect?: (terminal: Terminal) => void;
}

// Internal type for display purposes
interface DisplayTerminal {
  name: string;
  state: TerminalState;
  unreadCount: number;
  isReal: boolean;
  terminal?: Terminal;
}

// 17 terminal channels in order
const TERMINAL_ORDER = [
  'root', 'conductor', 'architect', 'librarian', 'nexus',
  'kernel', 'orch', 'fe', 'fe2',
  'joinery', 'cutting', 'abstractions',
  'inventory', 'procurement', 'sales', 'identity',
  'infra', 'e2e', 'tester'
];

const getStatusColor = (state: TerminalState): { led: string; glow: string } => {
  switch (state) {
    case 'WORKING':
      return { led: 'radial-gradient(circle at 30% 30%, #d1fae5 0%, #4ade80 45%, #16a34a 85%)', glow: '0 0 6px #4ade80' };
    case 'IDLE':
      return { led: 'radial-gradient(circle at 30% 30%, #bae6fd 0%, #38bdf8 45%, #0369a1 85%)', glow: '0 0 6px #38bdf8' };
    case 'OFFLINE':
    default:
      return { led: 'radial-gradient(circle at 30% 30%, #475569 0%, #334155 60%, #1e293b 100%)', glow: 'none' };
  }
};

export const TerminalRack: React.FC<TerminalRackProps> = ({
  terminals,
  onTerminalSelect,
}) => {
  const [selectedIndex, setSelectedIndex] = useState(0);

  // Build terminal map for quick lookup
  const terminalMap = new Map(terminals.map(t => [t.name.toLowerCase(), t]));

  // Get ordered list matching TERMINAL_ORDER
  const orderedTerminals: DisplayTerminal[] = TERMINAL_ORDER.map(name => {
    const real = terminalMap.get(name);
    return real
      ? { name: real.name, state: real.state, unreadCount: real.unreadCount, isReal: true, terminal: real }
      : { name, state: 'OFFLINE' as TerminalState, unreadCount: 0, isReal: false };
  });

  const terminalNames = orderedTerminals.map(t => t.name.toUpperCase());
  const selectedTerminal = orderedTerminals[selectedIndex];

  const handleSelect = useCallback((index: number) => {
    setSelectedIndex(index);
  }, []);

  const handleCommit = useCallback(() => {
    if (selectedTerminal?.terminal && onTerminalSelect) {
      onTerminalSelect(selectedTerminal.terminal);
    }
  }, [selectedTerminal, onTerminalSelect]);

  return (
    <div style={{
      display: 'flex',
      gap: '16px',
      flexWrap: 'wrap',
    }}>
      {/* Left side: Jog wheel selector */}
      <div style={{ flex: '0 0 auto' }}>
        <JogWheel
          items={terminalNames}
          selectedIndex={selectedIndex}
          onSelect={handleSelect}
          onCommit={handleCommit}
        />
      </div>

      {/* Right side: Terminal rack grid */}
      <div style={{
        flex: 1,
        minWidth: '320px',
        padding: '14px',
        background: 'linear-gradient(180deg, #3a3d42 0%, #2c2f34 50%, #1a1d22 100%)',
        borderRadius: '10px',
        boxShadow: `
          inset 0 1px 0 rgba(255,255,255,0.16),
          inset 0 -2px 0 rgba(0,0,0,0.55),
          0 2px 6px rgba(0,0,0,0.5)`,
      }}>
        {/* Rack title */}
        <div style={{
          marginBottom: '12px',
          padding: '6px 10px',
          background: 'linear-gradient(180deg, #050e0a, #0a1814)',
          borderRadius: '4px',
          boxShadow: 'inset 0 1px 4px rgba(0,0,0,0.9)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
        }}>
          <span style={{
            fontFamily: "'Oswald', sans-serif",
            fontSize: '12px',
            fontWeight: 700,
            color: '#4ade80',
            letterSpacing: '0.2em',
            textShadow: '0 0 6px #4ade80',
          }}>
            TERMINAL RACK
          </span>
          <span style={{
            fontFamily: "'IBM Plex Mono', monospace",
            fontSize: '10px',
            color: '#16a34a',
            letterSpacing: '0.15em',
          }}>
            {terminals.filter(t => t.state === 'WORKING').length} ACTIVE
          </span>
        </div>

        {/* Terminal grid - 4 columns */}
        <div style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(4, 1fr)',
          gap: '8px',
        }}>
          {orderedTerminals.map((terminal, idx) => {
            const statusColors = getStatusColor(terminal.state);
            const isSelected = idx === selectedIndex;

            return (
              <button
                key={terminal.name}
                onClick={() => {
                  setSelectedIndex(idx);
                  if (onTerminalSelect && terminal.terminal) {
                    onTerminalSelect(terminal.terminal);
                  }
                }}
                style={{
                  position: 'relative',
                  padding: '8px 6px',
                  border: 'none',
                  cursor: 'pointer',
                  background: isSelected
                    ? 'linear-gradient(180deg, #2c2f34 0%, #1a1d22 50%, #0a0c0f 100%)'
                    : 'linear-gradient(180deg, #4a4d52 0%, #3a3d42 100%)',
                  borderRadius: '5px',
                  boxShadow: isSelected
                    ? `inset 0 2px 4px rgba(0,0,0,0.9), 0 0 8px ${statusColors.glow !== 'none' ? statusColors.glow.split(' ').pop() : 'transparent'}`
                    : 'inset 0 1px 0 rgba(255,255,255,0.14), inset 0 -1px 0 rgba(0,0,0,0.5)',
                  display: 'flex',
                  flexDirection: 'column',
                  alignItems: 'center',
                  gap: '4px',
                  transition: 'all 100ms ease',
                }}
              >
                {/* Status LED */}
                <div style={{
                  width: '8px',
                  height: '8px',
                  borderRadius: '50%',
                  background: statusColors.led,
                  boxShadow: statusColors.glow,
                }} />

                {/* Terminal name */}
                <span style={{
                  fontFamily: "'Oswald', sans-serif",
                  fontSize: '9px',
                  fontWeight: 600,
                  color: isSelected ? '#e2e8f0' : '#0a0c0f',
                  letterSpacing: '0.12em',
                  textShadow: isSelected ? 'none' : '0 1px 0 rgba(255,255,255,0.2)',
                  textTransform: 'uppercase',
                }}>
                  {terminal.name.length > 6 ? terminal.name.slice(0, 5) + '..' : terminal.name}
                </span>

                {/* Unread count */}
                <div style={{
                  fontFamily: "'IBM Plex Mono', monospace",
                  fontSize: '8px',
                  color: isSelected ? '#4ade80' : '#16a34a',
                  textShadow: isSelected ? '0 0 4px #4ade80' : 'none',
                }}>
                  {terminal.unreadCount > 0 ? `▸${terminal.unreadCount}` : '—'}
                </div>
              </button>
            );
          })}
        </div>
      </div>
    </div>
  );
};
