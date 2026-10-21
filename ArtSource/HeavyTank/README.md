# 중장갑 전차 외형

2026-10-21 · editor-tooling-codex

Quaternius Animated Tanks의 Tank2(CC0)를 기반으로 만든 주력 전차 파생형이다. 게임의 **중장갑 전차** 카드에서 선택한다. 현재 장갑은 외형이며, 체력·이동·탄종·피격 판정은 주력 전차와 같다.

- `HeavyTank.blend`: 138개 편집 가능한 모델 부품과 별도 STUDIO 컬렉션.
- `HeavyTank.fbx`: 모델 부품만 내보낸 교환용 파일.
- `HeavyTank-preview.png`: Blender 렌더.
- `main-source.json`: Unity에서 사용하던 주력 메시를 정규화된 좌표로 추출한 기준 자료.
- `heavy-runtime.json`: Unity용 차체·포탑·포신 메시, 총 19,168삼각형. 면별 노멀과 재질을 보존하며 추가 콜라이더는 없다.

## 수정과 반영

1. Blender에서 `HeavyTank.blend`를 열어 수정·저장한다. 모델의 사용자 속성 `mount`는 차체=0, 포탑=1, 포신=2이다. 새 부품에도 지정한다. STUDIO에는 지정하지 않는다.
2. 프로젝트 루트에서 다음 명령을 실행한다. 모델을 다시 만들지 않고 저장된 편집 내용을 내보낸다.

```powershell
& 'C:/Program Files/Blender Foundation/Blender 4.5/blender.exe' --background ArtSource/HeavyTank/HeavyTank.blend --python ArtSource/HeavyTank/export_heavy.py
```

3. Unity Play를 멈추고 `Fortress > Heavy Tank > 2 Import Blender Derivative`를 실행한다. 메시와 역할 데이터를 갱신하고 양 대전·한글 메뉴를 저장한다. 카드 PNG는 별도 렌더이므로 외형 수정 시 다시 렌더해 `Assets/Fortress/Art/TankRoles/HeavyPortrait.png`를 갱신한다.

처음 디자인을 재생성할 때만 `build_heavy.py`를 실행한다. 이 명령은 현재 `.blend` 편집 내용을 덮어쓰므로 수동 수정 후에는 사용하지 않는다. 원형 주력 메시가 바뀌었다면 먼저 Unity `1 Export Main For Blender`를 실행한다.

Unity는 FBX 자동 축 변환 대신 정규화 메시 교환 파일을 사용한다. Blender의 높이는 Z, Unity의 높이는 Y이며 내보내기가 변환한다. 포탑과 포신의 높이 1.8m 오프셋을 분리해 기존 조준축·포구 3.8m·반동 부모에 연결한다. 기존 구형 씬 빌더를 실행했다면 마지막에 `HeavyTankBuilder.Upgrade()` 또는 `CommandUiBuilder.Upgrade()`를 실행한다.

## 출처

원본: [Quaternius Animated Tanks](https://quaternius.com/packs/animatedtanks.html), CC0 1.0. 프로젝트 원본 고지: `Assets/ThirdParty/QuaterniusTanks/License.txt`.
추가 장갑·스커트·장비는 이 프로젝트에서 Blender로 제작했다. 외부 텍스처나 추가 유료 에셋을 사용하지 않았다.

실사 텍스처·LOD·궤도 애니메이션·실제 장갑 강화는 아직 적용하지 않았다.
