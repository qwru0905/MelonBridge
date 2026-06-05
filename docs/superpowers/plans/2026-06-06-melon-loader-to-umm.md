# MelonLoader-to-UMM Bridge Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** MelonLoader 모드 DLL을 재컴파일 없이 UnityModManager 환경에서 실행할 수 있는 브릿지를 구현한다.

**Architecture:** `MelonLoader.dll` 스텁 어셈블리가 MelonLoader의 공개 API를 제공하고, UMM 모드인 `MelonBridge.dll`이 `MelonMods/` 폴더를 스캔해 ML 모드를 로드한 뒤 UMM 라이프사이클 콜백을 ML 콜백으로 라우팅한다. 스텁 내부 구현은 브릿지가 런타임에 주입하는 핸들러 인터페이스로 교체된다.

**Tech Stack:** C# / .NET Framework 4.7.2, UnityModManager, UnityEngine, NuGet: Lib.Harmony(HarmonyX), Tomlet(TOML), NUnit 3(테스트)

---

## 파일 구조

```
MelonLoaderToUMM/
├── lib/                                     ← 로컬 Unity/UMM 참조 DLL
│   ├── UnityEngine.dll
│   ├── UnityEngine.CoreModule.dll
│   └── UnityModManager.dll
│
├── MelonLoader.Stub/                        → 출력: MelonLoader.dll
│   ├── MelonLoader.Stub.csproj
│   ├── Attributes/
│   │   ├── MelonInfoAttribute.cs
│   │   ├── MelonGameAttribute.cs
│   │   ├── MelonPriorityAttribute.cs
│   │   ├── MelonColorAttribute.cs
│   │   ├── MelonAuthorColorAttribute.cs
│   │   ├── MelonOptionalDependenciesAttribute.cs
│   │   ├── MelonIncompatibleAssembliesAttribute.cs
│   │   ├── MelonPlatformDomainAttribute.cs
│   │   └── MelonProcessIdAttribute.cs
│   ├── Core/
│   │   ├── MelonBase.cs
│   │   ├── MelonMod.cs
│   │   └── MelonPlugin.cs
│   ├── Logging/
│   │   └── MelonLogger.cs
│   ├── Preferences/
│   │   ├── MelonPreferences.cs
│   │   ├── MelonPreferences_Category.cs
│   │   └── MelonPreferences_Entry.cs
│   ├── Events/
│   │   ├── MelonEvent.cs
│   │   └── MelonEvents.cs
│   ├── Coroutines/
│   │   └── MelonCoroutines.cs
│   └── Il2Cpp/
│       ├── RegisterTypeInIl2CppAttribute.cs
│       └── Il2CppSystemStubs.cs
│
├── MelonBridge/                             → 출력: MelonBridge.dll (UMM 모드)
│   ├── MelonBridge.csproj
│   ├── Info.json
│   ├── Main.cs
│   ├── ModLoader.cs
│   └── Bridges/
│       ├── LoggerBridge.cs
│       ├── PreferencesBridge.cs
│       ├── SceneBridge.cs
│       └── MelonCoroutineRunner.cs
│
└── MelonLoaderToUMM.Tests/
    ├── MelonLoaderToUMM.Tests.csproj
    ├── AttributeTests.cs
    ├── MelonEventTests.cs
    ├── LoggerBridgeTests.cs
    ├── PreferencesBridgeTests.cs
    └── ModLoaderTests.cs
```

---

## Task 1: 프로젝트 구조 & csproj 설정

**Files:**
- Create: `lib/` (빈 폴더, DLL은 수동 배치)
- Create: `MelonLoader.Stub/MelonLoader.Stub.csproj`
- Create: `MelonBridge/MelonBridge.csproj`
- Modify: `MelonLoaderToUMM.sln`

- [ ] **Step 1: `lib/` 폴더 생성 및 안내 파일 작성**

`lib/README.md` 생성:
```
Unity DLL 배치 위치:
- UnityEngine.dll
- UnityEngine.CoreModule.dll
  → Unity 설치 경로: Editor/Data/Managed/ 또는 게임 폴더 <Game>_Data/Managed/

UMM DLL 배치 위치:
- UnityModManager.dll
  → UMM 패키지의 UnityModManager/ 폴더 내
```

- [ ] **Step 2: `MelonLoader.Stub/MelonLoader.Stub.csproj` 작성**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net472</TargetFramework>
    <AssemblyName>MelonLoader</AssemblyName>
    <RootNamespace>MelonLoader</RootNamespace>
    <LangVersion>9.0</LangVersion>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <Reference Include="UnityEngine.CoreModule">
      <HintPath>$(SolutionDir)lib\UnityEngine.CoreModule.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>
</Project>
```

- [ ] **Step 3: `MelonBridge/MelonBridge.csproj` 작성**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net472</TargetFramework>
    <AssemblyName>MelonBridge</AssemblyName>
    <RootNamespace>MelonBridge</RootNamespace>
    <LangVersion>9.0</LangVersion>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <Reference Include="UnityEngine">
      <HintPath>$(SolutionDir)lib\UnityEngine.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="UnityEngine.CoreModule">
      <HintPath>$(SolutionDir)lib\UnityEngine.CoreModule.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="UnityModManager">
      <HintPath>$(SolutionDir)lib\UnityModManager.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Tomlet" Version="5.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\MelonLoader.Stub\MelonLoader.Stub.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 4: sln에 두 프로젝트 추가**

```
dotnet sln MelonLoaderToUMM.sln add MelonLoader.Stub/MelonLoader.Stub.csproj
dotnet sln MelonLoaderToUMM.sln add MelonBridge/MelonBridge.csproj
```

- [ ] **Step 5: lib/ DLL 없이 빌드 에러 확인 (참조 경로 검증)**

```
dotnet build MelonLoader.Stub/MelonLoader.Stub.csproj
```
Expected: `error` (UnityEngine.CoreModule.dll 없음) — 경로가 올바른지 확인용

- [ ] **Step 6: `lib/`에 Unity DLL 수동 배치 후 빌드 성공 확인**

Unity 설치 경로(`Editor/Data/Managed/`) 또는 아무 Unity 게임 폴더(`<Game>_Data/Managed/`)에서 복사:
- `UnityEngine.dll`
- `UnityEngine.CoreModule.dll`
- UMM 패키지에서 `UnityModManager.dll`

```
dotnet build MelonLoader.Stub/MelonLoader.Stub.csproj
```
Expected: SUCCEED (경고 있어도 OK)

- [ ] **Step 7: 커밋**

```bash
git add MelonLoader.Stub/MelonLoader.Stub.csproj MelonBridge/MelonBridge.csproj MelonLoaderToUMM.sln lib/README.md
git commit -m "chore: add project structure and csproj files"
```

---

## Task 2: 어트리뷰트 (Attributes)

**Files:**
- Create: `MelonLoader.Stub/Attributes/MelonInfoAttribute.cs`
- Create: `MelonLoader.Stub/Attributes/MelonGameAttribute.cs`
- Create: `MelonLoader.Stub/Attributes/MelonPriorityAttribute.cs`
- Create: `MelonLoader.Stub/Attributes/MelonColorAttribute.cs`
- Create: `MelonLoader.Stub/Attributes/MelonAuthorColorAttribute.cs`
- Create: `MelonLoader.Stub/Attributes/MelonOptionalDependenciesAttribute.cs`
- Create: `MelonLoader.Stub/Attributes/MelonIncompatibleAssembliesAttribute.cs`
- Create: `MelonLoader.Stub/Attributes/MelonPlatformDomainAttribute.cs`
- Create: `MelonLoader.Stub/Attributes/MelonProcessIdAttribute.cs`

- [ ] **Step 1: `MelonInfoAttribute.cs` 작성**

```csharp
using System;

