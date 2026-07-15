---
id: MSG-BACKEND-003
from: conductor
to: backend
type: task
priority: high
status: READ
ref: MSG-CONDUCTOR-001
created: 2026-07-06
content_hash: 2f5de52848a22cb3065f0adb861318fc3ed97af056e113e845d39d85abf106b8
---

# Implement GrooveOperation domain model, validation and serialization

Please implement the GrooveOperation class in CabinetBilder.Core.Machining namespace, extending MachiningOperation. It must include parameters like Width, Depth, Length, Direction, and IsThrough. Then, implement JSON serialization support for it in AutoCadSkeletonStore (or CabinetBilder.Adapter.AutoCAD persistence) so that these machining features can be saved to and loaded from DWG XRecords. Write appropriate unit tests.

## Acceptance Criteria

- [ ] GrooveOperation record class implemented in CabinetBilder.Core.Machining namespace
- [ ] GrooveOperation has parameters: Width, Depth, Length, DirectionX/Y/Z, IsThrough
- [ ] GrooveOperation complies with Z-sĂ­k / A-sĂ­k rules (Z=0 is face, depth is positive downwards, but local coordinates reflect -Z direction)
- [ ] Validation rules (depth <= thickness, coordinates within panel boundaries) implemented
- [ ] JSON serialization of GrooveOperation integrated into AutoCadSkeletonStore persistence
- [ ] Unit tests for GrooveOperation validation and coordinate mapping implemented and passing
- [ ] Round-trip serialization tests for GrooveOperation passing
