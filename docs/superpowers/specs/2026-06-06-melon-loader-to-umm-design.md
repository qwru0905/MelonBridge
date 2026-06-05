# MelonLoader to UnityModManager Bridge — Design Spec

**Date:** 2026-06-06  
**Status:** Approved

---

## 목표

MelonLoader용으로 작성된 모드 DLL을 재컴파일 없이 UnityModManager(UMM) 환경에서 실행할 수 있는 범용 브릿지를 구현한다.

- 대상: Mono / IL2CPP 양쪽 Unity 백엔드
- 커버 버전: MelonLoader 0.3.x, 0.4.x, 0.5.x, 0.6.x
- API 커버리지: 핵심 라이프사이클, 로깅, 설정, 이벤트, 코루틴, 어트리뷰트, IL2CPP no-op 스텁
- 번들 어셈블리: HarmonyX, Il2CppInterop, UnhollowerRuntimeLib, Semver, Mono.Cecil

---

## 아키텍처

### 프로젝트 구성

```
MelonLoaderToUMM/
├── MelonLoader.Stub/        → 출력: MelonLoader.dll
│   ├── Core/                  MelonBase, MelonMod, MelonPlugin
│   ├── Logging/               MelonLogger (정적 + Instance)
│   ├── Preferences/           MelonPreferences, Category, Entry<T>
│   ├── Events/                MelonEvents, MelonEvent<T>
│   ├── Coroutines/            MelonCoroutines
│   ├── Attributes/            MelonInfo, MelonGame, MelonPriority 등
│   └── Il2Cpp/                no-op 스텁
│
└── MelonBridge/             → 출력: MelonBridge.dll  (UMM 모드)
    ├── Main.cs                UMM 진입점 (static Main 클래스)
    ├── ModLoader.cs           MelonMods/ 폴더 스캔 & 로드
    └── Bridges/
        ├── LoggerBridge.cs    MelonLogger → UMM Logger
        ├── PreferencesBridge.cs  MelonPreferences → TOML .cfg
        └── SceneBridge.cs     SceneManager 이벤트 → ML 씬 콜백
```

### 번들 서드파티 어셈블리

MelonLoader 모드들이 참조하지만 UMM이 제공하지 않는 어셈블리들. 빌드 출력에 함께 포함된다.

| 어셈블리 | 버전 범위 | 라이선스 | 용도 |
|---|---|---|---|
| `0Harmony.dll` (HarmonyX) | 전 버전 | MIT | 런타임 패칭. UMM 번들 Harmony와 충돌 방지를 위해 별도 로드 |
| `Il2CppInterop.Runtime.dll` | 0.5.x+ | LGPL-2.1 | IL2CPP 런타임 interop |
| `Il2CppInterop.Common.dll` | 0.5.x+ | LGPL-2.1 | IL2CPP interop 공통 유틸 |
| `UnhollowerRuntimeLib.dll` | 0.3.x / 0.4.x | LGPL-2.1 | 구버전 IL2CPP interop |
| `UnhollowerBaseLib.dll` | 0.3.x / 0.4.x | LGPL-2.1 | 구버전 IL2CPP interop 기반 |
| `Semver.dll` | 전 버전 | MIT | 버전 비교 |
| `Mono.Cecil.dll` | 전 버전 | MIT | 어셈블리 메타데이터 조회 |

**HarmonyX 충돌 처리:** UMM도 자체 Harmony를 번들하므로 어셈블리 이름 충돌 가능. `Assembly.LoadFrom()`으로 경로 지정 로드하거나, AppDomain 격리를 통해 분리한다.

### 동작 흐름

```
UMM이 MelonBridge.dll 로드
  └─ Main.Load() 호출
       └─ MelonMods/ 폴더 스캔
            └─ 각 DLL → Assembly.LoadFrom()
                 └─ MelonMod / MelonPlugin 서브클래스 탐색
                      └─ MelonPriority로 정렬 후 순서대로 인스턴스화
                           └─ 라이프사이클 초기화 호출
                                └─ UMM 콜백에 등록
```

---

## 게임 디렉터리 구조