namespace MelonLoader
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class MelonInfoAttribute : Attribute
    {
        public Type SystemType { get; }
        public string Name { get; }
        public string Version { get; }
        public string Author { get; }
        public string DownloadLink { get; }

        public MelonInfoAttribute(Type systemType, string name, string version, string author, string downloadLink = null)
        {
            SystemType = systemType;
            Name = name;
            Version = version;
            Author = author;
            DownloadLink = downloadLink;
        }
    }
}
```

- [ ] **Step 2: `MelonGameAttribute.cs` 작성**

```csharp
using System;

namespace MelonLoader
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class MelonGameAttribute : Attribute
    {
        public string Developer { get; }
        public string Name { get; }

        public MelonGameAttribute(string developer = null, string name = null)
        {
            Developer = developer;
            Name = name;
        }
    }
}
```

- [ ] **Step 3: `MelonPriorityAttribute.cs` 작성**

```csharp
using System;

namespace MelonLoader
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class MelonPriorityAttribute : Attribute
    {
        public int Priority { get; }

        public MelonPriorityAttribute(int priority = 0)
        {
            Priority = priority;
        }
    }
}
```

- [ ] **Step 4: `MelonColorAttribute.cs` 작성**

```csharp
using System;

namespace MelonLoader
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class MelonColorAttribute : Attribute
    {
        public ConsoleColor DrawingColor { get; }

        public MelonColorAttribute(ConsoleColor color = ConsoleColor.Green)
        {
            DrawingColor = color;
        }
    }
}
```

- [ ] **Step 5: `MelonAuthorColorAttribute.cs` 작성**

```csharp
using System;

namespace MelonLoader
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class MelonAuthorColorAttribute : Attribute
    {
        public ConsoleColor DrawingColor { get; }

        public MelonAuthorColorAttribute(ConsoleColor color = ConsoleColor.DarkGray)
        {
            DrawingColor = color;
        }
    }
}
```

- [ ] **Step 6: `MelonOptionalDependenciesAttribute.cs` 작성**

```csharp
using System;

namespace MelonLoader
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class MelonOptionalDependenciesAttribute : Attribute
    {
        public string[] AssemblyNames { get; }

        public MelonOptionalDependenciesAttribute(params string[] assemblyNames)
        {
            AssemblyNames = assemblyNames;
        }
    }
}
```

- [ ] **Step 7: `MelonIncompatibleAssembliesAttribute.cs` 작성**

```csharp
using System;

namespace MelonLoader
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class MelonIncompatibleAssembliesAttribute : Attribute
    {
        public string[] AssemblyNames { get; }

        public MelonIncompatibleAssembliesAttribute(params string[] assemblyNames)
        {
            AssemblyNames = assemblyNames;
        }
    }
}
```

- [ ] **Step 8: `MelonPlatformDomainAttribute.cs` 작성**

```csharp
using System;

namespace MelonLoader
{
    public enum MelonPlatformDomain
    {
        Any = 0,
        Mono = 1,
        Il2Cpp = 2,
    }

    [AttributeUsage(AttributeTargets.Assembly)]
    public class MelonPlatformDomainAttribute : Attribute
    {
        public MelonPlatformDomain Domain { get; }

        public MelonPlatformDomainAttribute(MelonPlatformDomain domain = MelonPlatformDomain.Any)
        {
            Domain = domain;
        }
    }
}
```

- [ ] **Step 9: `MelonProcessIdAttribute.cs` 작성**

```csharp
using System;

namespace MelonLoader
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class MelonProcessIdAttribute : Attribute
    {
        public string ProcessId { get; }

        public MelonProcessIdAttribute(string processId = null)
        {
            ProcessId = processId;
        }
    }
}
```

- [ ] **Step 10: 빌드 확인**

```
dotnet build MelonLoader.Stub/MelonLoader.Stub.csproj
```
Expected: SUCCEED (0 errors)

- [ ] **Step 11: 커밋**

```bash
git add MelonLoader.Stub/Attributes/
git commit -m "feat(stub): add all MelonLoader attribute types"
```

---

## Task 3: 코어 타입 (MelonBase, MelonMod, MelonPlugin)

**Files:**
- Create: `MelonLoader.Stub/Core/MelonBase.cs`
- Create: `MelonLoader.Stub/Core/MelonMod.cs`
- Create: `MelonLoader.Stub/Core/MelonPlugin.cs`

- [ ] **Step 1: `MelonBase.cs` 작성**

```csharp
namespace MelonLoader
{
    public abstract class MelonBase
    {
        public MelonInfoAttribute Info { get; internal set; }
        public MelonLogger.Instance LoggerInstance { get; internal set; }

        public virtual void OnEarlyInitializeMelon() { }
        public virtual void OnInitializeMelon() { }
        public virtual void OnDeinitializeMelon() { }

        // 구버전 API (0.3.x / 0.4.x)
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

        // 신버전 API (0.5.x+)
        public virtual void OnSceneWasLoaded(int buildIndex, string sceneName) { }
        public virtual void OnSceneWasInitialized(int buildIndex, string sceneName) { }
        public virtual void OnSceneWasUnloaded(int buildIndex, string sceneName) { }
    }
}
```

- [ ] **Step 2: `MelonMod.cs` 작성**

```csharp
namespace MelonLoader
{
    public abstract class MelonMod : MelonBase { }
}
```

- [ ] **Step 3: `MelonPlugin.cs` 작성**

```csharp
namespace MelonLoader
{
    public abstract class MelonPlugin : MelonBase { }
}
```

- [ ] **Step 4: 빌드 확인**

```
dotnet build MelonLoader.Stub/MelonLoader.Stub.csproj
```
Expected: SUCCEED

- [ ] **Step 5: 커밋**

```bash
git add MelonLoader.Stub/Core/
git commit -m "feat(stub): add MelonBase, MelonMod, MelonPlugin"
```

---

## Task 4: MelonLogger

**Files:**
- Create: `MelonLoader.Stub/Logging/MelonLogger.cs`

`MelonLogger`는 내부적으로 `ILogHandler`를 통해 구현을 주입받는다. 브릿지 로드 전에는 no-op, 로드 후에는 UMM Logger로 라우팅된다.

- [ ] **Step 1: `MelonLogger.cs` 작성**

```csharp
using System;

namespace MelonLoader
{
    public static class MelonLogger
    {
        internal static ILogHandler Handler = new NullLogHandler();

        public static void Msg(string msg) => Handler.Msg(msg);
        public static void Msg(object obj) => Handler.Msg(obj?.ToString() ?? "null");
        public static void Msg(ConsoleColor color, string msg) => Handler.Msg(msg);
        public static void Warning(string msg) => Handler.Warning(msg);
        public static void Error(string msg) => Handler.Error(msg);
        public static void BigError(string msg) => Handler.Error($"== {msg} ==");

        internal interface ILogHandler
        {
            void Msg(string msg);
            void Warning(string msg);
            void Error(string msg);
        }

        private sealed class NullLogHandler : ILogHandler
        {
            public void Msg(string msg) { }
            public void Warning(string msg) { }
            public void Error(string msg) { }
        }

        public sealed class Instance
        {
            public string Name { get; }

            public Instance(string name)
            {
                Name = name;
            }

