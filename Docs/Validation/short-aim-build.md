# 짧은 조준선 Windows 프리뷰

2026-10-27 · editor-tooling-codex

- 사용자 승인: 착탄 원 제거, 짧은 점선, 테스트 및 빌드.
- ReadableAim의 원/바람 예측을 끄고 MatchTuning을 0.35초/8구간으로 변경. 두 대전 안내문과 재생성 빌더 반영. 카메라·전장·피해 규칙 유지.
- PlayMode 24/24 + AI 8/8 통과. AI 첫 작업은 도메인 재로드 후 결과 상태가 갱신되지 않아 고아 작업 정리 후 재실행. JSON 두 파일과 short-aim.png 참조.
- Windows64/Mono/nondevelopment, MainMenu·MilitaryAI·MilitaryMatch, LZ4/strict. build-34404257c8 성공 17.95초, 115.44MB, 오류0/경고0.
- Builds/Downloads/ProjectF-Windows-0.1.1-preview.zip: 57,171,173 bytes. 라이선스7·HTML·manifest·SHA256 동봉/생성. source b9522ea와 기존 로컬 에셋 사용.
- 독립 플레이어 D3D12·물리·입력 초기화, 프로세스 응답 확인. managed 예외 없음. 독립 플레이어의 전체 대전 수동 조작은 검증하지 않음.
- 설계 및 독립 코드 리뷰 차단0. 역할의 Opus 모델은 환경 미지원으로 상속 모델 사용. 외부 사이트 게시는 수행하지 않음.
- 빌드 자동 변경 설정4개는 백업 후 복원. 기존 작업 에셋과 동적 글꼴 데이터는 커밋에서 제외하고 보존.