```
Game/
├── UnityModManager/
│   └── MelonBridge/
│       ├── MelonBridge.dll
│       ├── MelonLoader.dll          ← 스텁
│       ├── 0Harmony.dll             ← HarmonyX
│       ├── Il2CppInterop.Runtime.dll
│       ├── Il2CppInterop.Common.dll
│       ├── UnhollowerRuntimeLib.dll
│       ├── UnhollowerBaseLib.dll
│       ├── Semver.dll
│       └── Mono.Cecil.dll
├── MelonMods/              ← ML 모드 DLL 배치 위치
│   ├── SomeMod.dll
│   └── AnotherMod.dll
└── UserData/
    └── MelonPreferences.cfg   ← ML 원본 TOML 형식
```

---

## 라이프사이클 매핑

| UMM 이벤트 | ML 메서드 (순서대로 호출) |
|---|---|
| `OnLoad()` | `OnEarlyInitializeMelon()` → `OnInitializeMelon()` → `OnApplicationStart()` → `OnApplicationLateStart()` |
| `OnUpdate()` | `OnUpdate()` |
| `OnFixedUpdate()` | `OnFixedUpdate()` |
| `OnLateUpdate()` | `OnLateUpdate()` |
| `OnGUI()` | `OnGUI()` |
| `OnToggle(false)` | `OnDeinitializeMelon()` → `OnApplicationQuit()` |
| SceneManager.sceneLoaded | `OnSceneWasLoaded()` → `OnSceneWasInitialized()` |
| SceneManager.sceneUnloaded | `OnSceneWasUnloaded()` |
| 설정 저장/로드 | `OnPreferencesSaved()` / `OnPreferencesLoaded()` |

**버전 호환 전략:** 스텁에서 모든 버전의 메서드를 virtual로 정의하고, 브릿지가 양쪽 모두 호출한다. 구버전 모드가 `OnApplicationStart`만 구현해도 동작하고, 신버전 모드가 `OnInitializeMelon`만 구현해도 동작한다.

**오류 격리:** 각 ML 모드의 콜백 호출을 `try/catch`로 감싸 한 모드의 예외가 다른 모드나 UMM에 영향을 주지 않는다.

---

## 스텁 API 표면

### Core

```csharp
public abstract class MelonBase
{
    public MelonInfoAttribute Info { get; internal set; }
    public MelonLogger.Instance LoggerInstance { get; internal set; }

    public virtual void OnEarlyInitializeMelon() { }
    public virtual void OnInitializeMelon() { }
    public virtual void OnDeinitializeMelon() { }
    public virtual void OnApplicationStart() { }
    public virtual void OnApplicationLateStart() { }
    public virtual void OnApplicationQuit() { }
    public virtual void OnApplicationDefiniteQuit() { }
    public virtual void OnUpdate() { }
    public virtual void OnFixedUpdate() { }
    public virtual void OnLateUpdate() { }
    public virtual void OnGUI() { }
    public virtual void OnPreferencesLoaded() { }
    public virtual void OnPreferencesSaved() { }
    public virtual void OnSceneWasLoaded(int buildIndex, string sceneName) { }
    public virtual void OnSceneWasInitialized(int buildIndex, string sceneName) { }
    public virtual void OnSceneWasUnloaded(int buildIndex, string sceneName) { }
}

public abstract class MelonMod : MelonBase { }
public abstract class MelonPlugin : MelonBase { }
```

### 로깅

```csharp
public static class MelonLogger
{
    public static void Msg(string msg);
    public static void Msg(object obj);
    public static void Msg(ConsoleColor color, string msg);
    public static void Warning(string msg);
    public static void Error(string msg);
    public static void BigError(string msg);

    public class Instance
    {
        public string Name { get; }
        public void Msg(string msg);
        public void Warning(string msg);
        public void Error(string msg);
    }
}
```

### Preferences

