# ProjectF architecture

Actor: game-architect-codex. Date: 2026-10-15.
Status: initial design; implementation gate is package EditMode tests in Unity 6000.5.6f1.
Planning authority: game-ai-team/knowledge/ProjectF/docs; frozen 2026-10-15.
Package baseline: Projectile commit 24c6f81, version 0.3.0; Physical scatter and ShotSurface verified in committed sources.

## Assembly and ownership

Assets/Fortress contains the Fortress runtime assembly, referencing UnityProjectile.Runtime and Unity.InputSystem. Map, Tank, Combat, Turn, Aim, Camera, UI and AI are namespace/folder boundaries within this small prototype; separate assemblies per folder would add overhead without current benefit. Editor automation belongs in Fortress.Editor, editor-only. Fortress.EditModeTests references runtime and Unity Test Runner, editor-only with UNITY_INCLUDE_TESTS.

Map owns the authoritative height array, bounds, lower height and water level. Terrain is a view; no collider queries drive combat. Tank owns health and stationary XZ positions. Combat owns damage and crater application. Turn coordinates the shot lifecycle. Aim supplies commands; UI and Camera consume events and never own simulation state. A scene composition root wires serialized references explicitly; no global singleton or service locator.

## Shot transaction

Aim accepts yaw, pitch, power and shell selection only while the turn is Aim. Fire uses ArtilleryShot -> ArtillerySolver.TrySolve(surface) -> runner.Launch(parameters, surface), including false solve results. Surface combines height data and tank capsules, excluding the firing tank. World state stays unchanged from launch until every member of the shot family finishes.

Subscribe before launch. Register the root and subscribe to SubmunitionLaunched before any runner advances. Register children immediately; collect Impacted records with shot-relative simulation time and stable child index. Finished marks a member complete. When all members finish, sort records by impact time then index, enter Applying, apply craters and combat, settle tanks vertically, judge, then publish the next Aim state. Do not use callback arrival order or wall-clock time as deterministic ordering. Verify package child event ordering during integration before relying on this contract. Return/destroy/reset cleans up every subscription and restores global timeScale.

## Data and presentation

GameTuning SO holds gravity, flight time, power bounds, aiming rates, preview interval, wind bound, terrain parameters, health, fall damage and other tuning. Shell definitions reference package ProjectileProfile assets and hold crater/damage/ammunition values; runtime ammunition is copied into match state. SpawnArea SO holds each player's allowed region. AI difficulty SO is deferred until Epic 4. Profiles and tuning are immutable during a match.

Use instance C# events for this single-scene prototype; SO event channels were considered but would introduce asset lifecycle state without a cross-scene consumer. UI and Camera listen to turn and package events. Camera may follow a cached transform, but never polls runner state for lifecycle decisions. Only global Time.timeScale changes for slow motion.

Match seed derives independent spawn, wind and shot streams. The wind-free player preview is distinct from the actual ProjectilePredictor debug marker. Height interpolation and first-hit ordering must be deterministic. Original heights are retained for restart; restart also clears pending impacts, family subscriptions and ammunition.

## Delivery gates

1. Package EditMode suite passes in 6000.5.6f1 before gameplay implementation.
2. Flat shot: actual impact versus predictor <= 0.01 m, including wind; input locked during flight; out-of-bounds shot completes.
3. Heightfield and crater tests, then re-aim consistency <= 0.01 m.
4. Hotseat completes through damage/fall/water loss and restart; only then add shell variants, camera and AI.

Deferred planning question remains first-player balance after Epic 3. No new requirements or planning changes are introduced here.
