# 포탄 추적 카메라

2026-10-24 · editor-tooling-codex / gameplay-programmer-codex

AI 대전과 로컬 대전에서 발사한 포탄을 따라가고, 착탄과 지형 파괴를 보여준 뒤 발사 전 수동 시점으로 돌아온다. 조준 방향과 탄도 계산은 기존 게임 로직을 사용한다.

## 연결

Unity Play를 멈추고 `Fortress > Upgrade Projectile Follow Camera`를 실행한다. `ProjectileCameraBuilder.Upgrade()`는 두 대전 씬의 Main Camera에 `ProjectileFollowCamera`를 연결하며, 기존 HUD나 탱크 외형을 재생성하지 않는다. 설정은 `Assets/Fortress/Data/ProjectileCameraSettings.asset`에 저장한다.

추적 중에는 수동 회전·줌 입력과 카메라 위치 갱신이 겹치지 않도록 제어권을 전환한다. 기존 수동 시점의 방향·거리·초점은 보존한다. 일시정지는 추적·착탄 관찰·복귀 연출을 함께 멈춘다.

AI의 준비 시간은 카메라의 착탄 관찰·복귀가 끝난 뒤 흐른다. 기본 1.5초를 복귀 중에 소비하지 않아 연속 카메라 이동 사이에 정지 구간이 생긴다. 카메라가 없거나 비활성화되면 기존 AI 준비 시간만 적용한다.

`Fortress > Upgrade Shell Exhaust`는 일반탄·클러스터 모탄·자탄의 기존 모델 아래에 밝은 중심부·주황 화염·짧은 연기 3겹을 연결한다. 모델 방향을 따르며 착탄·취소 시 모델과 함께 꺼진다. 자체 생성한 작은 알파 텍스처와 기존 CC0 연기 텍스처를 사용한다. 외형 재생성 시 이 메뉴를 마지막에 실행한다.

## 범위

- 일반탄과 역할별 특수탄 추적.
- 클러스터탄은 분열한 자탄 무리를 함께 보여주는 구도.
- 착탄 예고와 사격 가족 완료 이벤트에 따른 착탄 관찰·복귀.
- 취소·새 판·컴포넌트 비활성화·메뉴 이탈 시 제어권 정리.
- 기존 지형 높이와 외곽 높이를 이용한 카메라 지면 관통 방지.

이번 작업은 포탄 추적 연출이다. 별도 후방 조준 모드·전장 재구성·건물 부분 파괴·전역 슬로모션은 포함하지 않는다.

## 검증

2026-10-24 Unity PlayMode 관련 테스트 17/17 통과: ProjectileFollowCameraTests 7개, OrbitCameraTests 4개, SessionMenuTests 6개. 추적·복귀, 기존 줌/회전, 클러스터 전체 구도, 일시정지, 사격 취소·재시작·메뉴 이탈을 확인했다. 결과는 `Validation/projectile-camera-playmode.json`에 기록했다.

실제 플레이 화면의 비행·착탄·클러스터 분열을 `Validation/projectile-camera-{flight,impact,cluster}.png`로 확인했다. 캡처 때만 시간을 늦추거나 정지했으며 런타임 카메라는 시간 배율을 변경하지 않는다. 별도 실행 파일 빌드는 이번 검증에 포함하지 않았다.

후속 체감 수정: AI8·메뉴6·추적7·포탄뷰3 = 관련 PlayMode24/24 통과(55.412초). `Validation/shot-polish-playmode.json`에 결과 저장. 후방 화염의 실제 비행은 `Validation/shell-exhaust-final.png`로 확인하고 착탄 시 효과가 모델과 함께 꺼지는 것도 점검했다.