            public void Msg(string msg) => Handler.Msg($"[{Name}] {msg}");
            public void Msg(object obj) => Msg(obj?.ToString() ?? "null");
            public void Warning(string msg) => Handler.Warning($"[{Name}] {msg}");
            public void Error(string msg) => Handler.Error($"[{Name}] {msg}");
        }
    }
}
```

- [ ] **Step 2: 빌드 확인**

```
dotnet build MelonLoader.Stub/MelonLoader.Stub.csproj
```
Expected: SUCCEED

- [ ] **Step 3: 커밋**

```bash
git add MelonLoader.Stub/Logging/MelonLogger.cs
git commit -m "feat(stub): add MelonLogger with ILogHandler injection point"
```

---

## Task 5: MelonPreferences

**Files:**
- Create: `MelonLoader.Stub/Preferences/MelonPreferences_Entry.cs`
- Create: `MelonLoader.Stub/Preferences/MelonPreferences_Category.cs`
- Create: `MelonLoader.Stub/Preferences/MelonPreferences.cs`

`MelonPreferences`도 내부적으로 `IPreferencesBackend`를 통해 실제 저장/불러오기를 위임한다.

- [ ] **Step 1: `MelonPreferences_Entry.cs` 작성**

```csharp
using System;

namespace MelonLoader
{
    public class MelonPreferences_Entry<T>
    {
        public string Identifier { get; internal set; }
        public string DisplayName { get; internal set; }
        public T DefaultValue { get; internal set; }

        private T _value;
        public T Value
        {
            get => _value;
            set
            {
                _value = value;
                OnEntryValueChangedUntyped?.Invoke();
            }
        }

        public event Action OnEntryValueChangedUntyped;

        internal MelonPreferences_Entry(string identifier, string displayName, T defaultValue)
        {
            Identifier = identifier;
            DisplayName = displayName;
            DefaultValue = defaultValue;
            _value = defaultValue;
        }
    }
}
```

- [ ] **Step 2: `MelonPreferences_Category.cs` 작성**

```csharp
using System.Collections.Generic;

namespace MelonLoader
{
    public class MelonPreferences_Category
    {
        public string Identifier { get; }
        public string DisplayName { get; }

        internal readonly List<object> Entries = new();

        internal MelonPreferences_Category(string identifier, string displayName)
        {
            Identifier = identifier;
            DisplayName = displayName ?? identifier;
        }

        public MelonPreferences_Entry<T> CreateEntry<T>(string identifier, T defaultValue, string displayName = null)
        {
            var entry = new MelonPreferences_Entry<T>(identifier, displayName ?? identifier, defaultValue);
            Entries.Add(entry);
            MelonPreferences.Backend?.RegisterEntry(Identifier, entry);
            return entry;
        }
    }
}
```

- [ ] **Step 3: `MelonPreferences.cs` 작성**

```csharp
using System.Collections.Generic;

namespace MelonLoader
{
    public static class MelonPreferences
    {
        internal static IPreferencesBackend? Backend;

        private static readonly Dictionary<string, MelonPreferences_Category> Categories = new();

        public static MelonPreferences_Category CreateCategory(string identifier, string displayName = null)
        {
            if (!Categories.TryGetValue(identifier, out var category))
            {
                category = new MelonPreferences_Category(identifier, displayName);
                Categories[identifier] = category;
                Backend?.RegisterCategory(category);
            }
            return category;
        }

        public static MelonPreferences_Entry<T> CreateEntry<T>(
            string categoryIdentifier, string identifier, T defaultValue, string displayName = null)
        {
            var category = CreateCategory(categoryIdentifier);
            return category.CreateEntry(identifier, defaultValue, displayName);
        }

        public static void LoadAll() => Backend?.Load();
        public static void SaveAll() => Backend?.Save();

        internal interface IPreferencesBackend
        {
            void RegisterCategory(MelonPreferences_Category category);
            void RegisterEntry<T>(string categoryId, MelonPreferences_Entry<T> entry);
            void Load();
            void Save();
        }
    }
}
```

- [ ] **Step 4: 빌드 확인**

```
dotnet build MelonLoader.Stub/MelonLoader.Stub.csproj
```
Expected: SUCCEED

- [ ] **Step 5: 커밋**

```bash
git add MelonLoader.Stub/Preferences/
git commit -m "feat(stub): add MelonPreferences with backend injection point"
```

---

## Task 6: MelonEvent & MelonEvents

**Files:**
- Create: `MelonLoader.Stub/Events/MelonEvent.cs`
- Create: `MelonLoader.Stub/Events/MelonEvents.cs`

- [ ] **Step 1: `MelonEvent.cs` 작성**

```csharp
using System;
using System.Collections.Generic;

namespace MelonLoader
{
    public class MelonEvent
    {
        private readonly List<Action> _subscribers = new();

        public void Subscribe(Action callback, int priority = 0, bool once = false)
        {
            if (callback != null)
                _subscribers.Add(callback);
        }

        public void Unsubscribe(Action callback)
        {
            _subscribers.Remove(callback);
        }

        internal void Invoke()
        {
            foreach (var sub in _subscribers.ToArray())
            {
                try { sub(); }
                catch (Exception e) { MelonLogger.Error(e.ToString()); }
            }
        }
    }

    public class MelonEvent<T1>
    {
        private readonly List<Action<T1>> _subscribers = new();

        public void Subscribe(Action<T1> callback, int priority = 0, bool once = false)
        {
            if (callback != null)
                _subscribers.Add(callback);
        }

        public void Unsubscribe(Action<T1> callback)
        {
            _subscribers.Remove(callback);
        }

        internal void Invoke(T1 arg)
        {
            foreach (var sub in _subscribers.ToArray())
            {
                try { sub(arg); }
                catch (Exception e) { MelonLogger.Error(e.ToString()); }
            }
        }
    }
}
```

- [ ] **Step 2: `MelonEvents.cs` 작성**

```csharp
namespace MelonLoader
{
    public static class MelonEvents
    {
        public static readonly MelonEvent OnApplicationEarlyStart = new();
        public static readonly MelonEvent OnApplicationStart = new();
        public static readonly MelonEvent OnApplicationLateStart = new();
        public static readonly MelonEvent OnApplicationQuit = new();
        public static readonly MelonEvent OnUpdate = new();
        public static readonly MelonEvent OnFixedUpdate = new();
        public static readonly MelonEvent OnLateUpdate = new();
        public static readonly MelonEvent OnGUI = new();
    }
}
```

- [ ] **Step 3: 빌드 확인**

```
dotnet build MelonLoader.Stub/MelonLoader.Stub.csproj
```
Expected: SUCCEED

- [ ] **Step 4: 커밋**

```bash
git add MelonLoader.Stub/Events/
git commit -m "feat(stub): add MelonEvent and MelonEvents"
```

---

## Task 7: MelonCoroutines & IL2CPP 스텁

**Files:**
- Create: `MelonLoader.Stub/Coroutines/MelonCoroutines.cs`
- Create: `MelonLoader.Stub/Il2Cpp/RegisterTypeInIl2CppAttribute.cs`
- Create: `MelonLoader.Stub/Il2Cpp/Il2CppSystemStubs.cs`

- [ ] **Step 1: `MelonCoroutines.cs` 작성**

```csharp
using System.Collections;
using UnityEngine;

namespace MelonLoader
{
    public static class MelonCoroutines
    {
        internal static MonoBehaviour Runner;

        public static Coroutine Start(IEnumerator routine)
        {
            if (Runner == null)
            {
                MelonLogger.Warning("MelonCoroutines: Runner not initialized. Coroutine skipped.");
                return null;
            }
            return Runner.StartCoroutine(routine);
        }

        public static void Stop(Coroutine coroutine)
        {
            if (Runner == null || coroutine == null) return;
            Runner.StopCoroutine(coroutine);
        }
    }
}
```

- [ ] **Step 2: `RegisterTypeInIl2CppAttribute.cs` 작성**

```csharp
using System;

