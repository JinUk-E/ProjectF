# Tactical match HUD

2026-10-17 — ui-developer-codex / main-codex.

The approved direction references the restrained military presentation of War Rock and Call of Duty. All panels, gauges and the shell silhouette are built from original uGUI elements; no game artwork is imported.

- Upper corners: player/unit identification, HP, thin health bars and active-player edge marker. Low health uses red; normal emphasis uses olive.
- Upper center: current turn/player and wind direction/magnitude. Wind is acceleration in this simulation, so its label uses m/s².
- Lower center: elevation, bearing, power number and segmented gauge, plus shot/input state.
- Lower right: standard shell, unlimited ammunition and Space fire hint. Lower left retains movement/aim controls.
- Brief upper-center notifications announce turns and confirmed impacts. Misses do not claim impact. Match results provide the restart button and submerged-loss explanation.

`MatchHud` refreshes values from match/shot events. Only notification fading runs each frame. `HudStyleSO` / `CombatHudStyle.asset` contain colors and notification timing. Gameplay rules remain owned by `MatchController`.

Open MilitaryMatch using the Fortress editor menu. To rebuild only its UI, run `Fortress.Editor.MatchHudBuilder.UpgradeHudBatch` in batch mode. This preserves the gameplay objects and camera. New match creation also uses the shared HUD builder.

References: [War Rock](https://store.steampowered.com/app/880850/War_Rock/) and [Call of Duty controls/HUD guide](https://www.callofduty.com/guides/blackops6/training/call-of-duty-guides-black-ops-6-multiplayer-training-controls).
