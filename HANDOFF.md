# ProjectF — 3D 포트리스 구현 인수인계 (Codex용)

작성 2026-10-15. 구현 착수용 요약이다. **원본은 팀 저장소** `D:\MoraeGames\04.AI\05.AI_Team\game-ai-team\knowledge\ProjectF\`이며, 충돌하면 원본이 우선한다. Codex는 이 프로젝트 루트의 `AGENTS.md`와 `.agents/skills/game-team-protocol/SKILL.md` 절차를 따른다.

**Codex 실행:** 프로젝트 루트에서 `codex --add-dir "D:\MoraeGames\04.AI\05.AI_Team\game-ai-team"` (지식 기록·커밋에 필요). 행위자 표기는 `<역할>-codex`.

읽을 순서: `AGENTS.md` → 이 문서 → 팀 저장소 `knowledge/ProjectF/_shared.md` → `docs/01-planning/plan-frozen.md`(비어 있으면 구현 금지) → `docs/01-planning/requirements.md`, `backlog.md` → `docs/01-planning/critique.md`(왜 이렇게 정했는지).

---

## 1. 한 줄 목표

Unity 6000.5.6f1 + URP에서 **턴제 3D 아틸러리 프로토타입**을 만든다. 각도·파워·바람으로 포탄을 쏴 지형(높이맵)을 깎고 상대 탱크를 무너뜨린다. 투사체 계산은 전부 로컬 패키지 `com.unityprojectile.core`에 맡긴다.

성공 기준(핵심 4개):
1. 같은 각도·파워·바람 = 항상 같은 탄착점.
2. 디버그 예측 마커(`ProjectilePredictor` 탄착점)와 실제 `Impacted` 위치가 1cm 이내 일치. 크레이터 뒤에도.
3. 탄착 → 크레이터 → 발판 잃은 탱크 수직 낙하 → 발밑이 수면 이하면 패배.
4. 2인 핫시트 한 판이 승패까지 완주.

확장(일정 허락 시): 탄종 3종 + 탄 수 제한, 난이도 노이즈 AI.

---

## 2. 현재 상태

| 항목 | 상태 |
| --- | --- |
| ProjectF | URP 템플릿 빈 프로젝트. 게임 코드 0줄. `Assets/Settings`에 PC/Mobile 렌더러 있음(PC 사용). Input System 1.20, uGUI 2.5 포함 |
| 패키지 | `D:\MoraeGames\Packages\Projectile\Packages\com.unityprojectile.core` (별도 git 저장소). **0.3.0이 작업 트리에만 있고 미커밋.** CHANGELOG 0.3.0 항목 기준으로 Physical 자탄 모드와 `ShotSurface`까지 포함 |
| manifest | 패키지 아직 미등록 |
| 기획 동결 | 비판 패스 완료, **사용자 동결 승인 대기** |

**첫 행동 전 확인:** 패키지 저장소에서 0.3.0을 커밋한다(`git status`로 Runtime/Solving, ScatterPayload, ProjectileRunner, PayloadProfile 변경 확인 후). ProjectF 쪽에서 패키지 코드를 수정하거나 복사하지 않는다.

---

## 3. 패키지가 주는 것 (API 요약)

```csharp
// 입력: 각도·파워·바람 → 지형 교차 → 발사 파라미터
var shot = new ArtilleryShot(muzzle.position, ArtillerySolver.Aim(yaw, pitch), power,
                             gravity: 9.81f, wind: windVector, seed: turnSeed) { maxFlightTime = 15f };
bool hit = ArtillerySolver.TrySolve(shot, surface, out ProjectileLaunchParams launch);
// hit == false: 맵 밖. launch는 maxFlightTime까지의 자유 비행. 그래도 발사한다(턴 종료 보장).

// 발사: 반드시 surface를 넘기는 오버로드. 자탄(Physical)이 이 surface를 상속한다.
runner.ShotSurface = surface;   // 또는
runner.Launch(launch, surface);

// 예측(발사 전, 디버그 마커·AI 탐색용)
ProjectilePrediction p = ProjectilePredictor.Predict(profile, launch, 1 /*generation*/, surface);
// p.FirstVisibleImpactPosition, p.PayloadTime 등

