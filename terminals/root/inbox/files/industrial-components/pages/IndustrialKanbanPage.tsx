import { useState, useEffect } from 'react';
import { useAuth } from '../hooks/useAuth';
import { useKanban } from '../hooks/useKanban';
import { MetricGauge } from '../components/Industrial/MetricGauge';
import { JogWheel } from '../components/Industrial/JogWheel';

const TERMINALS = [
  'kernel', 'orch', 'fe', 'joinery', 'abstractions', 'cutting',
  'inventory', 'procurement', 'sales', 'identity', 'infra', 'e2e',
  'architect', 'librarian', 'nexus', 'root', 'conductor'
];

export function IndustrialKanbanPage() {
  const { authToken } = useAuth();
  const {
    boardData,
    metrics,
    isLoading,
    error,
    loadBoard,
    loadMetrics,
  } = useKanban(authToken);

  const [selectedTerminalIndex, setSelectedTerminalIndex] = useState(0);
  const [terminalFilter, setTerminalFilter] = useState<string | null>(null);

  useEffect(() => {
    loadBoard();
    loadMetrics();
  }, [loadBoard, loadMetrics]);

  const handleRefresh = () => {
    loadBoard();
    loadMetrics();
  };

  // Calculate metrics safely
  const discoveryWip = boardData?.discovery?.totals
    ? Object.values(boardData.discovery.totals).reduce((a, b) => a + b, 0)
    : 0;
  const deliveryWip = boardData?.delivery?.totals
    ? (boardData.delivery.totals.inbox || 0) + (boardData.delivery.totals.active || 0)
    : 0;
  const activeSessions = boardData?.delivery?.activeSessions?.length || 0;
  const throughput = metrics?.delivery?.throughput?.items_per_day || 0;

  // Filter and sort swimlanes
  const swimlanes = boardData?.delivery?.swimlanes?.sort((a, b) => {
    if (a.sessionActive !== b.sessionActive) return b.sessionActive ? 1 : -1;
    return (b.totals?.inbox || 0) - (a.totals?.inbox || 0);
  }) || [];

  // Filter swimlanes based on selection
  const filteredSwimlanes = terminalFilter
    ? swimlanes.filter(s => s.terminal === terminalFilter)
    : swimlanes;

  if (isLoading && !boardData) {
    return (
      <div className="ind-chassis" style={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        padding: '48px',
        margin: '24px',
      }}>
        <div style={{ textAlign: 'center' }}>
          <div style={{
            width: '48px',
            height: '48px',
            margin: '0 auto 16px',
            border: '3px solid transparent',
            borderTopColor: '#4ade80',
            borderRadius: '50%',
            animation: 'spin-detent 1s linear infinite',
          }} />
          <div style={{
            fontFamily: "'Oswald', sans-serif",
            fontSize: '14px',
            color: '#4ade80',
            letterSpacing: '0.2em',
            textShadow: '0 0 6px #4ade80',
          }}>
            LOADING KANBAN...
          </div>
        </div>
      </div>
    );
  }

  if (error && !boardData) {
    return (
      <div className="ind-chassis" style={{
        padding: '32px',
        margin: '24px',
        textAlign: 'center',
      }}>
        <div style={{
          fontFamily: "'Oswald', sans-serif",
          fontSize: '18px',
          color: '#ef4444',
          letterSpacing: '0.15em',
          marginBottom: '16px',
        }}>
          SYSTEM ERROR
        </div>
        <div style={{
          fontFamily: "'IBM Plex Mono', monospace",
          fontSize: '12px',
          color: '#94a3b8',
          marginBottom: '24px',
        }}>
          {error}
        </div>
        <button
          onClick={handleRefresh}
          className="ind-button"
          style={{ padding: '10px 24px' }}
        >
          RETRY CONNECTION
        </button>
      </div>
    );
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
      {/* Metrics Panel */}
      <div className="ind-chassis" style={{ padding: '14px 18px' }}>
        <div style={{
          display: 'flex',
          alignItems: 'flex-start',
          justifyContent: 'space-between',
          flexWrap: 'wrap',
          gap: '12px',
        }}>
          <div style={{
            display: 'flex',
            gap: '10px',
            flexWrap: 'wrap',
          }}>
            <MetricGauge
              label="DISCOVERY"
              value={discoveryWip}
              max={20}
              color="purple"
              size="md"
            />
            <MetricGauge
              label="DELIVERY"
              value={deliveryWip}
              max={50}
              color="blue"
              size="md"
            />
            <MetricGauge
              label="ACTIVE"
              value={activeSessions}
              max={TERMINALS.length}
              color="green"
              size="md"
            />
            <MetricGauge
              label="RATE"
              value={parseFloat(String(throughput)) || 0}
              max={10}
              color="amber"
              size="md"
            />
          </div>

          <div style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
            <JogWheel
              items={['ALL', ...TERMINALS.map(t => t.toUpperCase())]}
              selectedIndex={terminalFilter ? TERMINALS.indexOf(terminalFilter) + 1 : 0}
              onSelect={(idx) => {
                if (idx === 0) {
                  setTerminalFilter(null);
                  setSelectedTerminalIndex(0);
                } else {
                  setSelectedTerminalIndex(idx - 1);
                }
              }}
              onCommit={() => {
                if (selectedTerminalIndex === 0 && !terminalFilter) {
                  setTerminalFilter(null);
                } else {
                  const terminal = TERMINALS[selectedTerminalIndex];
                  setTerminalFilter(terminalFilter === terminal ? null : terminal);
                }
              }}
            />
            <button
              onClick={handleRefresh}
              disabled={isLoading}
              className="ind-button"
              style={{
                padding: '12px 18px',
                opacity: isLoading ? 0.5 : 1,
              }}
            >
              {isLoading ? 'SYNC...' : 'REFRESH'}
            </button>
          </div>
        </div>
      </div>

      {/* Discovery Track */}
      {boardData?.discovery && (
        <div className="ind-chassis" style={{ padding: '14px 18px' }}>
          <div style={{
            fontFamily: "'Oswald', sans-serif",
            fontSize: '12px',
            fontWeight: 600,
            color: '#0a0c0f',
            letterSpacing: '0.18em',
            textShadow: '0 1px 0 rgba(255,255,255,0.2)',
            marginBottom: '12px',
          }}>
            DISCOVERY TRACK
          </div>
          <div style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(5, 1fr)',
            gap: '8px',
          }}>
            {(['ideas', 'selected', 'debate', 'consensus', 'queue'] as const).map((stage) => {
              // Support both old (columns.ideas) and new (ideas) API structures
              const discoveryData = boardData.discovery as unknown as Record<string, unknown>;
              const stageItems = boardData.discovery?.columns?.[stage] || discoveryData?.[stage] || [];
              const itemsArray = Array.isArray(stageItems) ? stageItems : [];
              return (
                <div key={stage} style={{
                  background: 'linear-gradient(180deg, #050e0a 0%, #0a1814 100%)',
                  borderRadius: '6px',
                  padding: '10px',
                  boxShadow: 'inset 0 1px 4px rgba(0,0,0,0.9), inset 0 0 16px rgba(74,222,128,0.06)',
                }}>
                  <div style={{
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'space-between',
                    marginBottom: '8px',
                  }}>
                    <span style={{
                      fontFamily: "'IBM Plex Mono', monospace",
                      fontSize: '9px',
                      color: '#16a34a',
                      letterSpacing: '0.15em',
                      textTransform: 'uppercase',
                    }}>
                      {stage}
                    </span>
                    <span style={{
                      fontFamily: "'Share Tech Mono', monospace",
                      fontSize: '14px',
                      color: '#4ade80',
                      textShadow: '0 0 6px #4ade80',
                    }}>
                      {boardData.discovery?.totals?.[stage] || itemsArray.length || 0}
                    </span>
                  </div>
                  <div style={{
                    display: 'flex',
                    flexDirection: 'column',
                    gap: '4px',
                    maxHeight: '120px',
                    overflowY: 'auto',
                  }}>
                    {itemsArray.slice(0, 5).map((item: { id: string; title?: string }) => (
                      <div key={item.id} style={{
                        background: 'rgba(74, 222, 128, 0.08)',
                        borderRadius: '3px',
                        padding: '6px 8px',
                        border: '1px solid rgba(74, 222, 128, 0.15)',
                      }}>
                        <div style={{
                          fontFamily: "'IBM Plex Mono', monospace",
                          fontSize: '9px',
                          color: '#94a3b8',
                          whiteSpace: 'nowrap',
                          overflow: 'hidden',
                          textOverflow: 'ellipsis',
                        }}>
                          {item.title || item.id}
                        </div>
                      </div>
                    ))}
                    {itemsArray.length > 5 && (
                      <div style={{
                        fontFamily: "'IBM Plex Mono', monospace",
                        fontSize: '8px',
                        color: '#6b7280',
                        textAlign: 'center',
                      }}>
                        +{itemsArray.length - 5} more
                      </div>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      )}

      {/* Delivery Track - Terminal Swimlanes */}
      <div className="ind-chassis" style={{ padding: '14px 18px' }}>
        <div style={{
          fontFamily: "'Oswald', sans-serif",
          fontSize: '12px',
          fontWeight: 600,
          color: '#0a0c0f',
          letterSpacing: '0.18em',
          textShadow: '0 1px 0 rgba(255,255,255,0.2)',
          marginBottom: '12px',
        }}>
          DELIVERY TRACK
        </div>
        <div style={{
          display: 'flex',
          flexDirection: 'column',
          gap: '8px',
        }}>
          {filteredSwimlanes.length === 0 ? (
            <div style={{
              textAlign: 'center',
              padding: '24px',
              background: 'linear-gradient(180deg, #050e0a 0%, #0a1814 100%)',
              borderRadius: '6px',
            }}>
              <div style={{
                fontFamily: "'IBM Plex Mono', monospace",
                fontSize: '12px',
                color: '#6b7280',
              }}>
                No delivery data available
              </div>
            </div>
          ) : (
            filteredSwimlanes.map((swimlane) => (
              <div key={swimlane.terminal} style={{
                background: 'linear-gradient(180deg, #050e0a 0%, #0a1814 100%)',
                borderRadius: '6px',
                padding: '10px 12px',
                boxShadow: 'inset 0 1px 4px rgba(0,0,0,0.9), inset 0 0 16px rgba(74,222,128,0.06)',
              }}>
                <div style={{
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'space-between',
                  marginBottom: '8px',
                }}>
                  <div style={{
                    display: 'flex',
                    alignItems: 'center',
                    gap: '8px',
                  }}>
                    <div
                      className={`ind-led ${swimlane.sessionActive ? 'ind-led-green' : 'ind-led-off'}`}
                      style={{ width: '8px', height: '8px' }}
                    />
                    <span style={{
                      fontFamily: "'Oswald', sans-serif",
                      fontSize: '11px',
                      fontWeight: 600,
                      color: swimlane.sessionActive ? '#4ade80' : '#6b7280',
                      letterSpacing: '0.15em',
                      textTransform: 'uppercase',
                      textShadow: swimlane.sessionActive ? '0 0 4px #4ade80' : 'none',
                    }}>
                      {swimlane.terminal}
                    </span>
                    <span style={{
                      fontFamily: "'IBM Plex Mono', monospace",
                      fontSize: '8px',
                      color: swimlane.sessionActive ? '#16a34a' : '#4b5563',
                      letterSpacing: '0.1em',
                    }}>
                      {swimlane.sessionActive ? 'ACTIVE' : 'IDLE'}
                    </span>
                  </div>
                  <div style={{
                    display: 'flex',
                    gap: '12px',
                    fontFamily: "'IBM Plex Mono', monospace",
                    fontSize: '9px',
                    color: '#6b7280',
                  }}>
                    <span>IN: <span style={{ color: '#fbbf24' }}>{swimlane.totals?.inbox || 0}</span></span>
                    <span>ACT: <span style={{ color: '#4ade80' }}>{swimlane.totals?.active || 0}</span></span>
                    <span>ARC: <span style={{ color: '#6b7280' }}>{swimlane.totals?.archive || 0}</span></span>
                  </div>
                </div>
                <div style={{
                  display: 'grid',
                  gridTemplateColumns: 'repeat(4, 1fr)',
                  gap: '6px',
                }}>
                  {(['inbox', 'active', 'review', 'done'] as const).map((col) => (
                    <div key={col} style={{
                      background: 'rgba(0,0,0,0.3)',
                      borderRadius: '4px',
                      padding: '6px',
                    }}>
                      <div style={{
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'space-between',
                        marginBottom: '4px',
                      }}>
                        <span style={{
                          fontFamily: "'IBM Plex Mono', monospace",
                          fontSize: '8px',
                          color: '#4b5563',
                          textTransform: 'uppercase',
                          letterSpacing: '0.1em',
                        }}>
                          {col}
                        </span>
                        <span style={{
                          fontFamily: "'Share Tech Mono', monospace",
                          fontSize: '10px',
                          color: col === 'inbox' ? '#fbbf24' : col === 'active' ? '#4ade80' : '#6b7280',
                        }}>
                          {swimlane.columns?.[col]?.length || 0}
                        </span>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            ))
          )}
        </div>
      </div>
    </div>
  );
}
