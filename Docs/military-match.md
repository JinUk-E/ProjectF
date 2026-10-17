# Military hotseat match

Actor: main-codex. 2026-10-17.

Open Unity menu **Fortress > Open Military Match**, then Play. The menu opens an existing scene without overwriting it; on a fresh checkout it derives MilitaryMatch from MilitaryRange with Unity APIs.

Two people share the keyboard. A/D changes direction, W/S elevation, Q/E power, Space fires. Each player starts with 100 HP. Standard shells are unlimited. The short dotted guide ignores wind; the HUD shows actual horizontal acceleration. Developer impact markers are disabled in this match's private tuning asset.

A shot locks input and freezes the height field and tanks. After the supported shot finishes, apply radial explosion damage, crater, vertical settling and fall damage; judge defeat, then alternate players and draw a new seeded wind. Tank capsules are analytical, exclude the shooter and participate in the same solver used for actual flight. Misses still complete at the flight limit.

The result panel shows winner/turn count (or draw). **NEW MATCH** restores original terrain and HP, uses the next seed, and resamples starting positions and wind. Spawn and wind use independent seeded streams. The source TerrainData and tuning assets are not mutated by play. Disabling the match cancels a pending shot; re-enabling returns to aiming.

The military presentation adds mesh-only gun recoil, muzzle flash and impact flash over the existing CC0 tank/terrain/dust assets. Recoil does not move the aiming rig or muzzle and therefore cannot alter predicted trajectories. See `art-assets.md` for original asset licenses.

## Scope and remaining work

- This is a local two-player standard-shell match. AI, special ammunition and general submunition-family lifecycle tracking are later work. Scatter profiles remain rejected rather than ending a turn early.
- Water remains at -10m under the -8m diggable floor pending the user's planning decision. The defensive water-defeat check exists, but digging cannot trigger it with these settings. This is **not full FR-5 acceptance**. No frozen planning document was changed.
- Camera remains the shared overview. Shot tracking, audio and more detailed destruction presentation remain future improvements.
- Automated PlayMode testing exercises scene wiring and the restart button callback; physical keyboard/mouse play and full-frame GPU performance are not asserted.

## Assets and automation

- Scene: `Assets/Fortress/Scenes/MilitaryMatch.unity`
- Rules: `Assets/Fortress/Data/MatchTuning.asset`
- Spawns: `SpawnLeft.asset`, `SpawnRight.asset`
- Visual values: `BattleVisualTuning.asset` (particle values baked by the scene builder; recoil read at runtime)
- CLI creation: `-batchmode -nographics -executeMethod Fortress.Editor.MatchSceneBuilder.CreateBatch`
- Tests: `-runTests -testPlatform EditMode` or `PlayMode`; use `-force-d3d11` for graphics. Add `-testFilter Fortress.Tests.MatchPlayModeTests -fortressCapture` for match screenshots at 720p/1080p.

Project git has no remote configured; local commits do not publish the game repository.
