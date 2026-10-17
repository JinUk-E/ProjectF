# 수동 카메라

2026-10-17 — main-codex / editor-tooling-codex / test-writer-codex.

- 오른쪽 마우스 드래그: 전장 회전
- 휠: 확대/축소
- F: 현재 턴 탱크를 중심으로 보기 (보고 있는 방향은 유지)

MilitaryMatch와 MilitaryAI에 적용. 턴이 바뀌어도 수동 시점은 유지되며 포탑 조준과는 별개다. HUD 위에서 시작한 회전과 휠은 무시하고 창 포커스를 잃으면 드래그를 해제한다. 높이 배열을 샘플링해 카메라가 지형 안으로 들어가지 않도록 높인다. 높은 능선에서는 명목상 줌 거리보다 지형 회피가 우선한다.

설정: OrbitCameraSettings.asset. 씬 반영 도구: Fortress.Editor.OrbitCameraBuilder.Upgrade(). 자동 탄 추적·슬로모션은 이번 작업에 포함하지 않는다.

검증: 실행 중인 Unity MCP의 전체 Fortress PlayMode **29/29 통과**. 새 3건은 회전/줌 범위·조준 불변, 턴 시점 유지·탱크 초점·지형 높이, 실제 Input System 마우스 입력/포커스 해제를 확인한다. 결과: Docs/Validation/orbit-playmode.json. 이번에는 EditMode를 재실행하지 않았다.
