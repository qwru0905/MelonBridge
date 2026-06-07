# MelonBridge

MelonLoader용으로 빌드된 모드 DLL을 **재컴파일 없이** [UnityModManager](https://www.nexusmods.com/site/mods/21)(UMM) 환경에서 실행시켜주는 브릿지입니다.

`MelonLoader.dll` 스텁이 MelonLoader의 공개 API 표면을 제공하고, UMM 모드인 `MelonBridge.dll`이
`MelonMods/` 폴더의 모드를 스캔/로드한 뒤 UMM 라이프사이클을 MelonLoader 콜백으로 라우팅합니다.

> 설계 배경과 상세 아키텍처는 [`docs/superpowers/specs/2026-06-06-melon-loader-to-umm-design.md`](docs/superpowers/specs/2026-06-06-melon-loader-to-umm-design.md) 참고.

## 설치 (사용자)

1. [Releases](../../releases)에서 최신 `MelonBridge-<version>.zip`을 받습니다 (또는 직접 빌드 — 아래 참고).
2. 압축을 풀어 게임의 `<Game>/UnityModManager/Mods/MelonBridge/` 폴더에 통째로 넣습니다.
3. MelonLoader 모드 DLL은 게임 루트의 `MelonMods/` 폴더에 넣습니다 (없으면 새로 생성).
4. UMM에서 게임을 실행하면 MelonBridge가 `MelonMods/`를 스캔해 모드를 로드합니다.

## 지원 범위 및 제약

- **대상 백엔드**: Mono / IL2CPP 양쪽 Unity 백엔드를 지향합니다.
- **커버 버전**: MelonLoader 0.3.x ~ 0.6.x의 핵심 API (라이프사이클, 로깅, 설정, 이벤트, 코루틴, 어트리뷰트).
- **IL2CPP interop 한계**: `RegisterTypeInIl2Cpp`, `Il2CppInterop`/`Unhollower` 생태계는 게임의 IL2CPP 생성
  어셈블리에 의존하므로 no-op 스텁만 제공됩니다. IL2CPP 전용 모드는 부분적으로만 동작할 수 있습니다.
- **Harmony/MonoMod 버전 충돌**: UMM이 자체 번들하는 Harmony와 MelonLoader 모드가 요구하는 버전이 다를 수
  있습니다. MelonBridge는 자체 Harmony/MonoMod 사본을 셰이딩(`*.Melon` 식별자로 리네임)해 번들하고,
  로드되는 Melon 모드의 어셈블리 메타데이터를 해당 사본을 가리키도록 재작성하여 충돌을 피합니다.

## 개발 / 빌드

### 사전 준비

`lib/` 폴더에 참조 DLL을 직접 배치해야 합니다 (`lib/README.md` 참고):

- `UnityEngine.dll`, `UnityEngine.CoreModule.dll` — Unity 설치 경로 또는 게임의 `<Game>_Data/Managed/`
- `UnityModManager.dll` — UMM 패키지 내

### 빌드 & 테스트

```powershell
dotnet build MelonLoaderToUMM.sln -c Release
dotnet test MelonLoaderToUMM.Tests/
```

### 배포 패키지 만들기

```powershell
.\tools\package-release.ps1
```

`dist/MelonBridge/`에 사용자가 받을 번들이 스테이징되고, `dist/MelonBridge-<version>.zip`이 생성됩니다
(버전은 `MelonBridge/Info.json`의 `Version` 필드를 따릅니다).

### 로컬 게임 폴더로 자동 복사 (선택)

`MelonBridge/local.user.props.example`을 같은 폴더에 `local.user.props`로 복사하고
`GameModDir`을 본인 게임의 모드 폴더로 지정하면, 빌드할 때마다 결과물이 자동으로 복사됩니다
(이 파일은 머신별 경로를 담으므로 git에 커밋되지 않습니다).

## 번들된 서드파티 라이브러리

라이선스 정보는 [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md) 참고.
