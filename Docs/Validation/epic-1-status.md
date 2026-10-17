# Epic 1 validation status

Actor: main-codex. Updated 2026-10-17.

## Current result

Epic 1 flat-ground shooting and aiming implementation is complete with automated integration evidence. Scene: `Assets/Fortress/Scenes/FlatRange.unity`. Controls and scope: `Docs/flat-range.md`.

| Run | Total | Passed | Failed | Inconclusive |
| --- | ---: | ---: | ---: | ---: |
| Unity 6000.5.6f1 full EditMode | 173 | 170 | 0 | 3 |
| Unity 6000.5.6f1 Fortress PlayMode | 2 | 2 | 0 | 0 |
| Earlier package-only Unity 6000.0.32f1 | 165 | 162 | 0 | 3 |

Evidence: `epic1-editmode.xml`, `epic1-playmode.xml`, `package-fixed-6000.0.xml`. The three package allocation tests cannot measure GC.GetAllocatedBytesForCurrentThread in these runners. GC performance remains unverified. Headless tests do not verify rendered appearance or physical keyboard interaction.

Implemented: explicit shot surface launch, standard shell, tank rig, configurable aiming, fire lock, no-hit completion, pool/callback cleanup, impact VFX, event-driven status HUD and debug prediction marker. Four deterministic aim/wind cases satisfy impact error <= 0.01 m. The real scene also passes native frame-driven impact and disable/restart lifecycle tests.

## Resolved checkpoints

- Restricted-sandbox Unity licensing failed; normal-user execution approved by automatic review resolves entitlements successfully.
- External Projectile GetInstanceID incompatibility fixed with version-conditional identity APIs at commit `d88c01fbc64555ae0bdce0b8fd2b4041781da39e`. After explicit user push authorization, the commit was pushed to UnityProjectile origin/main; earlier local-only/push-blocked entries are superseded.
- TMP essentials imported through Unity's asynchronous import completion callback before scene creation. The editor generated scenes, prefabs and SO assets through Unity APIs.
- EditMode SendMessage(OnDisable) caused an engine assertion; the test now exercises CancelShot directly, while PlayMode independently verifies actual OnDisable behavior.
- MCP 10.2.0 dependency resolves; a live MCP connection has not been verified.

## Remaining

Epic 2: heightfield, terrain surface, crater application, player preview and post-crater re-aim consistency. Later: combat, turns, victory/restart. Target tank currently has no collision or damage. Team pull/rebase is blocked by pre-existing unstaged changes; those changes are preserved.
