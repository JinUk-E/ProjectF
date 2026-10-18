# Battlefield atmosphere and aim readability

Actor: editor-tooling-codex. 2026-10-19. User requested a heavier, semi-realistic battlefield and clearer 3D aiming.

`Fortress > Upgrade Battlefield Atmosphere` updates MilitaryMatch and MilitaryAI through Unity APIs. It retains terrain geometry and combat rules, replaces ground/road/rock layers, uses weathered building surfaces, soft daylight and distance fog. Steep surfaces blend into rock; crater updates repaint the affected area. Original two-layer scenes remain supported.

The aim preview retains the same wind-free partial trajectory: bright warm core, dark outline, muted blue occluded portions and direction chevrons. It does not provide a solved impact point or compensate for wind. Hidden while firing or input is disabled.

Recoil travels 0.65m and returns over 0.55s. Impact dust lasts 1.4–2.8s. Original synthesized cannon/impact WAVs provide low-frequency transients; these are placeholder sound design, not recorded artillery. Playback follows launch/actual impact events and stops on cancellation or disable. Camera shake is omitted to preserve aiming comfort.

CC0 sources: [Aerial Grass Rock](https://polyhaven.com/a/aerial_grass_rock), [Brown Mud Leaves 01](https://polyhaven.com/a/brown_mud_leaves_01), [Concrete Wall 009](https://polyhaven.com/a/concrete_wall_009), plus existing Rocky Terrain and Kenney smoke. [Poly Haven license](https://polyhaven.com/license). Official 2K diffuse/normal downloads were checksum-verified. License retained in Assets/ThirdParty/PolyHavenBattlefield.

This is a material/lighting pass on existing low-poly geometry. Dense vegetation, detailed building models and cinematic destruction remain outside this pass.

Validation: final graphics PlayMode **49/49 passed**, including preview wind independence/visibility and terrain repaint tests. `Validation/atmosphere-playmode.json` and actual 1280x720 render `Validation/atmosphere-aim.png`. The first terrain test used an overly strict sum tolerance; it now accounts for Unity's 8-bit splat-channel quantization (2/255). Scene launch audio playback and cancellation stop were also checked in play mode. Subjective speaker balance still needs listening on the user's setup.