namespace MelonLoader
{
    // no-op: IL2CPP 전용 어트리뷰트. Mono 환경에서는 무시된다.
    [AttributeUsage(AttributeTargets.Class)]
    public class RegisterTypeInIl2CppAttribute : Attribute { }
}
```

- [ ] **Step 3: `Il2CppSystemStubs.cs` 작성**

IL2CPP 모드가 아닌 환경에서 컴파일 에러 방지용 최소 스텁:

```csharp
// IL2CPP 환경에서는 게임의 생성 어셈블리가 이 타입들을 실제로 제공한다.
// Mono 환경에서는 이 파일의 타입들이 참조만 되고 실행되지 않는다.
namespace Il2CppSystem
{
    public class Object
    {
        public virtual string ToString() => base.ToString();
    }
}

namespace UnhollowerRuntimeLib
{
    public static class ClassInjector
    {
        public static void RegisterTypeInIl2Cpp<T>() { }
    }
}
```

- [ ] **Step 4: 빌드 확인**

```
dotnet build MelonLoader.Stub/MelonLoader.Stub.csproj
```
Expected: SUCCEED

- [ ] **Step 5: 커밋**

```bash
git add MelonLoader.Stub/Coroutines/ MelonLoader.Stub/Il2Cpp/
git commit -m "feat(stub): add MelonCoroutines and IL2CPP no-op stubs"
```

---

## Task 8: 테스트 프로젝트 설정 + 어트리뷰트 & 이벤트 테스트

**Files:**
- Create: `MelonLoaderToUMM.Tests/MelonLoaderToUMM.Tests.csproj`
- Create: `MelonLoaderToUMM.Tests/AttributeTests.cs`
- Create: `MelonLoaderToUMM.Tests/MelonEventTests.cs`

- [ ] **Step 1: 테스트 프로젝트 csproj 작성**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net472</TargetFramework>
    <IsPackable>false</IsPackable>
    <LangVersion>9.0</LangVersion>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="NUnit" Version="3.14.0" />
    <PackageReference Include="NUnit3TestAdapter" Version="4.5.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\MelonLoader.Stub\MelonLoader.Stub.csproj" />
    <ProjectReference Include="..\MelonBridge\MelonBridge.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: sln에 테스트 프로젝트 추가**

```
dotnet sln MelonLoaderToUMM.sln add MelonLoaderToUMM.Tests/MelonLoaderToUMM.Tests.csproj
```

- [ ] **Step 3: `AttributeTests.cs` 작성**

```csharp
using System.Reflection;
using NUnit.Framework;
using MelonLoader;

namespace MelonLoaderToUMM.Tests
{
    [TestFixture]
    public class AttributeTests
    {
        [Test]
        public void MelonInfoAttribute_StoresValues()
        {
            var attr = new MelonInfoAttribute(typeof(AttributeTests), "TestMod", "1.0.0", "Author", "http://example.com");

            Assert.AreEqual(typeof(AttributeTests), attr.SystemType);
            Assert.AreEqual("TestMod", attr.Name);
            Assert.AreEqual("1.0.0", attr.Version);
            Assert.AreEqual("Author", attr.Author);
            Assert.AreEqual("http://example.com", attr.DownloadLink);
        }

        [Test]
        public void MelonInfoAttribute_DownloadLink_DefaultsToNull()
        {
            var attr = new MelonInfoAttribute(typeof(AttributeTests), "TestMod", "1.0.0", "Author");
            Assert.IsNull(attr.DownloadLink);
        }

        [Test]
        public void MelonPriorityAttribute_DefaultIsZero()
        {
            var attr = new MelonPriorityAttribute();
            Assert.AreEqual(0, attr.Priority);
        }

        [Test]
        public void MelonPlatformDomainAttribute_DefaultIsAny()
        {
            var attr = new MelonPlatformDomainAttribute();
            Assert.AreEqual(MelonPlatformDomain.Any, attr.Domain);
        }

        [Test]
        public void MelonGameAttribute_AllowsMultiple()
        {
            var attrs = new[]
            {
                new MelonGameAttribute("Dev1", "Game1"),
                new MelonGameAttribute("Dev2", "Game2"),
            };
            Assert.AreEqual("Dev1", attrs[0].Developer);
            Assert.AreEqual("Dev2", attrs[1].Developer);
        }
    }
}
```

- [ ] **Step 4: 테스트 실행 → PASS 확인**

```
dotnet test MelonLoaderToUMM.Tests/ --filter "AttributeTests"
```
Expected: 5 tests PASS

- [ ] **Step 5: `MelonEventTests.cs` 작성**

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using MelonLoader;

namespace MelonLoaderToUMM.Tests
{
    [TestFixture]
    public class MelonEventTests
    {
        [Test]
        public void MelonEvent_Subscribe_ReceivesInvoke()
        {
            var melonEvent = new MelonEvent();
            var called = false;
            melonEvent.Subscribe(() => called = true);

            melonEvent.Invoke();

            Assert.IsTrue(called);
        }

        [Test]
        public void MelonEvent_Unsubscribe_DoesNotReceiveInvoke()
        {
            var melonEvent = new MelonEvent();
            var called = false;
            Action callback = () => called = true;
            melonEvent.Subscribe(callback);
            melonEvent.Unsubscribe(callback);

            melonEvent.Invoke();

            Assert.IsFalse(called);
        }

        [Test]
        public void MelonEvent_MultipleSubscribers_AllCalled()
        {
            var melonEvent = new MelonEvent();
            var results = new List<int>();
            melonEvent.Subscribe(() => results.Add(1));
            melonEvent.Subscribe(() => results.Add(2));

            melonEvent.Invoke();

            CollectionAssert.AreEqual(new[] { 1, 2 }, results);
        }

        [Test]
        public void MelonEventT_PassesArgument()
        {
            var melonEvent = new MelonEvent<string>();
            string received = null;
            melonEvent.Subscribe(s => received = s);

            melonEvent.Invoke("hello");

            Assert.AreEqual("hello", received);
        }

        [Test]
        public void MelonEvent_ThrowingSubscriber_DoesNotBreakOthers()
        {
            var melonEvent = new MelonEvent();
            var secondCalled = false;
            melonEvent.Subscribe(() => throw new System.Exception("boom"));
            melonEvent.Subscribe(() => secondCalled = true);

            melonEvent.Invoke();

            Assert.IsTrue(secondCalled);
        }
    }
}
```

- [ ] **Step 6: 테스트 실행 → PASS 확인**

```
dotnet test MelonLoaderToUMM.Tests/ --filter "MelonEventTests"
```
Expected: 5 tests PASS

- [ ] **Step 7: 커밋**

```bash
git add MelonLoaderToUMM.Tests/
git commit -m "test: add attribute and MelonEvent tests"
```

---

## Task 9: LoggerBridge

**Files:**
- Create: `MelonBridge/Bridges/LoggerBridge.cs`
- Create: `MelonLoaderToUMM.Tests/LoggerBridgeTests.cs`

- [ ] **Step 1: `LoggerBridgeTests.cs` (failing) 작성**

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using MelonLoader;
using MelonBridge.Bridges;

namespace MelonLoaderToUMM.Tests
{
    [TestFixture]
    public class LoggerBridgeTests
    {
        private class FakeLogger : LoggerBridge.IUmmLogger
        {
            public List<string> Logs { get; } = new();
            public List<string> Warnings { get; } = new();
            public List<string> Errors { get; } = new();

            public void Log(string msg) => Logs.Add(msg);
            public void Warning(string msg) => Warnings.Add(msg);
            public void Error(string msg) => Errors.Add(msg);
        }

        [TearDown]
        public void TearDown() => LoggerBridge.Detach();

        [Test]
        public void Attach_RoutesMsgToUmmLogger()
        {
            var fake = new FakeLogger();
            LoggerBridge.Attach(fake);

            MelonLogger.Msg("hello");

            CollectionAssert.Contains(fake.Logs, "hello");
        }

