# MelonBridge

MelonLoader용 모드 DLL을 **재컴파일 없이** [UnityModManager](https://www.nexusmods.com/site/mods/21)(UMM) 환경에서 실행시켜주는 브릿지입니다.

> 상세 설계: [`docs/superpowers/specs/2026-06-06-melon-loader-to-umm-design.md`](docs/superpowers/specs/2026-06-06-melon-loader-to-umm-design.md)

## 설치 (사용자)

1. [Releases](../../releases)에서 `MelonBridge-<version>.zip` 다운로드.
2. `<Game>/UnityModManager/Mods/MelonBridge/`에 압축 해제.
3. `<Game>/MelonMods/` 폴더에 MelonLoader 모드 DLL 배치.
4. UMM으로 게임 실행 시 자동 로드.

## 지원 및 제약

- **대상**: Mono 및 IL2CPP Unity 백엔드.
- **API**: MelonLoader 0.3.x ~ 0.6.x 핵심 (Lifecycle, Logging, Prefs, Events, Coroutines).
- **IL2CPP**: `RegisterTypeInIl2Cpp` 등 interop은 no-op 스텁만 제공되어 제한적 동작.
- **충돌 방지**: 자체 Harmony/MonoMod를 `*.Melon`으로 셰이딩하여 UMM 번들과의 버전 충돌 방지.

## 개발 및 빌드

### 사전 준비 (`lib/` 폴더)
- `UnityEngine.dll`, `UnityEngine.CoreModule.dll`
- `UnityModManager.dll`
- 상세 안내: `lib/README.md`

### 빌드 & 테스트
```powershell
dotnet build MelonLoaderToUMM.sln -c Release
dotnet test MelonLoaderToUMM.Tests/
```

### 배포 및 자동 복사
- **패키징**: `.\tools\package-release.ps1` 실행 (결과물: `dist/`)
- **자동 복사**: `local.user.props` 작성 후 `GameModDir`에 경로 지정 시 빌드 후 자동 복사.

## 라이선스
[`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md) 참고.
