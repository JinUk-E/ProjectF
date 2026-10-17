# ProjectF — AI 게임 프로젝트

<!-- GAME-AI-TEAM v1 | 1.2.0 -->

**엔진: Unity** (에이전트들은 `game-ai-team`의 `engines\` 플레이북을 따른다)

## 요청마다 먼저 — 위험 라우팅 (S/M/L)
구현 전에 `등급 — 이유` 한 줄을 판정한다 (PROTOCOL "작업 위험 라우팅"). **S**(오타·수치·단일 함수 버그)는 직접 처리하고 위임·비판자·문서 없이 `요청/변경/시험/한계`만 남긴다. **M**(새 동작·여러 파일·공유 계약)은 담당 에이전트 1명, 구체적 위험을 한 줄로 쓸 수 있을 때만 검토자 1명. **L**(세이브 포맷·결제/계정·외부 SDK·빌드/배포·큰 구조 변경)만 아래 전체 절차. 동결 게이트는 M·L에 적용된다.

## 진행 순서 (L 경로)
아키텍처·구현보다 **시나리오와 계획을 먼저** 확정한다. 기획 문서는 팀 저장소의 `knowledge\ProjectF\docs\`에 있다.

1. **기획 (docs\00-inception\)** — 메인 어시스턴트가 사용자와 대화하며 project-brief, scenario, pre-mortem, high-level-scope를 작성. "⏳ 아직 안 채운 틀" 줄은 채우면 지운다.
2. **계획 (docs\01-planning\)** — requirements와 backlog 작성.
3. **비판 패스 (필수)** — 동결 전, 임시 비판자 3인(기술·논리/자원·일정/플레이어 체감)을 읽기 전용(`Explore`)으로 투입해 `templates\critic-prompt.md` 지시문으로 기획을 공격시키고, 결과를 `critique.md`에 채택/반려 + 정량 4항목(채택률·반려 사유·직접 인용·★)으로 정리 (PROTOCOL 참조).
4. **동결 게이트** — 비판 패스 후 사용자 승인 시 `plan-frozen.md` 작성. 이 문서가 채워지기 전에는 M·L 구현 에이전트(game-architect 포함)를 투입하지 않는다.
5. **구현** — game-architect(설계) → 각 구현 에이전트. backlog를 갱신하며 진행. 아키텍처 산출물에도 비판 패스 권장. 위임 응답 마지막 줄의 `[STATUS: ...]`로 분기한다.
6. **백트래킹** — `[STATUS: CHECKPOINT]` 트리거(가정 파손·범위 밖·외부 의존·포맷 호환·설계 충돌·새 Blocker) 시 멈추고 사용자 보고 → 문서 갱신 → plan-frozen 이력 기록 후 재개.

## 팀 규칙
- 팀 저장소(game-ai-team)는 `game-ai-team` 플러그인의 `team_repo` 설정(`/plugin`에서 확인·변경)으로 찾는다. 미설정 시 폴백은 위치 기록 파일(`~/.claude/game-ai-team.location`). 규칙 전체는 저장소의 `PROTOCOL.md`, 전역 금지·강제는 `GLOBAL_RULES.md` 참조.
- **작업 연대기 의무**: 에이전트 위임이 끝날 때마다 `knowledge\ProjectF\_timeline.md`에 한 줄 추가할 것 (append-only). 세션 시작 시 이 파일로 이전 작업 흐름을 복원한다.
- 메인 어시스턴트가 프로젝트 전반의 결정을 내리면 `knowledge\ProjectF\_shared.md`에 기록하고 커밋할 것.
- 다른 프로젝트의 지식 폴더는 읽지도 쓰지도 말 것. 자기 프로젝트 안에서도 이번 작업과 관련된 노트만 선별해 읽는다.
- 이 프로젝트를 git으로 관리하면 `.gitignore`에 `game-ai-team/` 추가 (프로젝트 폴더 안에 clone한 경우).
