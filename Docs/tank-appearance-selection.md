# Tank appearances and main-menu selection

Implemented 2026-10-20 by editor-tooling-codex / ui-developer-codex.

## Assets and license

Uses the four models already included in `Assets/ThirdParty/QuaterniusTanks` from [Quaternius Animated Tanks](https://quaternius.com/packs/animatedtanks.html). The official pack lists four models under CC0, usable for personal and commercial projects; the bundled `License.txt` contains CC0 1.0. No paid asset or additional download is required.

| Role | Source | Ammunition |
| --- | --- | --- |
| MAIN BATTLE | Tank2.fbx | HE, unlimited |
| SIEGE | Tank4.fbx | HE + SHATTER, 3 rounds |
| EXCAVATOR | Tank3.fbx | HE + DRILL, 3 rounds |
| SUPPORT | Tank.fbx | HE + CLUSTER, 2 rounds |

Derived meshes and olive/sand materials are in `Assets/Fortress/Art/TankRoles`. Main-menu portraits render those same meshes. This is stylized military art; these are not photorealistic licensed vehicle replicas.

## Player flow

Open `MainMenu`, select one of four tank cards, inspect the larger portrait and ammunition description, then choose DEPLOY / VS AI or LOCAL DUEL / 2 PLAYERS. The lower opponent row cycles the AI/player-2 role. Selection remains when returning from a match. Entering a fresh application/editor Play session resets to MAIN BATTLE.

`TankLoadout.appearance` refers to a `TankAppearance` containing the portrait, description, mesh pieces and side materials. `TankAppearanceView` attaches the selected hull, turret and cannon to existing rig mounts. Original renderers are hidden while the component is enabled and restored when disabled. The existing gun transform remains the recoil parent. Aim pivots, muzzle location, collision capsules, HP, movement and ammo balance are unchanged.

## Regeneration

Run `Fortress/Upgrade Tank Appearances` with Play stopped. `TankAppearanceBuilder.Upgrade()` bakes four source FBXs, renders/imports single-sprite portraits, assigns the four role assets, installs views in both battle scenes and invokes `TankSelectionBuilder.Upgrade()` last. The builders are repeatable and keep generated asset identities.

If using the older `MenuFlowBuilder.Upgrade()` or `AmmunitionBuilder.Upgrade()` later, run the appearance builder afterwards to restore the new menu and appearance bindings. No manual scene YAML editing is required.

## Validation

Validation covers all four cards, portrait/description selection, invalid indices, opponent independence, AI and local entry, menu return persistence, model mounts and side materials, repeated enable/restart without duplicates, recoil and muzzle invariance. See `Docs/Validation/tank-selection-*` for final test results and screenshots.

Final result: Fortress.PlayModeTests **93/93 passed**, 0 failures/skips, 178.081 seconds. Runtime screenshots inspected for main menu and close-up Siege in battle. No C# compile errors or gameplay exceptions were found; MCP connection/reload diagnostics were unrelated. Standalone player build not run in this pass.
