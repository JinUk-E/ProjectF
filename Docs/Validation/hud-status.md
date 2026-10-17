# Tactical HUD validation

2026-10-17 — test-writer-codex / main-codex.

- Unity 6000.5.6f1, graphics PlayMode with D3D11: **17 passed, 0 failed, 0 inconclusive**. Results: `hud-playmode.xml`.
- New HUD checks cover power endpoints, elevation/bearing updates, wind rotation, health fill, active-player switching, notification expiry, misses without false impact messages, single Canvas, results and restart-button callback.
- Existing range, terrain destruction, match completion, input locking, cancellation/restart and full-health water defeat tests all passed in the same run.
- Actual rendered captures: `hud-ready-720.png`, `hud-ready-1080.png`, `hud-result-720.png`, `hud-result-1080.png`, `hud-danger-720.png`, `hud-turn-720.png`. Inspected layout at 720p and 1080p, low-health red indication and transient turn placement.
- UI-only scene upgrade completed successfully through `MatchHudBuilder.UpgradeHudBatch`. C# / Markdown whitespace checks passed. Unity-generated scene YAML retains Unity's normal blank-value whitespace.

Capture command flag is `-fortressHudCapture`; older gameplay capture evidence remains unchanged. EditMode was not rerun for this presentation change; the previous water-validation results are historical. Physical keyboard/mouse interaction and whole-frame GPU/GC profiling were not measured.
