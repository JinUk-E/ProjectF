# 직접 플레이와 판별 랜덤 지형

2026-10-17 — main-codex / editor-tooling-codex / test-writer-codex.

Unity 메뉴 **Fortress → Play Military Match**를 선택하고 Game 화면을 클릭한다. 두 사람이 같은 키보드를 번갈아 사용하는 핫시트 방식이다.

| 키 | 동작 |
|---|---|
| A / D | 포탑 좌우 방향 |
| W / S | 고각 올림 / 내림 |
| Q / E | 발사력 줄임 / 늘림 |
| Space | 발사 |
| 결과 화면 NEW MATCH | 새 지형으로 다음 판 |

발사 중에는 조준·중복 발사가 잠긴다. 탱크를 직접 이동시키는 조작은 현재 게임 규칙에 없다.

새 판은 새 시드로 구릉과 골짜기를 생성한다. 양쪽 스폰 구역은 수면 위 평탄지다. 지형은 포격으로 파괴되며 수면 -4m, 지형 하한 -8m를 유지한다. 같은 시드로 `MatchController.RestartMatch(seed)`를 호출하면 동일한 초기 지형·스폰·바람을 재현한다. 첫 시작 시드는 MatchController의 initialSeed다.

MatchTuning의 randomTerrain이 활성화된 MilitaryMatch에 적용된다. 높이·진폭·노이즈 크기·옥타브·스폰 평탄지 여백은 GameTuning에서 조절한다. 원본 TerrainData 에셋은 보존하고 런타임 복사본만 바꾼다. 설정이나 스폰이 유효하지 않으면 기존 판을 취소하기 전에 거부한다.

에디터 적용 CLI: `Fortress.Editor.MatchSceneBuilder.UpgradeRandomTerrainBatch`. 기존 연습장에는 랜덤 지형을 강제하지 않는다.

키보드 자동 검증은 가상 키보드 상태를 Unity Input System에 주입하여 실제 액션 바인딩과 Update를 통과한다. 사람이 물리 키보드로 조작한 검증과는 구분한다.