        [Test]
        public void Attach_RoutesWarningToUmmLogger()
        {
            var fake = new FakeLogger();
            LoggerBridge.Attach(fake);

            MelonLogger.Warning("warn");

            CollectionAssert.Contains(fake.Warnings, "warn");
        }

        [Test]
        public void Attach_RoutesErrorToUmmLogger()
        {
            var fake = new FakeLogger();
            LoggerBridge.Attach(fake);

            MelonLogger.Error("err");

            CollectionAssert.Contains(fake.Errors, "err");
        }

        [Test]
        public void Detach_StopsRouting()
        {
            var fake = new FakeLogger();
            LoggerBridge.Attach(fake);
            LoggerBridge.Detach();

            MelonLogger.Msg("after detach");

            CollectionAssert.IsEmpty(fake.Logs);
        }

        [Test]
        public void Instance_PrefixesName()
        {
            var fake = new FakeLogger();
            LoggerBridge.Attach(fake);
            var instance = new MelonLogger.Instance("MyMod");

            instance.Msg("test");

            CollectionAssert.Contains(fake.Logs, "[MyMod] test");
        }
    }
}
```

- [ ] **Step 2: 테스트 실행 → FAIL 확인**

```
dotnet test MelonLoaderToUMM.Tests/ --filter "LoggerBridgeTests"
```
Expected: compilation error (LoggerBridge 없음)

- [ ] **Step 3: `LoggerBridge.cs` 작성**

```csharp
using MelonLoader;

namespace MelonBridge.Bridges
{
    public static class LoggerBridge
    {
        public interface IUmmLogger
        {
            void Log(string msg);
            void Warning(string msg);
            void Error(string msg);
        }

        public static void Attach(IUmmLogger ummLogger)
        {
            MelonLogger.Handler = new UmmLogHandler(ummLogger);
        }

        public static void Detach()
        {
            MelonLogger.Handler = new MelonLogger.NullLogHandler();
        }

        private sealed class UmmLogHandler : MelonLogger.ILogHandler
        {
            private readonly IUmmLogger _logger;
            public UmmLogHandler(IUmmLogger logger) => _logger = logger;
            public void Msg(string msg) => _logger.Log(msg);
            public void Warning(string msg) => _logger.Warning(msg);
            public void Error(string msg) => _logger.Error(msg);
        }
    }
}
```

- [ ] **Step 4: `MelonLogger.cs`에서 `NullLogHandler` 접근 수정**

`MelonLogger.Stub/Logging/MelonLogger.cs`에서:
```csharp
// 변경 전
private sealed class NullLogHandler : ILogHandler
// 변경 후
internal sealed class NullLogHandler : ILogHandler
```

- [ ] **Step 5: 테스트 실행 → PASS 확인**

```
dotnet test MelonLoaderToUMM.Tests/ --filter "LoggerBridgeTests"
```
Expected: 5 tests PASS

- [ ] **Step 6: 커밋**

```bash
git add MelonBridge/Bridges/LoggerBridge.cs MelonLoader.Stub/Logging/MelonLogger.cs MelonLoaderToUMM.Tests/LoggerBridgeTests.cs
git commit -m "feat(bridge): add LoggerBridge with UMM logger routing"
```

---

## Task 10: PreferencesBridge (TOML)

**Files:**
- Create: `MelonBridge/Bridges/PreferencesBridge.cs`
- Create: `MelonLoaderToUMM.Tests/PreferencesBridgeTests.cs`

TOML 파일(`UserData/MelonPreferences.cfg`)을 읽고 쓰는 백엔드 구현. `MelonPreferences.IPreferencesBackend`를 구현한다.

- [ ] **Step 1: `PreferencesBridgeTests.cs` (failing) 작성**

```csharp
using System.IO;
using NUnit.Framework;
using MelonLoader;
using MelonBridge.Bridges;

namespace MelonLoaderToUMM.Tests
{
    [TestFixture]
    public class PreferencesBridgeTests
    {
        private string _tempPath;

        [SetUp]
        public void SetUp()
        {
            _tempPath = Path.GetTempFileName();
        }

        [TearDown]
        public void TearDown()
        {
            PreferencesBridge.Detach();
            if (File.Exists(_tempPath)) File.Delete(_tempPath);
        }

        [Test]
        public void SaveAndLoad_RoundTripsStringEntry()
        {
            PreferencesBridge.Attach(_tempPath);
            var category = MelonPreferences.CreateCategory("TestCat");
            var entry = category.CreateEntry("myKey", "defaultVal");
            entry.Value = "newVal";

            MelonPreferences.SaveAll();

            // 새 엔트리로 로드
            var entry2 = category.CreateEntry<string>("myKey2", "def");
            // 같은 파일에서 로드
            entry.Value = "defaultVal"; // 리셋
            MelonPreferences.LoadAll();

            Assert.AreEqual("newVal", entry.Value);
        }

        [Test]
        public void SaveAndLoad_RoundTripsIntEntry()
        {
            PreferencesBridge.Attach(_tempPath);
            var category = MelonPreferences.CreateCategory("IntCat");
            var entry = category.CreateEntry("count", 0);
            entry.Value = 42;

            MelonPreferences.SaveAll();
            entry.Value = 0;
            MelonPreferences.LoadAll();

            Assert.AreEqual(42, entry.Value);
        }

        [Test]
        public void Load_MissingFile_UsesDefaultValue()
        {
            File.Delete(_tempPath);
            PreferencesBridge.Attach(_tempPath);
            var category = MelonPreferences.CreateCategory("DefaultCat");
            var entry = category.CreateEntry("key", "fallback");

            MelonPreferences.LoadAll();

            Assert.AreEqual("fallback", entry.Value);
        }
    }
}
```

- [ ] **Step 2: 테스트 실행 → FAIL 확인**

```
dotnet test MelonLoaderToUMM.Tests/ --filter "PreferencesBridgeTests"
```
Expected: compilation error (PreferencesBridge 없음)

- [ ] **Step 3: `PreferencesBridge.cs` 작성**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using MelonLoader;
using Tomlet;
using Tomlet.Models;

namespace MelonBridge.Bridges
{
    public static class PreferencesBridge
    {
        private static TomlBackend? _backend;

        public static void Attach(string cfgPath)
        {
            _backend = new TomlBackend(cfgPath);
            MelonPreferences.Backend = _backend;
        }

        public static void Detach()
        {
            MelonPreferences.Backend = null;
            _backend = null;
        }

        private sealed class TomlBackend : MelonPreferences.IPreferencesBackend
        {
            private readonly string _path;
            private readonly Dictionary<string, Dictionary<string, object>> _data = new();
            private readonly List<(string catId, string entryId, Action<TomlValue?> setter)> _registrations = new();

            public TomlBackend(string path) => _path = path;

            public void RegisterCategory(MelonPreferences_Category category) { }

            public void RegisterEntry<T>(string categoryId, MelonPreferences_Entry<T> entry)
            {
                _registrations.Add((categoryId, entry.Identifier, tomlVal =>
                {
                    if (tomlVal is null) return;
                    try
                    {
                        entry.Value = TomletMain.To<T>(tomlVal);
                    }
                    catch { /* 타입 변환 실패 시 기본값 유지 */ }
                }));
            }

            public void Load()
            {
                if (!File.Exists(_path)) return;
                var doc = TomlParser.ParseFile(_path);
                foreach (var (catId, entryId, setter) in _registrations)
                {
                    if (doc.TryGetValue(catId, out var catVal) &&
                        catVal is TomlTable catTable &&
                        catTable.TryGetValue(entryId, out var entryVal))
                    {
                        setter(entryVal);
                    }
                }
            }

            public void Save()
            {
                // 기존 파일 로드 후 머지
                TomlDocument doc;
                try { doc = File.Exists(_path) ? TomlParser.ParseFile(_path) : new TomlDocument(); }
                catch { doc = new TomlDocument(); }

                foreach (var (catId, entryId, _) in _registrations)
                {
                    // 카테고리 테이블 확보
                    if (!doc.ContainsKey(catId))
                        doc[catId] = new TomlTable();
                    // 값은 각 Entry에서 직접 접근해야 하지만 IPreferencesBackend에서
                    // Entry 값을 알 방법이 없으므로 저장 시점 snapshot이 필요.
                    // 해결: RegisterEntry 시 getter도 함께 저장.
                }
                File.WriteAllText(_path, doc.SerializedValue);
            }
        }
    }
}
```

