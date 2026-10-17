# Free art sources — military presentation pass

Actor: editor-tooling-codex. Retrieved 2026-10-17. User direction: a weighty military style (muted olive/desert/gunmetal), not a bright cartoon presentation.

| Asset | Official source | License | Project use |
| --- | --- | --- | --- |
| Animated Tanks Pack, Quaternius | https://quaternius.com/packs/animatedtanks.html | CC0 1.0; original License.txt included | Tank visual geometry, adapted to the existing rig |
| Rocky Terrain, Amal Kumar / Poly Haven | https://polyhaven.com/a/rocky_terrain | CC0 1.0; https://polyhaven.com/license | 2K diffuse and OpenGL normal terrain textures |
| Particle Pack, Kenney | https://kenney.nl/assets/particle-pack | CC0; original License.txt included | smoke_01 transparent particle sprite |

CC0 permits copying, modifying and redistributing the asset, including commercial use. Reference: https://creativecommons.org/publicdomain/zero/1.0/. These are asset copyright terms, not an endorsement by the creators. Source licenses and SHA-256 hashes are retained in Assets/ThirdParty. Only models/textures/license files are redistributed; website preview renders are not packaged.

## Selection and adaptation

Quaternius tanks have separate turret and gun meshes plus detailed tracks. The free models remain low-poly; material, palette, terrain, effects and lighting create the heavier military presentation. They are not photorealistic replacements. The editor creates derived meshes/materials under Fortress while retaining the original imported models.

The gameplay pivots and muzzle remain authoritative. Art fitting must preserve yaw/pitch/fire direction and avoid moving the ballistic launch point. Terrain texture replacement uses a separate TerrainData asset with identical height samples; no collision or terrain-height change belongs to this pass.

A separate `Assets/Fortress/Scenes/MilitaryRange.unity` allows comparison with TerrainRange. The builder opens an existing MilitaryRange rather than overwriting user changes. Controls remain A/D, W/S, Q/E and Space.

## Verification

The existing TerrainRange PlayMode cases also exercise MilitaryRange: two-shot prediction after cratering, terrain-source preservation, preview wind independence and lifecycle, debug marker toggle, and crater update timing. Optional -fortressCapture writes military-after-shot.png and military-tank-detail.png under Docs/Validation. Rendering is checked for mesh orientation, barrel placement, terrain material and HUD readability.

## Completed validation — 2026-10-17

Graphics-enabled Unity 6000.5.6f1 PlayMode: **8 passed, 0 failed** (`Docs/Validation/art-playmode.xml`): 2 flat-range checks plus 3 terrain checks each on the baseline and military scenes. The same post-crater prediction tolerance of 0.01m holds. Per-scene measured crater CPU update max was 0.327ms; this is not whole-frame/GPU performance certification.

Real 1280x720 URP renders were inspected for tank orientation, barrel-to-aim alignment, track silhouette, material visibility and HUD readability. Final framing uses a compact 195px reference-height HUD panel, neutral light, closer camera and 24m texture tiles. Screenshots: `Docs/Validation/military-after-shot.png`, `military-tank-detail.png`.

Open **Fortress > Open Military Range**, then Play. This pass uses no paid asset, account purchase or new runtime dependency. Original FlatRange and TerrainRange scenes remain available. Manual keyboard playtesting and full performance profiling remain future checks.

Additional capture check: 2/2 selected shot tests passed (`art-capture.xml`); smoke simulation reported 24 live particles. Static full-range capture does not establish final smoke readability, so dust opacity/scale still merits live play tuning.
