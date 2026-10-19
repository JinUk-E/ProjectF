# Physical perimeter and exterior fog

Actor: editor-tooling-codex / shader-vfx-codex. Request: physically show and restrict where tanks can travel, and obscure the surrounding map with fog.

The playable heightfield rectangle remains unchanged. A continuous concrete perimeter uses the terrain-edge samples, with its inner face aligned to the existing movement limit. Its rendered mesh and physics collider share geometry. This boundary is not a destructible battlefield building; existing analytic tank movement remains authoritative.

`Fortress/ExteriorFog` applies distance from the playable rectangle to the exterior ground and distant trees. Haze starts 2 m outside and becomes opaque fog color at 35 m, independently of camera orbit. The combat terrain and tanks keep their existing materials. Normal camera-distance fog still blends the horizon.

Setup: stop play, run `Fortress > Upgrade Battlefield Surroundings`. Existing CC0 concrete and ground textures are reused.

Validation: full PlayMode run passed 66/67; the new fog test incorrectly included the match root's combat buildings. Restricted its query to the active exterior view without changing runtime code; BoundaryWallTests + RubbleExteriorTests then passed 10/10. See Validation/perimeter-fog-full-run.json and perimeter-fog-retest.json. Shader compiler reported no errors. Inspected perimeter-fog-overview.png and perimeter-fog-detail.png at 1280x720.

The wall is 2.25 m tall and 1 m thick, placed outside the existing movement rectangle. Tank movement cannot leave that rectangle; the perimeter collider also matches the rendered wall. Projectile simulation retains its analytic combat surface and does not gain a new destructible perimeter target.
