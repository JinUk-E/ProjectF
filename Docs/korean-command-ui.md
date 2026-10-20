# Korean command UI

Implemented by editor-tooling-codex and ui-developer-codex, 2026-10-20.

## Presentation

- Main menu: real 3D hangar preview with four selectable tank models, Korean role/weapon briefing, AI deployment and local two-player entry. Charcoal/gunmetal palette with orange action and selection accents.
- Battle: top health/turn/screen-relative wind; one bottom panel for ammunition, elevation, power and fire. +/- and fire buttons call the existing gameplay APIs and obey pause, projectile flight and AI ownership gates. Keyboard controls remain available.
- Korean wording covers dynamic health/turn/wind/notifications/results, ammunition, selection, loading and departure confirmation, plus static menu/manual/pause labels.
- KoreanUi maps existing English role identifiers into display text. Gameplay data IDs and balance are unchanged.
- HangarTankPreview toggles existing model objects only when the selected role changes. CombatPanelControls updates button availability only when the editable state changes.

## Font and source

NanumGothic-Regular.ttf is bundled from the [Google Fonts official repository](https://github.com/google/fonts/tree/main/ofl/nanumgothic). SIL Open Font License 1.1 is included at Assets/ThirdParty/NanumGothic/OFL.txt. No system font or remote font service is required at runtime. The generated TextMesh Pro asset includes prewarmed UI glyphs, embedded atlas/material and the source font for dynamic additions.

Tank models remain the existing Quaternius CC0 derivatives. The hangar meshes, platform, floor seams and lighting are original scene geometry.

## Editor regeneration

Run Fortress / Upgrade Korean Command UI, or CommandUiBuilder.Upgrade(), outside Play mode. It regenerates navigation, hangar layout, combat HUD and Korean font bindings in MainMenu/MilitaryMatch/MilitaryAI. Run this LAST after any older MenuFlowBuilder, MatchHudBuilder, AmmunitionBuilder or TankAppearanceBuilder upgrades; those older generators still produce their earlier layouts.

## Later armor / upgrades

Not implemented in this UI task. TankLoadout already separates role, ammunition and appearance, so it can reference future equipment/stat definitions. Before adding upgrades, define damage reduction and per-tank health, mobility/firepower trade-offs, whether progression persists, upgrade costs and save behavior. Keep role definitions separate from a player's owned/unlocked/upgraded instance, and apply resolved stats at match start. Avoid placeholder upgrade buttons that imply functionality already exists.

## Verification

Validation captures and results are in Docs/Validation/korean-ui-*. Actual main menu and battle rendering, Korean glyphs and text overflow, selected model identity, real button calls, pause/AI guards and existing gameplay regression are checked. Independent executable build validation remains outside this pass.

Results: initial full run 97/98 (one old English wind-prefix expectation); corrected full run 98/98, 203.303s. Final ammo-button binding cleanup and single-command assertion: KoreanCommandUiTests/TankSelectionTests/SessionMenuTests 14/14, 16.656s. Rendered main, battle and pause at Full HD and inspected ultrawide layout. No C# compilation errors.
