# Building damage and collapse

Actor: editor-tooling-codex. User request 2026-10-19: buildings should visibly break rather than disappear.

Buildings retain their existing HP, foundation-collapse and analytic cover rules. Visible wall panels, roof plates and facility details move into an animated fragment view on destruction, then remain as low rubble. Nonlethal damage adds surface damage while keeping cover intact. The collapse is presentation only: fragments do not create rigidbody collisions or additional damage, and rubble remains passable.

`Fortress > Upgrade Building Destruction` connects the scorched surface material and a Kenney CC0 dust prefab to DepotCover. Collapse duration and spread live in CoverSettings. The original smoke texture and license were already in the project; no new external asset dependency was added.

This does not implement arbitrary holes through walls or per-panel ballistic cover. A destroyed building stops blocking shots/movement under the existing resolution rules while its short collapse animation finishes.

Validation (2026-10-19): Unity PlayMode 57/57 passed in 127.72 seconds, including four new damage/collapse/restart/foundation regression tests. Full result: Validation/building-collapse-playmode.json. Inspected 1280x720 renders: building-damaged.png, building-collapsing.png and building-rubble.png. Restarted the AI match after inspection. Geometry remains a stylized panel collapse, not a fracture simulation.
