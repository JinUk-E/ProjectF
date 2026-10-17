# 직접 플레이와 판별 랜덤 지형

2026-10-17 — main-codex / editor-tooling-codex / test-writer-codex.

Unity 메뉴 **Fortress → Play Military Match**를 선택하고 Game 화면을 클릭한다. 두 사람이 같은 키보드를 번갈아 사용하는 핫시트 방식이다.

| 키 | 동작 |
|---|---|
| A / D | 포탑 좌우 방향 |
| W / S | 고각 올림 / 내림 |
| Q / E | 발사력 줄임 / 늘림 |
| Space | 발사 |
| 방향키 | 카메라 기준 탱크 이동 (자기 턴 12m, 초당 4m) |
| 결과 화면 NEW MATCH | 새 지형으로 다음 판 |

발사 중에는 이동·조준·중복 발사가 잠긴다. 이동은 수면, 30도 초과 경사, 다른 탱크, 맵 가장자리에서 제한된다. HUD에 남은 이동 거리를 표시한다. 발사 취소는 이동 거리를 회복시키지 않는다. 우클릭 드래그로 카메라 회전, 휠로 확대/축소, F로 현재 탱크를 찾는다.

새 판은 분리된 언덕과 중앙·우회 도로, 파괴 가능한 보급 건물 6채를 배치한다. 이전의 양쪽 연속 능선은 협곡처럼 느껴진다는 플레이 피드백으로 교체했다. 실제 지역의 지형 재현은 아니다. 지형과 도로는 포격으로 파괴되며 수면 -4m, 지형 하한 -8m를 유지한다. 같은 시드로 `MatchController.RestartMatch(seed)`를 호출하면 동일한 초기 지형·건물·스폰·바람을 재현한다. 첫 시작 시드는 MatchController의 initialSeed다.

MilitaryMatch와 MilitaryAI는 KoreanMountainStyle 프로필의 tacticalHills 설정과 수직 범위 64m의 MountainTerrain을 사용한다. 언덕 높이·반경·분포·도로 폭은 프로필에서 조절한다. 현재 씬 업그레이드는 `Fortress.Editor.TacticalBattlefieldBuilder.Upgrade()`를 사용한다. 엄폐·건물 규칙은 [전술 전장](tactical-battlefield.md)을 참고한다.

MatchTuning의 randomTerrain이 활성화된 MilitaryMatch에 적용된다. 높이·진폭·노이즈 크기·옥타브·스폰 평탄지 여백은 GameTuning에서 조절한다. 원본 TerrainData 에셋은 보존하고 런타임 복사본만 바꾼다. 설정이나 스폰이 유효하지 않으면 기존 판을 취소하기 전에 거부한다.

에디터 적용 CLI: `Fortress.Editor.MatchSceneBuilder.UpgradeRandomTerrainBatch`. 기존 연습장에는 랜덤 지형을 강제하지 않는다.

키보드 자동 검증은 가상 키보드 상태를 Unity Input System에 주입하여 실제 액션 바인딩과 Update를 통과한다. 사람이 물리 키보드로 조작한 검증과는 구분한다.
