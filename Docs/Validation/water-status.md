# Approved water defeat validation

2026-10-17 — main-codex / test-writer-codex.

User approved: water -4m, terrain lower bound -8m. Planning authority updated requirements FR-4/5, high-level-scope, backlog, handoff and plan-frozen change history; historical critique retained with a correction note.

- **EditMode:** 210 total, 207 passed, 0 failed, 3 pre-existing package GC measurement inconclusive (`water-editmode.xml`).
- **Graphics PlayMode:** 14 passed, 0 failed (`water-playmode.xml`).
- Boundaries: ground -3.999m survives, -4m and -4.001m drown; HP can remain positive. Terrain and tank settling remain deferred until the shot finishes.
- Saved-scene integration: using actual solver/runner shots with explosion and fall damage disabled on a temporary settings clone, water alone defeated a tank at **100 HP**, ground **-5.578693m**, after **5 alternating shots**. Original shared settings are preserved.
- Result-button restart restores original dry terrain, full HP, non-drowned combatants and a new seed. Reinitialization commits Aim state before emitting input/UI events; this fixes an intermediate Ended/winner=-1 snapshot discovered during testing.
- Actual renders inspected: `water-defeat.png` and `water-crater-detail.png`. The water plane has no collider and leaves terrain/capsule projectile collision unchanged.

The old match-status.md and Epic2 water-blocker records describe earlier snapshots; their water approval blocker is now resolved. Existing range assets retain their stored values, while new GameTuning defaults and MilitaryMatch use -4m. Water is a simple opaque surface, without waves or refraction. Physical input and whole-frame GPU/GC profiling remain outside this validation.
