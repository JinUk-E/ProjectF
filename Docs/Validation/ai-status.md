# AI match validation

2026-10-17 — main-codex / test-writer-codex.

- Unity 6000.5.6f1 graphics PlayMode: **26 passed, 0 failed** (`ai-playmode.xml`).
- Full EditMode: **212 total, 209 passed, 0 failed, 3 existing package GC-measurement inconclusive** (`ai-editmode.xml`).
- AI scene tests cover thinking/one-shot dispatch/human turn return, real Input System keyboard suppression during AI turn, same-seed restart, disable/enable, restart inside the SetAim event, deterministic non-mutating planning, the 200-solve cap and out-of-range fallback.
- Full-health match with a precise scripted human opponent and normal AI ended after **5 shots**: human 69 HP, AI 7 HP but submerged. Results and NEW MATCH worked; no delayed AI shot after completion/restart. This is integration evidence, not a human playtest or a win-rate measurement.
- Normal calibration: scene seed 1, calm wind, 64 noise seeds. Mean distance from predicted impact to opponent center was **8.286m on AI shot 1**, **3.304m on shot 3**, no fallback. Acceptance bands 6–10m / 2–4m capture the approximate planning targets of 8m / 3m. Different distances, winds and terrain will change difficulty; broad balance testing remains future work.
- Actual 720p AI-thinking and submerged-victory renders inspected (`ai-thinking.png`, `ai-result.png`). Existing 2-player, random terrain, crater, water-loss, HUD and range tests passed.

MilitaryAI uses normal difficulty and a 1.5-second thinking timer. Easy/Hard are adjustable settings presets, not separately calibrated difficulty guarantees. AI currently uses standard shells. Physical keyboard/mouse playtesting and whole-frame GPU/GC profiling were not performed.