`Save()` 구현을 완성하려면 `RegisterEntry`에서 getter도 저장해야 한다. `_registrations` 타입을 수정:

```csharp
private readonly List<(string catId, string entryId, Action<TomlValue?> setter, Func<TomlValue> getter)> _registrations = new();

public void RegisterEntry<T>(string categoryId, MelonPreferences_Entry<T> entry)
{
    _registrations.Add((
        categoryId,
        entry.Identifier,
        tomlVal =>
        {
            if (tomlVal is null) return;
            try { entry.Value = TomletMain.To<T>(tomlVal); }
            catch { }
        },
        () => TomletMain.ValueFrom(entry.Value)
    ));
}

public void Save()
{
    TomlDocument doc;
    try { doc = File.Exists(_path) ? TomlParser.ParseFile(_path) : new TomlDocument(); }
    catch { doc = new TomlDocument(); }

    foreach (var (catId, entryId, _, getter) in _registrations)
    {
        if (!doc.ContainsKey(catId))
            doc[catId] = new TomlTable();
        ((TomlTable)doc[catId])[entryId] = getter();
    }
    File.WriteAllText(_path, doc.SerializedValue);
}
```

위 내용을 모두 반영한 최종 `PreferencesBridge.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using MelonLoader;
using Tomlet;
using Tomlet.Models;

namespace MelonBridge.Bridges
{
    public static class PreferencesBridge
    {
        private static TomlBackend? _backend;

        public static void Attach(string cfgPath)
        {
            _backend = new TomlBackend(cfgPath);
            MelonPreferences.Backend = _backend;
        }

        public static void Detach()
        {
            MelonPreferences.Backend = null;
            _backend = null;
        }

        private sealed class TomlBackend : MelonPreferences.IPreferencesBackend
        {
            private readonly string _path;
            private readonly List<(string catId, string entryId, Action<TomlValue?> setter, Func<TomlValue> getter)> _reg = new();

            public TomlBackend(string path) => _path = path;

            public void RegisterCategory(MelonPreferences_Category category) { }

            public void RegisterEntry<T>(string categoryId, MelonPreferences_Entry<T> entry)
            {
                _reg.Add((
                    categoryId,
                    entry.Identifier,
                    tomlVal =>
                    {
                        if (tomlVal is null) return;
                        try { entry.Value = TomletMain.To<T>(tomlVal); }
                        catch { }
                    },
                    () => TomletMain.ValueFrom(entry.Value)
                ));
            }

            public void Load()
            {
                if (!File.Exists(_path)) return;
                TomlDocument doc;
                try { doc = TomlParser.ParseFile(_path); }
                catch { return; }

                foreach (var (catId, entryId, setter, _) in _reg)
                {
                    if (doc.TryGetValue(catId, out var catVal) &&
                        catVal is TomlTable catTable &&
                        catTable.TryGetValue(entryId, out var entryVal))
                    {
                        setter(entryVal);
                    }
                }
            }

            public void Save()
            {
                TomlDocument doc;
                try { doc = File.Exists(_path) ? TomlParser.ParseFile(_path) : new TomlDocument(); }
                catch { doc = new TomlDocument(); }

                foreach (var (catId, entryId, _, getter) in _reg)
                {
                    if (!doc.ContainsKey(catId))
                        doc[catId] = new TomlTable();
                    ((TomlTable)doc[catId])[entryId] = getter();
                }
                File.WriteAllText(_path, doc.SerializedValue);
            }
        }
    }
}
```

- [ ] **Step 4: 테스트 실행 → PASS 확인**

```
dotnet test MelonLoaderToUMM.Tests/ --filter "PreferencesBridgeTests"
```
Expected: 3 tests PASS

- [ ] **Step 5: 커밋**

```bash
git add MelonBridge/Bridges/PreferencesBridge.cs MelonLoaderToUMM.Tests/PreferencesBridgeTests.cs
git commit -m "feat(bridge): add PreferencesBridge with TOML backend"
```

---

## Task 11: ModLoader

**Files:**
- Create: `MelonBridge/ModLoader.cs`
- Create: `MelonLoaderToUMM.Tests/ModLoaderTests.cs`

ML 모드 DLL을 스캔하고, `MelonMod`/`MelonPlugin` 서브클래스를 찾아 인스턴스화한다.

- [ ] **Step 1: `ModLoaderTests.cs` (failing) 작성**

```csharp
using System;
using System.Reflection;
using NUnit.Framework;
using MelonLoader;
using MelonBridge;

namespace MelonLoaderToUMM.Tests
{
    // 테스트용 더미 모드 (같은 어셈블리 내에 정의)
    [assembly: MelonInfo(typeof(DummyMod), "DummyMod", "1.0.0", "Tester")]

    public class DummyMod : MelonMod
    {
        public bool InitCalled = false;
        public override void OnInitializeMelon() => InitCalled = true;
    }

    [TestFixture]
    public class ModLoaderTests
    {
        [Test]
        public void FindMelonTypes_FindsSubclassesInAssembly()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var types = ModLoader.FindMelonTypes(assembly);
            Assert.IsTrue(types.Count > 0);
        }

        [Test]
        public void CreateInstance_ReturnsMelonBase()
        {
            var instance = ModLoader.CreateInstance(typeof(DummyMod));
            Assert.IsNotNull(instance);
            Assert.IsInstanceOf<DummyMod>(instance);
        }

        [Test]
        public void CreateInstance_SetsInfoFromAttribute()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var instance = ModLoader.CreateInstance(typeof(DummyMod));
            ModLoader.InjectInfo(instance, assembly);

            Assert.IsNotNull(instance.Info);
            Assert.AreEqual("DummyMod", instance.Info.Name);
        }

        [Test]
        public void SortByPriority_HigherPriorityFirst()
        {
            var mods = new System.Collections.Generic.List<MelonBase>
            {
                CreateWithPriority(5),
                CreateWithPriority(0),
                CreateWithPriority(10),
            };

            var sorted = ModLoader.SortByPriority(mods);

            Assert.AreEqual(0, GetPriority(sorted[0]));
            Assert.AreEqual(5, GetPriority(sorted[1]));
            Assert.AreEqual(10, GetPriority(sorted[2]));
        }

        private MelonBase CreateWithPriority(int priority)
        {
            var mod = (DummyMod)ModLoader.CreateInstance(typeof(DummyMod));
            mod.Info = new MelonInfoAttribute(typeof(DummyMod), "mod", "1.0", "a");
            typeof(DummyMod).Assembly
                .GetCustomAttribute<MelonPriorityAttribute>(); // 어셈블리 어트리뷰트 대신
            // priority를 직접 Info에 넣는 대신 별도 필드 사용 테스트
            return mod;
        }

        private int GetPriority(MelonBase mod) => 0; // 우선순위 테스트는 SortByPriority 내부 로직 기반
    }
}
```

