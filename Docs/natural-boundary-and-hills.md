# Natural boundary and distributed hills

Actor: editor-tooling-codex / shader-vfx-codex. The user requested replacing the artificial perimeter wall with mountains/rocks and spreading the terrain beyond the centre.

The natural perimeter replaces concrete, posts and cable with an irregular rocky shoulder and foothill strip outside the same playable rectangle. Its visual mesh, collider and camera height sampling agree. Exterior haze begins behind the visible rock shoulder. Existing movement limits, destructible combat terrain, building/rubble cover and water defeat remain in force.

Central hills are reduced to 75% height; additional low hills are distributed through the outer quadrants. Connected central/flank roads and spawn pads remain clear. The same seed reproduces the same map.

Run `Fortress > Upgrade Battlefield Surroundings` in edit mode to apply settings and the rock material. The existing Poly Haven CC0 rock texture uses world triplanar mapping, reduced saturation and exterior fog.

Validation (2026-10-19, editor-tooling-codex): all 69 PlayMode tests passed in 133.989 seconds. This covers natural boundary collider/mesh continuity and camera queries, eight-seed peripheral hills, roads/spawns, movement, AI, rubble cover, terrain damage and projectile regression. ExteriorFog has no shader compilation errors. Overview and close-up renders are saved in `Validation/natural-boundary-*.png`; full results are in `Validation/natural-boundary-playmode.json`.

Visual iteration widened the initial steep rock wall into a 4 m gentle toe, 14 m irregular crest and 32 m foothill band. Terrain-edge material transitions remain visible at close range; this is procedural scenery rather than authored terrain blending.
