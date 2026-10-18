# Vegetation, facilities and camera-relative wind

Actor: editor-tooling-codex, 2026-10-19.

`Fortress > Upgrade Vegetation and Facilities` creates original branching pine/shrub meshes and materials, connects deterministic vegetation placement to both military match scenes, and expands the wind HUD. No additional third-party assets or licenses are required.

The terrain seed controls placement, rotation and scale. Roads, spawn zones, buildings, water and steep slopes are excluded. Plants are decorative, with no movement or projectile collision. Terrain destruction removes undermined plants after the shot resolves; restarting the same seed restores placement. Buildings retain their authoritative bounds, HP and destruction rules while receiving military facility details.

The wind arrow now shows where wind pushes the projectile in the current camera's screen axes. It updates when the camera rotates without changing world wind. GUN-relative crosswind and head/tailwind values stay meaningful while orbiting. Values remain m/s² because gameplay models wind as acceleration, not measured air velocity. Near-zero wind hides the arrow. Buildings still provide physical cover; foliage is not a new concealment or cover mechanic.

Generated pine/shrub meshes are replaceable prefabs under Assets/Fortress/Art/FieldDetails. Settings control density and clearance; runtime generation does not modify the source prefab or map.

Validation: full PlayMode 53 cases completed, 52 passed and one legacy world-axis HUD expectation failed. The legacy case now uses a known top-down camera basis; that case plus the four new cases passed on a focused 5/5 rerun. Evidence: Validation/field-details-full-run.json and field-details-retest.json. Actual 720p overview and facility closeup inspected, default seed contains 65 pines and 90 shrubs. Mesh density reduced and 32-bit fallback included for larger regenerated plant meshes. Detailed facilities remain stylized modular geometry, not photogrammetry models.
