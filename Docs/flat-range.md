# ProjectF — Epic 1 flat range

Actor: editor-tooling-codex. Verified: 2026-10-17, Unity 6000.5.6f1.

Open `Assets/Fortress/Scenes/FlatRange.unity`, or use **Fortress > Open Flat Range**, then press Play and focus the Game view.

| Key | Action |
| --- | --- |
| A / D | Rotate turret |
| W / S | Raise / lower barrel (0–85 degrees) |
| Q / E | Reduce / increase launch power |
| Space | Fire standard shell |

The green sphere marks the debug impact prediction including wind. Aim and fire are locked during flight. The target tank is visual only: damage, terrain destruction, player turns and victory are later milestones. This scene always fires from the blue tank.

Settings: `Assets/Fortress/Data/GameTuning.asset`. Projectile settings: `Profile_Standard.asset`, `FlightProfile_Arc.asset`, `Fuse_Impact.asset`, `Payload_Single.asset`. Do not edit ScriptableObject settings during a shot. Runtime state stays in FlatShotController.

Requires the external Projectile repository at `D:/MoraeGames/Packages/Projectile`, commit `d88c01f` or compatible later revision. The project manifest uses a local file dependency. TMP Essential Resources and their included font license are preserved under Assets/TextMesh Pro.

## Verification

Use Unity Test Runner: EditMode (all), then PlayMode (Fortress.Tests). CLI uses `-batchmode -nographics -projectPath <project> -runTests -testPlatform EditMode -testResults <result.xml>`; replace EditMode with PlayMode and add `-testFilter Fortress.Tests` for scene integration tests. Do not run two editors against the same project.

- EditMode: 173 total, 170 passed, 0 failed, 3 inconclusive package allocation tests.
- PlayMode: 2 passed, 0 failed, exercising the saved scene and native MonoBehaviour lifecycle.
- Predict/impact distance <= 0.01 m, including wind; no-hit timeout; input lock; pool reuse and cancellation covered.
- GC allocation profiling, rendered visual inspection and manual keyboard playtesting remain unverified by these headless runs.

Evidence: `Docs/Validation/epic1-editmode.xml`, `epic1-playmode.xml`.
Next milestone: Epic 2 heightfield, terrain shot surface, crater application and re-aim consistency tests.