실제로는 `SortByPriority`가 `MelonPriorityAttribute`를 어셈블리에서 읽으므로, 같은 어셈블리에서 다른 우선순위를 테스트하기 어렵다. 테스트를 단순화:

```csharp
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using MelonLoader;
using MelonBridge;

namespace MelonLoaderToUMM.Tests
{
    public class DummyMod : MelonMod
    {
        public bool InitCalled;
        public override void OnInitializeMelon() => InitCalled = true;
    }

    [TestFixture]
    public class ModLoaderTests
    {
        [Test]
        public void FindMelonTypes_FindsSubclassesInAssembly()
        {
            var types = ModLoader.FindMelonTypes(Assembly.GetExecutingAssembly());
            Assert.IsTrue(types.Count > 0, "DummyMod이 발견되어야 함");
        }

        [Test]
        public void CreateInstance_ReturnsMelonBase()
        {
            var instance = ModLoader.CreateInstance(typeof(DummyMod));
            Assert.IsInstanceOf<DummyMod>(instance);
        }

        [Test]
        public void InjectInfo_SetsInfoAndLogger()
        {
            var instance = ModLoader.CreateInstance(typeof(DummyMod));
            var attr = new MelonInfoAttribute(typeof(DummyMod), "TestMod", "1.0", "Author");
            ModLoader.InjectInfo(instance, attr);

            Assert.AreEqual("TestMod", instance.Info.Name);
            Assert.IsNotNull(instance.LoggerInstance);
            Assert.AreEqual("TestMod", instance.LoggerInstance.Name);
        }

        [Test]
        public void SortByPriority_SortsAscending()
        {
            var list = new List<(MelonBase mod, int priority)>
            {
                (ModLoader.CreateInstance(typeof(DummyMod)), 10),
                (ModLoader.CreateInstance(typeof(DummyMod)), 0),
                (ModLoader.CreateInstance(typeof(DummyMod)), 5),
            };

            var sorted = ModLoader.SortByPriority(list);

            Assert.AreEqual(0, sorted[0].priority);
            Assert.AreEqual(5, sorted[1].priority);
            Assert.AreEqual(10, sorted[2].priority);
        }
    }
}
```

- [ ] **Step 2: 테스트 실행 → FAIL 확인**

```
dotnet test MelonLoaderToUMM.Tests/ --filter "ModLoaderTests"
```
Expected: compilation error (ModLoader 없음)

- [ ] **Step 3: `ModLoader.cs` 작성**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MelonLoader;

namespace MelonBridge
{
    public static class ModLoader
    {
        public static List<MelonBase> LoadAll(string modsFolder)
        {
            if (!Directory.Exists(modsFolder))
            {
                MelonLogger.Warning($"MelonMods 폴더를 찾을 수 없음: {modsFolder}");
                return new List<MelonBase>();
            }

            var allMods = new List<(MelonBase mod, int priority)>();

            foreach (var dllPath in Directory.GetFiles(modsFolder, "*.dll"))
            {
                try
                {
                    var assembly = Assembly.LoadFrom(dllPath);
                    var infoAttr = assembly.GetCustomAttribute<MelonInfoAttribute>();
                    if (infoAttr == null) continue;

                    if (ShouldSkipForPlatform(assembly)) continue;

                    var types = FindMelonTypes(assembly);
                    foreach (var type in types)
                    {
                        var mod = CreateInstance(type);
                        InjectInfo(mod, infoAttr);
                        var priority = assembly.GetCustomAttribute<MelonPriorityAttribute>()?.Priority ?? 0;
                        allMods.Add((mod, priority));
                    }
                }
                catch (Exception e)
                {
                    MelonLogger.Error($"모드 로드 실패 [{Path.GetFileName(dllPath)}]: {e}");
                }
            }

            // MelonPlugin 먼저, MelonMod 나중; 같은 타입 내에서는 priority 오름차순
            return allMods
                .OrderBy(x => x.mod is MelonPlugin ? 0 : 1)
                .ThenBy(x => x.priority)
                .Select(x => x.mod)
                .ToList();
        }

        public static List<Type> FindMelonTypes(Assembly assembly)
        {
            var result = new List<Type>();
            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsAbstract && (
                    typeof(MelonMod).IsAssignableFrom(type) ||
                    typeof(MelonPlugin).IsAssignableFrom(type)))
                {
                    result.Add(type);
                }
            }
            return result;
        }

        public static MelonBase CreateInstance(Type type)
        {
            return (MelonBase)Activator.CreateInstance(type);
        }

        public static void InjectInfo(MelonBase mod, MelonInfoAttribute info)
        {
            mod.Info = info;
            mod.LoggerInstance = new MelonLogger.Instance(info.Name);
        }

        public static List<(MelonBase mod, int priority)> SortByPriority(
            List<(MelonBase mod, int priority)> mods)
        {
            return mods.OrderBy(x => x.priority).ToList();
        }

        private static bool ShouldSkipForPlatform(Assembly assembly)
        {
            var domainAttr = assembly.GetCustomAttribute<MelonPlatformDomainAttribute>();
            if (domainAttr == null || domainAttr.Domain == MelonPlatformDomain.Any)
                return false;

            // 현재 환경: Mono (IL2CPP 감지는 런타임에 따라 다름)
            bool isIl2Cpp = Type.GetType("Il2CppSystem.Object, Il2CppMscorlib") != null;

            if (domainAttr.Domain == MelonPlatformDomain.Il2Cpp && !isIl2Cpp)
            {
                MelonLogger.Warning($"IL2CPP 전용 모드를 Mono 환경에서 스킵: {assembly.GetName().Name}");
                return true;
            }
            if (domainAttr.Domain == MelonPlatformDomain.Mono && isIl2Cpp)
            {
                MelonLogger.Warning($"Mono 전용 모드를 IL2CPP 환경에서 스킵: {assembly.GetName().Name}");
                return true;
            }
            return false;
        }
    }
}
```

- [ ] **Step 4: 테스트 실행 → PASS 확인**

```
dotnet test MelonLoaderToUMM.Tests/ --filter "ModLoaderTests"
```
Expected: 4 tests PASS

- [ ] **Step 5: 커밋**

```bash
git add MelonBridge/ModLoader.cs MelonLoaderToUMM.Tests/ModLoaderTests.cs
git commit -m "feat(bridge): add ModLoader with assembly scanning and priority sorting"
```

---

## Task 12: SceneBridge & MelonCoroutineRunner

**Files:**
- Create: `MelonBridge/Bridges/SceneBridge.cs`
- Create: `MelonBridge/Bridges/MelonCoroutineRunner.cs`

이 두 컴포넌트는 Unity API에 의존하므로 단위 테스트 없이 구현한다. 인게임 테스트로 검증.

- [ ] **Step 1: `MelonCoroutineRunner.cs` 작성**

```csharp
using UnityEngine;

namespace MelonBridge.Bridges
{
    public sealed class MelonCoroutineRunner : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            MelonLoader.MelonCoroutines.Runner = this;
        }
    }
}
```

- [ ] **Step 2: `SceneBridge.cs` 작성**

```csharp
using System;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine.SceneManagement;

namespace MelonBridge.Bridges
{
    public static class SceneBridge
    {
        private static List<MelonBase> _mods = new();

        public static void Attach(List<MelonBase> mods)
        {
            _mods = mods;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        public static void Detach()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            _mods.Clear();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            foreach (var mod in _mods)
            {
                try { mod.OnSceneWasLoaded(scene.buildIndex, scene.name); }
                catch (Exception e) { MelonLogger.Error($"[{mod.Info?.Name}] OnSceneWasLoaded: {e}"); }

                try { mod.OnSceneWasInitialized(scene.buildIndex, scene.name); }
                catch (Exception e) { MelonLogger.Error($"[{mod.Info?.Name}] OnSceneWasInitialized: {e}"); }
            }
        }

