# Task ID: 0021
# Title: Machining Features (Grooving/Backpanel slot) (DB-13)
# Category: feature
# Milestone: Phase 12
# Status: new
# Source: MSG-CONDUCTOR-001

## Szándék (Intent)

Implement GrooveOperation domain model, validation, serialization, and AutoCAD 3D visualization to support panel grooving and backpanel slotting features.

## Elfogadási kritérium (Acceptance Criteria)

### GrooveOperation Domain & Serialization (MSG-BACKEND-003)
- [ ] GrooveOperation record class implemented in CabinetBilder.Core.Machining namespace
- [ ] GrooveOperation has parameters: Width, Depth, Length, DirectionX/Y/Z, IsThrough
- [ ] GrooveOperation complies with Z-sík / A-sík rules (Z=0 is face, depth is positive downwards, but local coordinates reflect -Z direction)
- [ ] Validation rules (depth <= thickness, coordinates within panel boundaries) implemented
- [ ] JSON serialization of GrooveOperation integrated into AutoCadSkeletonStore persistence
- [ ] Unit tests for GrooveOperation validation and coordinate mapping implemented and passing
- [ ] Round-trip serialization tests for GrooveOperation passing

### GrooveOperation AutoCAD 3D Visualization & Properties (MSG-BACKEND-004)
- [ ] GrooveOperation 3D geometry generation implemented (using transient graphics or overrules)
- [ ] Groove is drawn relative to the panel local coordinate system (-Z direction depth)
- [ ] Properties Palette binding implemented for GrooveOperation properties
- [ ] Editing groove parameters in the palette updates the XRecord and triggers drawing redraw
- [ ] Visual check: grooving is shown as a or transparent 3D body on the panel
