import React, { useState, useRef, useCallback } from 'react';

interface JogWheelProps {
  items: string[];
  selectedIndex: number;
  onSelect: (index: number) => void;
  onCommit?: () => void;
}

export const JogWheel: React.FC<JogWheelProps> = ({
  items,
  selectedIndex,
  onSelect,
  onCommit,
}) => {
  const [angle, setAngle] = useState(0);
  const [isDragging, setIsDragging] = useState(false);
  const jogRef = useRef<HTMLDivElement>(null);
  const dragStartAngle = useRef(0);
  const dragStartRot = useRef(0);

  const total = Math.max(items.length, 1);
  const step = 360 / Math.max(total, 12);

  // Generate tick marks
  const ticks = Array.from({ length: 24 }, (_, i) => {
    const tickAngle = i * 15;
    const isMajor = i % 6 === 0;
    return {
      angle: `${tickAngle}deg`,
      w: isMajor ? '3px' : '2px',
      h: isMajor ? '10px' : '6px',
      color: isMajor ? '#4ade80' : 'rgba(255,255,255,0.2)',
      glow: isMajor ? '0 0 4px #4ade80' : 'none',
    };
  });

  const handlePointerDown = useCallback((e: React.PointerEvent) => {
    if (!jogRef.current) return;
    (e.target as HTMLElement).setPointerCapture?.(e.pointerId);

    const rect = jogRef.current.getBoundingClientRect();
    const cx = rect.left + rect.width / 2;
    const cy = rect.top + rect.height / 2;
    const a = Math.atan2(e.clientY - cy, e.clientX - cx) * 180 / Math.PI;

    dragStartAngle.current = a;
    dragStartRot.current = angle;
    setIsDragging(true);
  }, [angle]);

  const handlePointerMove = useCallback((e: React.PointerEvent) => {
    if (!isDragging || !jogRef.current) return;

    const rect = jogRef.current.getBoundingClientRect();
    const cx = rect.left + rect.width / 2;
    const cy = rect.top + rect.height / 2;
    const a = Math.atan2(e.clientY - cy, e.clientX - cx) * 180 / Math.PI;

    let delta = a - dragStartAngle.current;
    if (delta > 180) delta -= 360;
    if (delta < -180) delta += 360;

    setAngle(dragStartRot.current + delta);
  }, [isDragging]);

  const handlePointerUp = useCallback(() => {
    if (!isDragging) return;

    // Snap to nearest detent
    const snapped = Math.round(angle / step) * step;
    let idx = Math.round(snapped / step) % total;
    if (idx < 0) idx += total;

    setAngle(snapped);
    setIsDragging(false);
    onSelect(idx);
  }, [isDragging, angle, step, total, onSelect]);

  const handlePrev = () => {
    const newIdx = (selectedIndex - 1 + total) % total;
    setAngle(angle - step);
    onSelect(newIdx);
  };

  const handleNext = () => {
    const newIdx = (selectedIndex + 1) % total;
    setAngle(angle + step);
    onSelect(newIdx);
  };

  const selectedLabel = items[selectedIndex] || '---';

  return (
    <div style={{
      display: 'flex', flexDirection: 'column', gap: '10px',
      position: 'relative', padding: '14px',
      background: 'linear-gradient(180deg, #3a3d42 0%, #2c2f34 50%, #1a1d22 100%)',
      borderRadius: '10px',
      boxShadow: `
        inset 0 1px 0 rgba(255,255,255,0.16),
        inset 0 -2px 0 rgba(0,0,0,0.55),
        0 2px 6px rgba(0,0,0,0.5),
        0 -12px 24px rgba(0,0,0,0.4)`
    }}>
      {/* Corner screws */}
      {['top:6px;left:6px', 'top:6px;right:6px', 'bottom:6px;left:6px', 'bottom:6px;right:6px'].map((pos, i) => (
        <div key={i} style={{
          position: 'absolute', ...Object.fromEntries(pos.split(';').map(p => p.split(':'))),
          width: '10px', height: '10px', borderRadius: '50%',
          background: 'radial-gradient(circle at 35% 35%, #b8bdc4, #6b6e73 45%, #2c2f34 85%, #1a1d22)',
          boxShadow: 'inset 0 -1px 1px rgba(0,0,0,0.6), 0 1px 1px rgba(0,0,0,0.4)'
        }} />
      ))}

      {/* Display readout */}
      <div style={{
        margin: '0 auto', padding: '8px 12px', minWidth: '220px', borderRadius: '4px',
        background: 'linear-gradient(180deg, #050e0a, #0a1814)',
        boxShadow: 'inset 0 1px 4px rgba(0,0,0,0.9), inset 0 0 16px rgba(74,222,128,0.06)',
        display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: '14px'
      }}>
        <div style={{ display: 'flex', flexDirection: 'column', gap: '1px' }}>
          <span style={{
            fontFamily: "'IBM Plex Mono', monospace", fontSize: '8px',
            color: '#16a34a', letterSpacing: '0.2em'
          }}>▸ SELECTED</span>
          <span style={{
            fontFamily: "'Share Tech Mono', monospace", fontSize: '16px',
            color: '#4ade80', textShadow: '0 0 6px #4ade80', letterSpacing: '0.06em'
          }}>{selectedLabel}</span>
        </div>
        <div style={{
          display: 'flex', flexDirection: 'column', gap: '1px', alignItems: 'flex-end'
        }}>
          <span style={{
            fontFamily: "'IBM Plex Mono', monospace", fontSize: '8px',
            color: '#16a34a', letterSpacing: '0.2em'
          }}>CH</span>
          <span style={{
            fontFamily: "'Share Tech Mono', monospace", fontSize: '16px',
            color: '#4ade80', textShadow: '0 0 6px #4ade80'
          }}>{String(selectedIndex + 1).padStart(2, '0')}</span>
        </div>
      </div>

      {/* Wheel area */}
      <div style={{
        display: 'flex', alignItems: 'center', justifyContent: 'center',
        gap: '18px', padding: '14px 6px 6px'
      }}>
        {/* Left button - PREV */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
          <button
            onClick={handlePrev}
            className="bumper"
            style={{
              width: '54px', height: '54px', borderRadius: '8px', border: 'none', cursor: 'pointer',
              background: 'linear-gradient(180deg, #4a4d52 0%, #2c2f34 100%)',
              boxShadow: 'inset 0 1px 0 rgba(255,255,255,0.18), inset 0 -2px 0 rgba(0,0,0,0.55), 0 2px 4px rgba(0,0,0,0.5)',
              fontFamily: "'IBM Plex Mono', monospace", fontSize: '18px',
              color: '#0a0c0f', textShadow: '0 1px 0 rgba(255,255,255,0.18)'
            }}
          >◀</button>
          <span style={{
            fontFamily: "'Oswald', sans-serif", fontSize: '8px', fontWeight: 600,
            color: '#0a0c0f', textAlign: 'center', letterSpacing: '0.18em',
            textShadow: '0 1px 0 rgba(255,255,255,0.18)'
          }}>PREV</span>
        </div>

        {/* The wheel */}
        <div style={{ position: 'relative', width: '170px', height: '170px' }}>
          {/* Outer ring */}
          <div style={{
            position: 'absolute', inset: 0, borderRadius: '50%',
            background: 'linear-gradient(180deg, #0a0c0f 0%, #2c2f34 100%)',
            boxShadow: 'inset 0 4px 10px rgba(0,0,0,0.9), inset 0 -2px 4px rgba(255,255,255,0.04)'
          }} />

          {/* Tick marks */}
          <div style={{ position: 'absolute', inset: '8px', borderRadius: '50%', pointerEvents: 'none' }}>
            {ticks.map((tick, i) => (
              <div key={i} style={{
                position: 'absolute', top: '50%', left: '50%',
                transform: `translate(-50%, -50%) rotate(${tick.angle}) translateY(-72px)`,
                width: tick.w, height: tick.h,
                background: tick.color,
                boxShadow: tick.glow,
                borderRadius: '1px'
              }} />
            ))}
          </div>

          {/* Spinning wheel face */}
          <div
            ref={jogRef}
            onPointerDown={handlePointerDown}
            onPointerMove={handlePointerMove}
            onPointerUp={handlePointerUp}
            onPointerCancel={handlePointerUp}
            className="bumper"
            style={{
              position: 'absolute', inset: '20px', borderRadius: '50%',
              background: `
                radial-gradient(circle at 50% 50%, #3a3d42 0%, #2c2f34 60%, #1a1d22 100%),
                repeating-conic-gradient(from 0deg, rgba(0,0,0,0.18) 0deg, rgba(0,0,0,0.18) 1deg, transparent 1deg, transparent 15deg)`,
              backgroundBlendMode: 'overlay',
              boxShadow: `
                inset 0 2px 4px rgba(255,255,255,0.12),
                inset 0 -3px 6px rgba(0,0,0,0.6),
                0 4px 12px rgba(0,0,0,0.6)`,
              transform: `rotate(${angle}deg)`,
              transition: isDragging ? 'none' : 'transform 200ms cubic-bezier(0.34, 1.56, 0.64, 1)',
              cursor: 'grab', touchAction: 'none'
            }}
          >
            {/* Knurling */}
            <div style={{
              position: 'absolute', inset: 0, borderRadius: '50%', pointerEvents: 'none',
              background: 'repeating-conic-gradient(from 0deg, rgba(255,255,255,0.06) 0deg, rgba(255,255,255,0.06) 2deg, transparent 2deg, transparent 11deg)'
            }} />

            {/* Grip notch */}
            <div style={{
              position: 'absolute', top: '5px', left: '50%', transform: 'translateX(-50%)',
              width: '14px', height: '30px', borderRadius: '0 0 7px 7px',
              background: 'linear-gradient(180deg, #fbbf24 0%, #d97706 100%)',
              boxShadow: `
                0 0 10px rgba(251,191,36,0.7),
                0 0 18px rgba(251,191,36,0.4),
                inset 0 1px 0 rgba(255,255,255,0.4),
                inset 0 -2px 2px rgba(0,0,0,0.4)`
            }} />

            {/* Center hub */}
            <div style={{
              position: 'absolute', top: '50%', left: '50%', transform: 'translate(-50%,-50%)',
              width: '56px', height: '56px', borderRadius: '50%',
              background: 'linear-gradient(180deg, #4a4d52 0%, #2c2f34 100%)',
              boxShadow: `
                inset 0 1px 0 rgba(255,255,255,0.2),
                inset 0 -1px 0 rgba(0,0,0,0.6),
                0 2px 4px rgba(0,0,0,0.5)`,
              display: 'flex', alignItems: 'center', justifyContent: 'center'
            }}>
              {/* Inner LED */}
              <div style={{
                width: '22px', height: '22px', borderRadius: '50%',
                background: 'radial-gradient(circle at 35% 35%, #d1fae5 0%, #4ade80 45%, #16a34a 100%)',
                boxShadow: '0 0 8px #4ade80, 0 0 16px rgba(74,222,128,0.5), inset 0 -2px 3px rgba(0,0,0,0.4), inset 0 1px 2px rgba(255,255,255,0.4)'
              }} />
            </div>
          </div>
        </div>

        {/* Right button - NEXT */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
          <button
            onClick={handleNext}
            className="bumper"
            style={{
              width: '54px', height: '54px', borderRadius: '8px', border: 'none', cursor: 'pointer',
              background: 'linear-gradient(180deg, #4a4d52 0%, #2c2f34 100%)',
              boxShadow: 'inset 0 1px 0 rgba(255,255,255,0.18), inset 0 -2px 0 rgba(0,0,0,0.55), 0 2px 4px rgba(0,0,0,0.5)',
              fontFamily: "'IBM Plex Mono', monospace", fontSize: '18px',
              color: '#0a0c0f', textShadow: '0 1px 0 rgba(255,255,255,0.18)'
            }}
          >▶</button>
          <span style={{
            fontFamily: "'Oswald', sans-serif", fontSize: '8px', fontWeight: 600,
            color: '#0a0c0f', textAlign: 'center', letterSpacing: '0.18em',
            textShadow: '0 1px 0 rgba(255,255,255,0.18)'
          }}>NEXT</span>
        </div>
      </div>

      {/* ENTER button */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '12px', padding: '8px 4px 0' }}>
        <button
          onClick={onCommit}
          className="bumper"
          style={{
            flex: 1, padding: '12px 16px', border: 'none', cursor: 'pointer', borderRadius: '6px',
            background: 'linear-gradient(180deg, #16a34a 0%, #052e16 100%)',
            boxShadow: `
              inset 0 1px 0 rgba(255,255,255,0.3),
              inset 0 -2px 0 rgba(0,0,0,0.5),
              0 2px 4px rgba(0,0,0,0.5),
              0 0 12px rgba(74,222,128,0.3)`,
            fontFamily: "'Oswald', sans-serif", fontWeight: 700, fontSize: '13px',
            color: '#d1fae5', letterSpacing: '0.28em',
            textShadow: '0 1px 0 rgba(0,0,0,0.5), 0 0 6px rgba(74,222,128,0.5)'
          }}
        >
          ◆ ENTER ◆
        </button>
        <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '3px' }}>
          <div className="ind-led ind-led-green" style={{ animationDuration: '1.6s' }} />
          <span style={{
            fontFamily: "'Oswald', sans-serif", fontSize: '7px', fontWeight: 600,
            color: '#0a0c0f', letterSpacing: '0.18em',
            textShadow: '0 1px 0 rgba(255,255,255,0.18)'
          }}>LIVE</span>
        </div>
      </div>
    </div>
  );
};
