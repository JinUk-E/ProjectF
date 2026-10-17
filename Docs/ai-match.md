# AI 대전

2026-10-17 — main-codex / gameplay-programmer-codex.

**Unity → Fortress → Play vs AI**로 시작한다. Game 화면을 클릭하면 1번 탱크를 조작할 수 있다. 2번은 AI다.

| 조작 | 키 |
|---|---|
| 방향 | A / D |
| 고각 | W / S |
| 발사력 | Q / E |
| 발사 | Space |

AI는 자기 차례에 약 1.5초 동안 조준한 뒤 발사한다. 화면에 AI AIMING이 표시되며 그동안 사람 입력은 차단된다. 승패 뒤 NEW MATCH를 누르면 새 랜덤 지형에서 다시 시작한다. 기존 Fortress → Play Military Match는 2인 핫시트다.

MilitaryAI 씬은 AiNormal 설정을 사용한다. AI Opponent의 Settings 참조를 AiEasy / AiNormal / AiHard 에셋으로 바꾸면 조준 오차와 턴별 보정 속도가 바뀐다. 난이도 선택 메뉴는 아직 없다.

AI는 일반탄을 사용하며 현재 바람과 파괴된 지형을 대상으로 격자 탐색과 국소 정밀화를 한다. 최종 노이즈 결과를 포함해 TrySolve 호출은 최대 200회다. 탐색 중 실제 포탑이나 지형을 움직이지 않는다. 사거리 밖에서는 탐색한 최대 사거리 방향으로 발사한다. 같은 판 시드와 AI 턴 번호는 동일한 노이즈를 만든다.

예약된 발사는 판 세대/턴과 연결된다. 재시작, AI 비활성화, 게임 종료 시 취소한다. AI가 지형을 직접 파거나 피해를 바로 적용하지 않으며 사람과 같은 발사·착탄·턴 처리 경로를 사용한다.

씬 생성 CLI: `Fortress.Editor.AiMatchBuilder.CreateBatch`. 검증 결과는 `Docs/Validation/ai-status.md`를 참고한다.
