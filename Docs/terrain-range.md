# ProjectF — Epic 2 terrain range

Actor: main-codex. Date: 2026-10-17.

Open `Assets/Fortress/Scenes/TerrainRange.unity` (menu **Fortress > Open Terrain Range**), press Play and focus the Game view. Controls remain A/D direction, W/S elevation, Q/E power, Space fire. FlatRange remains available for the original flat-ground baseline.

Yellow dashes show only the first 1.5 seconds of a wind-free shot. The green sphere is the actual debug prediction, including wind and current terrain. `TerrainGameTuning.asset > Show Debug Prediction` disables it. Craters appear when the standard shell finishes. Fire twice without changing aim to see the next prediction follow the deformed ground. Target tanks are visual only; damage, falling, turns and victory belong to Epic 3.

## Runtime and data

- `HeightField` owns a cloned world-height array indexed [z,x]. Bounds and lower/water heights are explicit. It rejects crater mutation during an active shot.
- `TerrainShotSurface` clips the segment to map bounds, traverses grid cells and solves the bilinear surface intersection, including an intervening ridge when both endpoints are above terrain. No physics raycasts or Terrain collider queries.
- `FlatShotController` freezes terrain before launch, records a genuine surface impact, and applies the crater after Finished before unlocking aim. A cancelled or out-of-bounds shot does not dig. Standard SingleImpactPayload remains the only supported payload; multi-shell family ordering is later work.
- `HeightFieldTerrain` clones TerrainData for the play session, uploads only the crater rectangle with SetHeightsDelayLOD and SyncHeightmap, and restores the asset reference on destruction. Invalid lower heights outside Terrain's vertical range fail explicitly.
- `DashedAimPreview` reuses LineRenderers and updates on StateChanged using ArtilleryShot.PositionAt with zero wind. Its segment count, duration, width and dash fraction come from GameTuning.
- Terrain is 257x257 over 160m. Samples interpolate bilinearly; the test checks the maximum possible deviation from triangular rendering after two craters against the 0.3m requirement. This bound applies to the tested terrain/settings, not arbitrary coarse grids or unlimited craters.

## Verification and limits

Evidence: `Docs/Validation/epic2-editmode.xml`, `epic2-playmode.xml`. PlayMode runs use Direct3D 11 on this machine's RTX 3070. The timing test includes crater calculation, rectangle allocation/upload and heightmap synchronization after one warmup, over 32 samples. It is an editor CPU-path measurement, not a guarantee of end-to-end frame time or 60 FPS on other hardware.

Use Unity Test Runner EditMode (all) and PlayMode (Fortress.Tests). The optional CLI flag `-fortressCapture` renders the two-shot test to `Docs/Validation/terrain-after-shot.png` via URP. Use a graphics-enabled test run for capture.

GC package allocation checks remain inconclusive in this runner. Manual keyboard playtesting and GPU/frame-time profiling remain outstanding.

## Epic 3 planning issue

The frozen wording puts water below the hard terrain lower bound. Current defaults preserve that: lower=-8m, water=-10m. A tank standing on clamped terrain cannot reach that water level, so terrain digging alone cannot trigger the planned water defeat. Before implementing water defeat, clarify the intended water/lower relationship through the planning backtracking procedure. No frozen planning document or water-defeat behavior has been changed here.


2026-10-17 main-codex update: the user approved water=-4m above floor=-8m. New GameTuning defaults and MilitaryMatch now use this rule; stored legacy range settings remain unchanged. The earlier planning follow-up above is resolved. See military-match.md and Validation/water-status.md.
