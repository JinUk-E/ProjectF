# Ammunition, tank roles and terrain

2026-10-20 — main-codex / gameplay-programmer-codex / editor-tooling-codex

Select PLAYER 1 and OPPONENT / PLAYER 2 on the main menu, then deploy to AI or local duel. Both seats retain their selected role on a rematch. All roles currently share the existing tank model, health and movement allowance.

| Role | Basic ammunition | Special ammunition per tank, per match |
| --- | --- | --- |
| MAIN BATTLE | HE, unlimited | None |
| SIEGE | HE, unlimited | SHATTER ×3 |
| EXCAVATOR | HE, unlimited | DRILL ×3 |
| SUPPORT | HE, unlimited | CLUSTER ×2 |

Use the lower-right buttons or **1 / 2** to select basic / special. Special ammunition falls back to HE when depleted. Selection is blocked during flight, pause and the AI turn. Successful launch consumes a round; cancelling an already launched shot does not refund it. Restart refills both tanks.

| Ammunition | Maximum explosion damage | Building damage | Radius | Digging depth |
| --- | --- | --- | --- | --- |
| HE | 30 | 28 | 4 m | 0.55 m |
| SHATTER | 18 | 90 | 3 m | 0.30 m |
| DRILL | 12 | 12 | 3.5 m | 1.80 m |
| CLUSTER, per bomblet | 9 | 9 | 1.5 m | 0.18 m |

Damage falls off with distance. SHATTER is a demolition shell, not a penetrating projectile. CLUSTER releases six Physical-mode bomblets while descending at a target-relative altitude of 12 m, or on collision if the arc never reaches that trigger. Its casing causes no extra damage. Wind and the authoritative terrain/body/cover surface are inherited by the bomblets. Easy AI retains its existing aim errors; it occasionally selects useful special ammunition.

Every active family member must finish before terrain, damage and tank settling are applied in impact-time order (child index breaks ties). Cancellation discards buffered impacts and stops all remaining family members. Flat practice ranges retain their single-shell contract. The external projectile package is unchanged.

Battlefield roads now roll over a base height of 4 m with broad 3 m undulations. Spawn terraces blend into that road elevation, and hills remain separated by connected routes. Water remains -4 m and the terrain minimum remains -8 m. Shallow HE craters plus thicker starting ground prevent four ground hits alone from drowning a newly spawned tank; low ground and concentrated digging remain dangerous. This is an initial balance pass, not a guarantee against every four-shot defeat.

Crater rims are part of the authoritative height grid and affect movement and subsequent shots. Their low deposition is capped against original ground to prevent repeated explosions building walls. Soil fragments arc outward and stay on the ground; their pool is capped at 192 pieces and resets on a new match. The small fragments are cosmetic, while existing destroyed-building rubble still provides physical cover.

Editor setup: **Fortress > Upgrade Ammunition and Tank Roles**, then **Fortress > Upgrade Terrain Relief And Blast Debris**. These generate data assets/prefabs and connect both battle scenes using Unity editor APIs. MenuFlowBuilder rebuilding the main menu requires re-running the ammunition upgrade to restore its role picker.

Validation evidence is in `Docs/Validation/ammunition-*.json` and the `ammo-*.png` / `cluster-*.png` captures. Initial full PlayMode run: 81/82; the one failure assumed constant road base elevation and was corrected to measure hill relief above the new rolling base. Final full PlayMode: **87/87 passed** (207.920 s). Fortress EditMode: **49/49 passed** (1.202 s), including rim bounds/reset/freeze and existing terrain/combat tests. An EditMode job started during PlayMode teardown was orphaned by the domain reload; it was cleared after the editor reported no active test and rerun successfully. Live main/battle HUD and six-child split followed by 84 settled fragments were inspected. Standalone build and long-session balance are not part of this validation.