        private static void OnSceneUnloaded(Scene scene)
        {
            foreach (var mod in _mods)
            {
                try { mod.OnSceneWasUnloaded(scene.buildIndex, scene.name); }
                catch (Exception e) { MelonLogger.Error($"[{mod.Info?.Name}] OnSceneWasUnloaded: {e}"); }
            }
        }
    }
}
```

- [ ] **Step 3: 빌드 확인**

```
dotnet build MelonBridge/MelonBridge.csproj
```
Expected: SUCCEED

- [ ] **Step 4: 커밋**

```bash
git add MelonBridge/Bridges/SceneBridge.cs MelonBridge/Bridges/MelonCoroutineRunner.cs
git commit -m "feat(bridge): add SceneBridge and MelonCoroutineRunner"
```

---

## Task 13: Main.cs (UMM 진입점)

**Files:**
- Create: `MelonBridge/Main.cs`
- Create: `MelonBridge/Info.json`

UMM의 `ModEntry`를 받아 모든 브릿지와 ModLoader를 연결한다.

- [ ] **Step 1: `Info.json` 작성**

```json
{
  "Id": "MelonBridge",
  "Version": "1.0.0",
  "DisplayName": "MelonLoader Bridge",
  "Author": "MelonLoaderToUMM",
  "EntryMethod": "MelonBridge.Main.Load",
  "HomePage": "",
  "Repository": ""
}
```

- [ ] **Step 2: `Main.cs` 작성**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using MelonLoader;
using MelonBridge.Bridges;
using UnityEngine;
using UnityModManagerNet;

namespace MelonBridge
{
    public static class Main
    {
        public static UnityModManager.ModEntry ModEntry { get; private set; }
        private static List<MelonBase> _mods = new();

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModEntry = modEntry;

            // 1. Logger 연결
            LoggerBridge.Attach(new UmmLoggerAdapter(modEntry.Logger));

            // 2. Preferences 연결
            var userDataPath = Path.Combine(
                Path.GetDirectoryName(modEntry.Path), "..", "..", "UserData");
            var cfgPath = Path.Combine(userDataPath, "MelonPreferences.cfg");
            PreferencesBridge.Attach(cfgPath);

            // 3. 모드 로드
            var modsFolder = Path.Combine(
                Path.GetDirectoryName(modEntry.Path), "..", "..", "MelonMods");
            _mods = ModLoader.LoadAll(modsFolder);

            // 4. MelonCoroutineRunner 생성
            var runnerGo = new GameObject("MelonCoroutineRunner");
            runnerGo.AddComponent<MelonCoroutineRunner>();

            // 5. SceneBridge 연결
            SceneBridge.Attach(_mods);

            // 6. UMM 콜백 등록
            modEntry.OnUpdate = OnUpdate;
            modEntry.OnFixedUpdate = OnFixedUpdate;
            modEntry.OnLateUpdate = OnLateUpdate;
            modEntry.OnGUI = OnGUI;
            modEntry.OnToggle = OnToggle;

            // 7. 초기화 콜백 호출
            InvokeAll(m => m.OnEarlyInitializeMelon(), "OnEarlyInitializeMelon");
            InvokeAll(m => m.OnInitializeMelon(), "OnInitializeMelon");
            InvokeAll(m => m.OnApplicationStart(), "OnApplicationStart");
            InvokeAll(m => m.OnApplicationLateStart(), "OnApplicationLateStart");

            MelonLogger.Msg($"MelonBridge: {_mods.Count}개 모드 로드 완료.");
            return true;
        }

        private static void OnUpdate(UnityModManager.ModEntry entry, float dt)
        {
            InvokeAll(m => m.OnUpdate(), "OnUpdate");
        }

        private static void OnFixedUpdate(UnityModManager.ModEntry entry, float dt)
        {
            InvokeAll(m => m.OnFixedUpdate(), "OnFixedUpdate");
        }

        private static void OnLateUpdate(UnityModManager.ModEntry entry, float dt)
        {
            InvokeAll(m => m.OnLateUpdate(), "OnLateUpdate");
        }

        private static void OnGUI(UnityModManager.ModEntry entry)
        {
            InvokeAll(m => m.OnGUI(), "OnGUI");
        }

        private static bool OnToggle(UnityModManager.ModEntry entry, bool active)
        {
            if (!active)
            {
                InvokeAll(m => m.OnDeinitializeMelon(), "OnDeinitializeMelon");
                InvokeAll(m => m.OnApplicationQuit(), "OnApplicationQuit");
                SceneBridge.Detach();
            }
            return true;
        }

        private static void InvokeAll(Action<MelonBase> action, string callbackName)
        {
            foreach (var mod in _mods)
            {
                try { action(mod); }
                catch (Exception e)
                {
                    MelonLogger.Error($"[{mod.Info?.Name ?? "?"}] {callbackName}: {e}");
                }
            }
        }

        private sealed class UmmLoggerAdapter : LoggerBridge.IUmmLogger
        {
            private readonly UnityModManager.ModEntry.ModLogger _logger;
            public UmmLoggerAdapter(UnityModManager.ModEntry.ModLogger logger) => _logger = logger;
            public void Log(string msg) => _logger.Log(msg);
            public void Warning(string msg) => _logger.Warning(msg);
            public void Error(string msg) => _logger.Error(msg);
        }
    }
}
```

- [ ] **Step 3: 전체 빌드 확인**

```
dotnet build MelonLoaderToUMM.sln
```
Expected: SUCCEED (0 errors)

- [ ] **Step 4: 모든 테스트 실행**

```
dotnet test MelonLoaderToUMM.Tests/
```
Expected: 모든 테스트 PASS

- [ ] **Step 5: 커밋**

```bash
git add MelonBridge/Main.cs MelonBridge/Info.json
git commit -m "feat(bridge): add UMM entry point Main.cs wiring all bridges"
```

---

## Task 14: 빌드 출력 검증 & Info.json 배포 설정

**Files:**
- Modify: `MelonBridge/MelonBridge.csproj` (빌드 후 Info.json 복사)

- [ ] **Step 1: `MelonBridge.csproj`에 Info.json 출력 포함 설정 추가**

```xml
<!-- MelonBridge.csproj의 <ItemGroup> 내에 추가 -->
<ItemGroup>
  <None Include="Info.json">
    <CopyToOutputDirectory>Always</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

- [ ] **Step 2: Release 빌드 수행**

```
dotnet build MelonLoaderToUMM.sln -c Release
```
Expected: SUCCEED

- [ ] **Step 3: 출력 파일 확인**

```
dir MelonBridge\bin\Release\net472\
```
Expected 목록:
- `MelonBridge.dll`
- `MelonLoader.dll` (스텁)
- `Info.json`
- `Tomlet.dll`

- [ ] **Step 4: 게임에 배포하는 방법 검증**

`MelonBridge/bin/Release/net472/` 폴더 전체를 게임의 `UnityModManager/MelonBridge/`에 복사.
추가로 번들할 서드파티 DLL은 NuGet 캐시 또는 직접 수집해 같은 폴더에 배치:
- `0Harmony.dll` (HarmonyX — NuGet 패키지 `Lib.Harmony` 내 `net472` 폴더)
- `Il2CppInterop.Runtime.dll`, `Il2CppInterop.Common.dll`
- `UnhollowerRuntimeLib.dll`, `UnhollowerBaseLib.dll`
- `Semver.dll`
- `Mono.Cecil.dll`

- [ ] **Step 5: 최종 커밋**

```bash
git add MelonBridge/MelonBridge.csproj
git commit -m "chore: include Info.json in build output"
```