// 이벤트 (호스트가 구독. 상태 폴링 금지)
runner.Launched, SegmentChanged, ImpactSoon, HoldReached, PayloadTriggered, Impacted, Finished,
runner.SubmunitionLaunched (carrier, child)   // 자탄 추적용
```

- **비행 프로필 고정:** `FlightProfile.segments = [PhysicsArcSegment(gravity 9.81)]` 하나 + 선형 speedCurve. 이래야 날아가는 경로가 해석기 포물선과 비트 동일.
- `IShotSurface.TryHit(from, to, out hit)`: 선분의 첫 교차. 같은 세계면 같은 답(결정론). 해석기가 1/120초 스텝으로 부른다.
- `PlaneSurface(height)`: 평지. 1단계 테스트용.
- 신관: `ImpactFuse`(충돌), `AltitudeFuse(height)`(표적 기준 상대 고도, 하강 중만, 미달 시 충돌 대체), `PenetrationFuse(depth, travelTime)`(탄착 후 최종 방향으로 depth 연장, 패키지가 궤적 처리).
- 페이로드: `SingleImpactPayload`, `ScatterPayload`(`mode = Physical`, `count`, `spreadAngle`, `ejectSpeed`, `maxFlightTime`; 자탄 프리팹·프로필 지정). Physical 모드는 모탄 바람과 `ShotSurface`로 자탄 탄착을 푼다.
- `ProjectilePool` (maxActive), `ProjectileEventRelay`(UnityEvent 연결), `ProjectileLauncher`(씬 저작용).
- 발사점은 지형 위에 떠 있어야 한다(표면에 닿으면 즉시 명중 판정). 자기 탱크는 surface에서 제외.
- EditMode 테스트 14+개가 패키지에 있다. 연결 후 전체 통과를 첫 체크포인트로 삼는다(검증은 6000.0.32f1에서만 됨).

---

## 4. 아키텍처 규칙 (어기지 말 것)

1. **발사 경로 하나:** `ArtilleryShot` → `TrySolve(surface)` → `Launch(params, surface)`. surface 없는 오버로드 금지(자탄이 평지로 떨어진다).
2. **지형의 진실은 게임이 소유한 높이 배열.** Unity Terrain은 그 배열을 그리는 뷰. `IShotSurface`와 탱크 낙하 모두 배열을 읽는다. `Physics.Raycast`·Terrain 콜라이더를 판정에 쓰지 않는다.
3. **한 사격(모탄+자탄) 동안 세계 고정.** `Impacted`는 수집만 한다. 사격 가족 전체 `Finished` 후 탄착 시각순(동시는 자탄 인덱스순)으로 크레이터·낙하·피해를 일괄 적용. 비행 중 높이 배열·탱크 위치 변경 금지. 탄착 순간에는 VFX만.
4. **카메라·VFX·UI·턴은 패키지 이벤트에만 연결.** 러너 내부 상태 폴링 금지.
5. **슬로모션은 전역 `Time.timeScale`.** 러너 `TimeScale`은 건드리지 않는다.
6. **수치는 전부 ScriptableObject.** 크레이터 반경, 피해, 바람 범위, 예측 구간, 탄 수, AI 노이즈, 스폰 영역. 코드 상수 금지.
7. **패키지 수정·커밋은 패키지 저장소에서만.** ProjectF에 복사본·패치 금지.
8. **결정론:** 판 시드 하나에서 스폰 위치·바람 순서·탄 시드가 파생. 같은 시드 + 같은 입력 = 같은 결과.

---

## 5. 모듈 구성

```
Assets/Fortress/                     (asmdef: Fortress, 참조: UnityProjectile.Runtime, InputSystem)
  Map/      HeightField(높이 배열·하한·수면), TerrainShotSurface : IShotSurface, CraterApplier, TerrainView(SetHeights)
  Tank/     Tank(HP, 포탑 yaw, 포신 pitch, 포구), TankSurface(캡슐 → IShotSurface 합성), SpawnArea SO
  Turn/     TurnController(상태 머신), ShotFamilyTracker(SubmunitionLaunched로 가족 추적), ImpactQueue, WindGenerator, MatchSeed
  Aim/      AimInput(Input System), AimPreview(바람 0 점선), DebugPrediction(기즈모), AmmoSelector
  Combat/   DamageResolver(반경 감쇠·낙하 피해·수면 판정), ShellCatalog SO(탄종·탄 수)
  Camera/   ShotCamera(이벤트 구동)
  UI/       Hud, ResultScreen
  AI/       AiAimer(TrySolve 격자 탐색 + 노이즈)
  Data/     SO 에셋: GameTuning, ShellProfiles(ProjectileProfile 3종 + FlightProfile + Fuse + Payload), SpawnAreas
