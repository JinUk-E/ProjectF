# Military match validation

2026-10-17 — test-writer-codex / editor-tooling-codex.

Unity 6000.5.6f1, Windows, graphics PlayMode D3D11. Final results:

| Check | Result |
| --- | --- |
| All EditMode, including projectile package | 196 total: 193 passed, 0 failed, 3 existing GC-measurement inconclusive |
| All PlayMode, including previous ranges | 13 passed, 0 failed |
| Full HP hotseat match, controlled zero wind and solver-selected test shots | 7 shots to Player 1 victory; no health editing |
| Restart result-button callback | New seed, health reset, original heights restored |
| Flight lock and delayed application | Health/terrain/positions frozen until completion; next player selected |
| Cancel/restart during flight | No pending damage; seed replay and launch rig preserved |
| Match disable/enable | Cancellation, freeze released, aim input/HUD restored |
| Visual inspection | Actual 1280x720 and 1920x1080 HUD; damage and completed-match captures |

Evidence: `match-editmode.xml`, `match-playmode.xml`, `match-ready-720.png`, `match-ready-1080.png`, `match-after-hit.png`, `match-result.png`, `match-completed.png`.

During verification one test incorrectly assumed HeightField.Revision resets to zero. Reset intentionally increments it; assertions now check a new revision and restored heights. An EditMode SendMessage lifecycle test raised Unity's ShouldRunBehaviour assertion; real enabled-toggle lifecycle coverage is in the passing PlayMode test instead. No log assertion suppression is used.

Limitations: water remains below the diggable floor pending explicit planning approval; no dig-to-water acceptance claimed. Supported shot family is a single standard shell; scatter remains rejected. AI, special shells and camera tracking are deferred. Physical input, subjective recoil/dust quality and full-frame GPU/GC profiling are not covered by these automated results.
