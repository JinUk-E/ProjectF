# Seeded terrain and keyboard validation

2026-10-17 — test-writer-codex / main-codex.

- Unity 6000.5.6f1 EditMode: **212 total, 209 passed, 0 failed, 3 existing package GC-measurement inconclusive** (`random-editmode.xml`).
- Graphics PlayMode: **20 passed, 0 failed** (`random-playmode.xml`).
- 32 seeds: finite terrain heights above the lower bound, non-flat terrain, dry flat spawn rectangles. Same seed reproduces pristine terrain after crater/reset without mutating the source grid.
- Saved MilitaryMatch: different seeds change over 100 sampled grid positions; same seed restores the full height grid, spawn and wind. Invalid generation during flight preserves the old field, active runner, seed and firing state.
- Existing hit, delayed damage, turn, victory/restart, full-health water defeat, cancellation, HUD and range regressions passed with random terrain enabled in MilitaryMatch.
- Synthetic keyboard uses actual Input System actions and Update: A/D, W/S, Q/E change aim/power in both directions; Space fires; additional aim/fire input is locked during flight. Initial batch run could not route keyboard input without a focused Game view. Test-scoped IgnoreFocus / AllDeviceInputAlwaysGoesToGameView corrected the harness; original settings are restored in finally.
- Actual renders inspected for seeds 12345 and 98765 (`random-seed-*.png`). Terrain remains a smooth rolling battlefield with protected flat spawn pads.

The first match uses initialSeed; NEW MATCH increments the seed. Explicit RestartMatch(seed) supports reproduction. Human physical keyboard/mouse interaction and whole-frame GPU/GC profiling were not performed. See `Docs/random-terrain-and-controls.md` for the manual Play menu and keys.
