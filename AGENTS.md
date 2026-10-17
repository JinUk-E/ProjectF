# ProjectF — AI 게임 프로젝트 (Codex)

**엔진: Unity**. 이 프로젝트는 game-ai-team 체계로 진행된다 — 작업 전 반드시 `game-team-protocol` 스킬(`.agents/skills/game-team-protocol/SKILL.md`)을 읽고 절차를 따를 것.

## 상시 룰 (예외 없음)

1. 코드·문서·에셋을 만들거나 수정하는 작업 전에 **`game-team-protocol` 스킬을 읽고 절차를 따른다** (작업 시작/종료 절차, 지식 기록).
2. **동결 게이트**: `knowledge\ProjectF\docs\01-planning\plan-frozen.md`가 채워져 있지 않으면 신규 기능·구조 변경 구현을 시작하지 않는다.
3. 역할·프로젝트 격리: 다른 역할의 지식 파일, 다른 프로젝트의 지식 폴더는 읽지도 쓰지도 않는다.
4. 모든 기록·커밋의 행위자 표기에 `-codex` 접미사를 쓴다.
5. 프로젝트 루트의 `CLAUDE.md`는 Claude Code 전용 오케스트레이터 규칙이다. 읽어서 맥락은 파악하되, 그 안의 서브에이전트 위임 절차(Task 도구·add-pipeline 결정자)는 Codex에서 실행할 수 없으므로 따르지 않는다. 그 파이프라인의 산출물(`docs/0N-*/reports/`)은 **읽기 전용**이다.

## 팀 저장소 찾기

1. `~/.claude/settings.json` 의 `pluginConfigs["game-ai-team@game-ai-team"].options.team_repo` 값 — Claude Code 플러그인으로 설치한 경우 여기에 있다 (표준)
2. `~/.claude/game-ai-team.location` 파일에 적힌 경로 — 레거시 install 스크립트로 설치한 경우에만 존재한다
3. 이 프로젝트 폴더 또는 상위 폴더들에서 `game-ai-team` 폴더 탐색
4. 못 찾으면 사용자에게 물어보고 2번 파일에 저장

이하 `<팀저장소>`로 표기. 규칙 원본: `<팀저장소>\PROTOCOL.md`(절차 전체), `<팀저장소>\GLOBAL_RULES.md`(전역 금지·강제 — 최우선).

## 진행 순서 (요약 — 상세는 PROTOCOL)

1. **기획** (`knowledge\ProjectF\docs\00-inception\`) → 2. **계획** (`01-planning\`) → 3. **비판 패스** → 4. **동결 게이트**: `01-planning\plan-frozen.md`가 채워져 있지 않으면 **구현 작업을 시작하지 말고** 기획 단계가 필요함을 보고할 것 (버그 수정·소규모 유지보수는 예외, 동결 전임을 명시) → 5. **구현** → 6. 기획 변경 필요 시 **백트래킹** (멈추고 사용자 보고).

## 역할 채택

구현 작업은 역할을 채택하고 수행한다. 작업 성격에 맞는 역할 정의를 `<팀저장소>\agents\<역할>.md`에서 **전문을 읽고** 그 지침대로 일할 것:

| 역할 | 담당 |
| --- | --- |
| game-architect | 시스템 설계·모듈 구조 (구현 전 설계) |
| gameplay-programmer | 게임플레이 로직 (기본 구현 담당) |
| ui-developer | 화면·HUD·메뉴 |
| code-reviewer | 엔진 특화 코드 리뷰 |
| test-writer | 단위·통합 테스트 |
| performance-optimizer | 성능 분석·최적화 |
| editor-tooling | 에디터 확장·빌드 자동화 (프리팹·씬 도구 포함) |
| shader-vfx | 셰이더·파티클·포스트 프로세싱 |
| data-designer | 데이터 스키마·밸런싱 테이블 |

- 엔진별 기술 규칙: `<팀저장소>\engines\<엔진소문자>.md` 플레이북의 공통 섹션 + 자기 역할 섹션을 읽고 따를 것. 엔진이 "미정"이면 플레이북이 없음을 보고하고 엔진 종속 코드를 쓰지 않는다.
- 행위자 표기: 모든 기록·커밋에서 역할명에 `-codex` 접미사를 쓴다 (예: `gameplay-programmer-codex`). 오케스트레이터(역할 없이 조율)는 `main-codex`.

## 지식·기록 의무 (요약 — 절차는 game-team-protocol 스킬)

- 작업 시작: `knowledge\ProjectF\`의 `_shared.md`·자기 역할 지식 파일·`_timeline.md`를 읽고 맥락 복원.
- 작업 종료: 자기 역할 지식 파일에 기록, 공유할 내용은 `_shared.md`/`notes\`로 승격, `_timeline.md`에 한 줄 추가(오케스트레이터만), git 커밋.
- 다른 프로젝트의 지식 폴더는 읽지도 쓰지도 말 것.
- 이 프로젝트에 `scripts/sync-knowledge.*`가 있으면 기획 문서 원본은 프로젝트 `docs/`다. 지식 폴더의 `docs\`를 직접 고치지 말고, 기획 변경은 백트래킹 절차로 사용자에게 보고한다.
