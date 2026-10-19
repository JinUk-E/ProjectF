# Rubble cover and battlefield surroundings

Actor: editor-tooling-codex. User approved the frozen-plan change on 2026-10-19: rubble should provide low cover and force a choice between hiding, going around and firing over it.

The changed contract replaces the previous passable-rubble rule. Destroyed buildings retain low, narrower cover. Movement and projectile queries use the rubble geometry; high trajectories can clear it. Existing shot-time world freezing, post-shot terrain settlement, water defeat and seeded restart remain in force. This does not add visibility/stealth detection or a second rubble-health resource.

The playable heightfield keeps its existing extent. A separate decorative landscape continues its edge into foothills and distant woodland. Boundary posts and cables mark the playable extent; distant landscape is not an expanded playable map.

Editor setup: stop play and run `Fortress > Upgrade Battlefield Surroundings`. The tool creates the surroundings settings, materials and distant tree prefab, then connects MilitaryMatch and MilitaryAI through Unity's scene API. Existing Poly Haven CC0 ground textures are reused; tree geometry is generated in the project.

Validation (2026-10-19): all 63 PlayMode tests passed in 130.08 seconds, including six new rubble/exterior tests and updated passable-rubble expectations. The new tests cover low/high shots, blast shielding, movement detours, rendered versus analytic slab bounds, frozen-world behavior, water, seeded restart, seam updates after edge craters, camera ground clearance and mesh sampling. Full results: Validation/rubble-exterior-playmode.json.

Inspected 1280x720 renders: Validation/rubble-cover.png and Validation/exterior-overview.png. Default rubble is approximately 1.1 m high within a narrower footprint; exposed upper tank surfaces and high arcs can still be hit. It is physical cover, not a hidden/detection state. The outer 180 m apron reuses ground textures and adds 160 distant trees; camera clearance also samples that visual surface. The exterior remains non-combat scenery.