Assets/Fortress.Tests/EditMode/      TerrainShotSurfaceTests, CraterApplyTests, ReaimConsistencyTests
```

`IShotSurface` 최종 구현은 `CompositeShotSurface { HeightField terrain; List<Tank> tanks; Tank exclude; }`.

---

## 6. 요구사항 (수치 포함)

### FR-1 패키지 연결·평지 사격 [P0]
manifest 로컬 경로 등록 → EditMode 테스트 통과 → `PlaneSurface`에서 발사 → `Impacted` 위치 오차 1cm 이내 → 폭발 파티클.

### FR-2 조준 입력 [P0]
yaw -180~180, pitch 0~85, 파워 min~max(SO). 포탑·포신 트랜스폼 즉시 반영. 발사 중 입력 잠금.

### FR-3 예측 점선·디버그 예측 [P0]
- 플레이어용 점선: **바람 0** 포물선 `origin + v0·t + ½g·t²`를 `previewSeconds`(기본 1.5s)까지만. 바람은 UI로만 공개(바람 보정이 플레이어 몫).
- 디버그: `ProjectilePredictor` 실제 예측 탄착점 기즈모. 발사 후 `Impacted`와 1cm 이내.
- 조준값 변경마다 갱신, 60fps.

### FR-4 파괴 가능 지형 [P0]
- `HeightField`: float[,] (257×257 권장), 셀 크기, 하한, **수면 높이**. Terrain 높이맵 해상도 = 배열 해상도, pixelError 1.
- `TerrainShotSurface`: 선분을 셀 단위로 걸으며 선형 보간 높이와 비교해 첫 교차. 맵 범위 밖은 false.
- 크레이터: 원형 R, 깊이 프로필(구면 캡 등). **일괄 적용 단계에서만** 적용. `TerrainData.SetHeightsDelayLOD` 부분 갱신. 1회 16ms 이하. 콜라이더 갱신 불필요.
- 테스트: 크레이터 후 같은 조준으로 다시 예측·발사 → 탄착점 새 지형 기준 일치.

### FR-5 탱크·피해 [P0]
- HP 100. 폭발 피해 `damage × (1 − d/R)`. 관통탄이 캡슐에 맞으면 폭발 중심 = 관통 종점, 크레이터 중심 = 그 지면 투영.
- 일괄 적용 후 발밑 높이 하락 시 같은 XZ에서 수직 낙하, `fallDamagePerMeter`(기본 5) × 낙차.
- 발밑 ≤ 수면 → 패배 플래그.
- surface에 탱크 캡슐 포함, 발사 탱크 제외.
- 스폰: 좌·우 `SpawnArea`(SO) 안에서 판 시드로 위치 선택. 판 중 수평 고정.

### FR-6 턴·바람 [P0]
- 상태 머신: `Aim → Firing → Resolving(가족 Finished 대기) → Applying(일괄 적용) → Judging → Aim`.
- 가족 추적: 모탄 `SubmunitionLaunched`로 자탄 등록, 전원 `Finished`면 Resolving 종료.
- `TrySolve` false(맵 밖)여도 발사. `maxFlightTime` 15s에 `Finished` → 턴 종료 보장.
- 바람: 수평 벡터, 세기 `[0, maxWind]` 기본 4 m/s², 판 시드에서 파생. `ArtilleryShot.wind`로 전달.

### FR-7 탄종 3종·탄 수 [P1]
| 탄종 | 구성 | 크레이터 | 피해 | 판당 |
| --- | --- | --- | --- | --- |
| 일반 | ImpactFuse + SingleImpactPayload | R 4m | 30 | 무제한 |
| 집속 | AltitudeFuse(15m) + ScatterPayload **Physical**, 자탄 6 | 자탄 R 1.5m | 자탄 8 | 2발 |
| 관통 | PenetrationFuse(2m, 0.4s) + payload delay 0.5s | R 6m | 40 | 2발 |
자탄도 ImpactQueue에 들어가 일괄 적용. 모두 SO.

### FR-8 카메라 [P1]
조준(탱크 뒤) → `Launched`에 탄 추적 → `ImpactSoon`에 `Prediction.FirstVisibleImpactPosition` 부감 → Applying(크레이터·낙하)까지 유지 → 다음 조준 카메라. 집속탄 `PayloadTriggered`에 0.5s 전역 `Time.timeScale` 0.3.

### FR-9 AI [P1]
- 현재 surface·바람으로 `TrySolve` 격자 탐색(각도×파워) + 국소 정밀화, 상한 200회. 상대 탱크에 가장 가까운 해. 해 없음(사거리 밖) → 최대 사거리 사격.
- 해에 난이도별 노이즈(각도 σ, 파워 σ), 턴마다 `decay` 배 축소. 보통 난이도 기대 오차 1턴 8m → 3턴 3m. SO.
- 사고 연출 1~2s.

### FR-10 승패·재시작 [P1]
HP 0 또는 수면 낙하 → 결과 화면(승자·턴 수·재시작). 재시작: 높이 배열·Terrain 원본 복원, 탄 수 초기화, 새 시드.

### FR-11 최소 HUD [P1]
HP 바 2개, 턴, 바람 화살표·수치, 각도·파워 숫자, 탄종·잔탄. uGUI 캔버스 1개, 1080p·720p 겹침 없음.

### FR-12 턴 타이머 [P2, 보류]
핫시트 대기 불만 시 30s 제한 + 자동 발사.

### NFR
60 FPS(자탄 6 동시 비행 포함) / 크레이터 16ms / 결정론 / 렌더·판정 시각 오차 0.3m 허용 / 패키지 EditMode 전체 통과 유지 + Fortress 테스트 3종.

---

## 7. 작업 순서와 일정 (작업일 12~16일, 상한 15일)

| Epic | 내용 | 일수 | 완료 신호 |
| --- | --- | --- | --- |
| 1 | 패키지 연결, 모듈 뼈대, 평지 사격, 조준 입력 | 1 | 테스트 통과 + 평지에서 예측·탄착 일치 |
| 2 | HeightField, TerrainShotSurface, 크레이터 일괄 적용, 점선·디버그 마커 | 3~4 | 재조준 일치 EditMode 테스트 통과 |
| 3 | 탱크 피해·낙하·수면, 턴 상태 머신, 바람, 최소 HUD, 승패·재시작 | 4~5 | 핫시트 한 판 완주 |
| 4 | 탄종 3종·탄 수, 카메라 연출, AI | 4~6 | 확장 성공 기준 |

- **조기 경보:** 작업일 7일까지 재조준 일치 테스트 없음 / 작업일 10일까지 승패 판정 없음 → 범위 절단.
- **절단 순서:** AI → 관통탄 → 카메라 연출. 탄종·AI는 성공 기준의 "확장"이라 잘라도 프로토타입은 성립.

---

## 8. 알려진 위험과 이미 정한 대응

| 위험 | 대응(이미 기획에 반영) |
| --- | --- |
| 해석기 지형과 렌더 지형 불일치 | 높이 배열이 진실, Terrain은 뷰, 디버그 마커로 상시 감시 |
| 자탄이 먼저 판 땅 위 허공에서 뒤 자탄 폭발 | 사격 중 세계 고정 + 일괄 적용 |
| 패키지가 6000.5에서 깨짐 | 연결 직후 EditMode 전체 실행이 첫 체크포인트. 깨지면 패키지 저장소에서 수정 |
| AI가 첫 발부터 명중(치트 느낌) | 해석기 해 + 난이도 노이즈·감쇠 |
| 점선 연장만으로 맞힘 | 점선은 바람 0, 바람은 UI 수치 |
| 관통탄 우세 | 판당 탄 수 제한 |
| 2판째부터 조준값 암기 | 스폰 랜덤 배치 |
| 연출에 시간 소진 | Epic 3(완주) 먼저, VFX는 프리셋·큐브 |

미해결(★): 선공 보정 — Epic 3 완주 후 플레이테스트에서 판단.

---

## 9. Epic 1 구체 지시 (첫 세션)

1. 패키지 저장소에서 0.3.0 커밋 확인. `Packages/manifest.json`에 추가:
   `"com.unityprojectile.core": "file:D:/MoraeGames/Packages/Projectile/Packages/com.unityprojectile.core"`
2. Unity 열고 Test Runner → EditMode 전체 실행. 실패하면 멈추고 보고(패키지 쪽 문제인지 분리).
3. `Assets/Fortress/` + `Fortress.asmdef`(참조 `UnityProjectile.Runtime`, `Unity.InputSystem`), `Assets/Fortress.Tests/EditMode/` + asmdef.
4. SO 생성: `FlightProfile_Arc`(PhysicsArcSegment 9.81, 선형 곡선), `Fuse_Impact`, `Payload_Single`, `Profile_Standard`, `GameTuning`(previewSeconds 1.5, maxWind 4, maxFlightTime 15, gravity 9.81, 파워 범위).
5. 씬: Plane + Tank 프리팹 2대(포탑/포신/포구 Transform) + 탄 프리팹(`ProjectileRunner` + `ProjectileEventRelay` + 메시) + `ProjectilePool`.
6. `AimInput`(Input System 액션: yaw, pitch, power, fire, 탄종 전환) → `Tank` 트랜스폼 반영.
7. `FireController`: `ArtilleryShot` → `TrySolve(new PlaneSurface(0))` → `pool.Get(prefab).Launch(launch, surface)`. `Impacted`에서 폭발 파티클 + 위치 로그. `Prediction` 탄착점과 비교해 1cm 이내 확인.
8. 커밋(ProjectF를 git으로 관리한다면 초기화 후). `.gitignore`에 Library, Logs, UserSettings, `game-ai-team/`.

Epic 2부터는 §6 FR-3·FR-4 순서대로. 매 Epic 끝에 §7 완료 신호를 확인하고 나서 다음으로 간다.

---

## 10. 결정 대기 (사용자)

1. 기획 동결 승인 여부. 승인되면 팀 저장소 `docs/01-planning/plan-frozen.md`가 채워진다. **그 전에는 AGENTS.md 상시 룰 2에 따라 구현을 시작하지 않는다.**
2. 패키지 0.3.0 커밋 시점과 주체.

## 11. 역할 배정 (Codex)

**이 프로젝트에 한해 설계 소유자는 Codex다** (사용자 결정 2026-10-15, `_shared.md`에 기록). 어댑터 일반 권장은 game-architect를 Codex에 주지 않지만, 프로젝트별 배정은 `_shared.md`가 우선한다. 설계 소유자는 한 곳이어야 하므로 Claude Code 세션은 이 프로젝트에서 game-architect를 투입하지 않는다.

| Epic | Codex 역할 | 비고 |
| --- | --- | --- |
| 0 (Epic 1 앞) | **game-architect-codex** | §5 모듈 구성을 초안으로 받아 asmdef 경계·모듈 간 통신(이벤트/DI)·SO 스키마를 확정. 산출물은 `_shared.md` "아키텍처 결정" 절과 `knowledge/ProjectF/game-architect.md`. §4 규칙 8개는 설계의 제약이지 설계 대상이 아니다 |
| 1 | editor-tooling-codex, gameplay-programmer-codex | 패키지 연결·테스트 실행·씬/프리팹/SO 세팅(Unity MCP 활용), 평지 사격 |
| 2 | gameplay-programmer-codex, test-writer-codex | HeightField·TerrainShotSurface·크레이터·테스트 |
| 3 | gameplay-programmer-codex, ui-developer는 보조만 | 턴·전투·승패. HUD는 최소치라 Codex가 배선 |
| 4 | data-designer-codex, gameplay-programmer-codex, code-reviewer-codex | 탄종 SO·AI. 리뷰는 2차 의견 |

§5 모듈 구성은 **초안**이다. game-architect-codex가 확정한 뒤에는 설계 산출물로 취급하고, 바꿔야 하면 멈추고 사용자에게 보고(백트래킹). §4 규칙과 FR 수락 기준은 설계로 바꿀 수 없다(기획 동결 범위).
