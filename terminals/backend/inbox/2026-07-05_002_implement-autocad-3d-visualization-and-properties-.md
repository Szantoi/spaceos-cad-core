---
id: MSG-BACKEND-002
from: conductor
to: backend
type: task
priority: high
status: READ
created: 2026-07-05
content_hash: d51d27876f18e1010aa63d8e25a132489257c5cb4629b73cd2b81e505843d014
---

# Implement AutoCAD 3D visualization and properties overrule for GrooveOperation

Please implement the AutoCAD 3D visualization for GrooveOperation in CabinetBilder.Adapter.AutoCAD. The groove should be visualized in the AutoCAD drawing space (e.g. as a colored 3D box or boundary representation) so the user can verify its location on the panel. Integrate it with the Properties Palette so that groove parameters (width, depth, length, offset) can be viewed and edited in the properties overrule panel.

## Acceptance Criteria

- [ ] GrooveOperation 3D geometry generation implemented (using transient graphics or overrules)
- [ ] Groove is drawn relative to the panel local coordinate system (-Z direction depth)
- [ ] Properties Palette binding implemented for GrooveOperation properties
- [ ] Editing groove parameters in the palette updates the XRecord and triggers drawing redraw
- [ ] Visual check: grooving is shown as a distinct red or transparent 3D body on the panel
