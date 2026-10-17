# Epic 2 validation — 2026-10-17

Actor: main-codex. Unity 6000.5.6f1. Epic 2 implementation and automated validation completed.

| Suite | Total | Passed | Failed | Inconclusive |
| --- | ---: | ---: | ---: | ---: |
| Full EditMode | 187 | 184 | 0 | 3 |
| Fortress PlayMode, Direct3D 11 | 5 | 5 | 0 | 0 |

Evidence: `epic2-editmode.xml`, `epic2-playmode.xml`. Three existing package GC tests remain inconclusive because their allocation measurement API is unavailable. No allocation-performance claim is made.

Terrain coverage: cloned height ownership; bilinear interpolation; boundary/vertical/outside/entering segments; saddle/ridge first roots even with above-surface endpoints; frozen-world rejection; lower clamp; cancellation; no-hit timeout; two-shot post-crater predictor/impact <=0.01m; private TerrainData; view-height agreement; wind-free preview and visibility lifecycle; debug toggle; invalid vertical configuration rejected before cloning.

Crater calculation + partial height upload + SyncHeightmap on 257x257: 32 samples after one warmup, **max 0.325ms, mean 0.233ms** on this RTX 3070 PC in graphics-enabled editor PlayMode. This measures the synchronous CPU path, not GPU completion or end-to-end frame rate. The tested CPU update meets 16ms; 60 FPS and six-child shot performance remain unverified.

Worst bilinear-to-triangle deviation bound after the tested two shots: **0.0989434m**, within the 0.3m tolerance. This is not a guarantee for arbitrary edited settings or many repeated craters.

`terrain-after-shot.png` is a real 1280x720 URP render after two shots, inspected for visible terrain crater, aim dashes, tanks and non-overlapping HUD. Physical keyboard input remains manually untested.

Scene and controls: `../terrain-range.md`. The current standard shell is the entire shot family; scatter remains rejected. Multi-child tracking/order, combat, tank settling, turns and victory are later milestones.

Planning follow-up before Epic 3 water defeat: frozen water-below-hard-lower wording makes crater-only water loss unreachable. Existing lower=-8m/water=-10m preserved. No change to frozen planning documents. Team pull/rebase remains blocked by pre-existing unrelated unstaged changes, which are preserved.