```csharp
public static class MelonPreferences
{
    public static MelonPreferences_Category CreateCategory(string id, string displayName = null);
    public static MelonPreferences_Entry<T> CreateEntry<T>(string categoryId, string id, T defaultValue, string displayName = null);
    public static void LoadAll();
    public static void SaveAll();
}

public class MelonPreferences_Category
{
    public string Identifier { get; }
    public string DisplayName { get; }
    public MelonPreferences_Entry<T> CreateEntry<T>(string id, T defaultValue, string displayName = null);
}

public class MelonPreferences_Entry<T>
{
    public T Value { get; set; }
    public T DefaultValue { get; }
    public event Action OnEntryValueChangedUntyped;
}
```

### 어트리뷰트

```csharp
[AttributeUsage(AttributeTargets.Assembly)]
public class MelonInfoAttribute : Attribute
{
    public Type SystemType;
    public string Name, Version, Author, DownloadLink;
}

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class MelonGameAttribute : Attribute
{
    public string Developer, Name;
}

// 추가 어트리뷰트:
// MelonPriorityAttribute, MelonColorAttribute, MelonAuthorColorAttribute,
// MelonOptionalDependenciesAttribute, MelonIncompatibleAssembliesAttribute,
// MelonPlatformDomainAttribute, MelonProcessIdAttribute
```

### Events / Coroutines

```csharp
public static class MelonEvents
{
    public static readonly MelonEvent OnApplicationEarlyStart;
    public static readonly MelonEvent OnApplicationStart;
    public static readonly MelonEvent OnApplicationLateStart;
    public static readonly MelonEvent OnApplicationQuit;
    public static readonly MelonEvent OnUpdate;
    public static readonly MelonEvent OnFixedUpdate;
    public static readonly MelonEvent OnLateUpdate;
    public static readonly MelonEvent OnGUI;
}

public static class MelonCoroutines
{
    public static Coroutine Start(IEnumerator routine);
    public static void Stop(Coroutine coroutine);
}
```

### IL2CPP 스텁 (no-op)

`RegisterTypeInIl2Cpp`, `Il2CppSystem.*` 등 — 컴파일 에러 방지용 빈 구현만 제공. Mono 환경에서는 불필요하고, IL2CPP 환경에서는 게임의 생성 어셈블리가 대신하므로 충돌 없이 무시된다.

---

## 모드 로딩 세부 사항

1. `MelonInfo` 어트리뷰트의 `SystemType`으로 메인 클래스 특정
2. `MelonPriority` 어트리뷰트 기준 오름차순 정렬 후 초기화
3. `MelonPlatformDomain` 체크 — IL2CPP 전용 모드를 Mono에서 로드 시 경고 출력 후 스킵
4. 초기화 순서: `MelonPlugin` → `MelonMod`

---

## 설정 영속화

- ML 원본 형식인 TOML 기반 `UserData/MelonPreferences.cfg` 사용
- UMM 자체 설정 시스템과 분리 (충돌 방지)
- 경량 TOML 파서 내장 (Tomlet)

---

## MelonCoroutines 구현

`MelonBridge` 초기화 시 빈 `GameObject`에 `MelonCoroutineRunner` MonoBehaviour를 부착. `MelonCoroutines.Start()`는 이 컴포넌트에 위임한다.

---

## 한계 및 제약

- **IL2CPP interop 완전 구현 불가**: `RegisterTypeInIl2Cpp`, `Il2CppInterop` 에코시스템은 게임의 생성 어셈블리에 의존하므로 no-op 스텁만 제공. IL2CPP 전용 ML 모드는 부분적으로만 동작할 수 있다.
- **HarmonyX vs UMM Harmony 충돌**: UMM이 번들하는 Harmony와 HarmonyX가 같은 어셈블리 이름(`0Harmony`)을 사용. `Assembly.LoadFrom()` 경로 지정으로 분리하되, 두 인스턴스가 동일 메서드를 패칭할 경우 충돌 가능. 이 경우 경고 로그를 출력하고, UMM Harmony를 우선한다.
- **IL2CPP interop 완전 구현 불가**: Il2CppInterop / Unhollower DLL을 번들하더라도 게임의 생성 어셈블리(예: `Assembly-CSharp-il2cpp.dll`) 없이는 IL2CPP 전용 모드가 실제로 동작하지 않는다. Mono 게임에서는 이 DLL들이 참조만 되고 실행되지 않으므로 문제없다.
