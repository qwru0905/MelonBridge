# Third-Party Notices

MelonBridge bundles the following third-party libraries in its release output
(`dist/MelonBridge/`, deployed to `UnityModManager/Mods/MelonBridge/`). All are
MIT-licensed; full license texts are available at the linked upstream repositories.

| Bundled file(s) | Upstream project | License |
|---|---|---|
| `0Harmony.Melon.dll` | [HarmonyX](https://github.com/BepInEx/HarmonyX) (Lib.Harmony) | MIT |
| `MonoMod.Utils.Melon.dll`, `MonoMod.RuntimeDetour.Melon.dll` | [MonoMod](https://github.com/MonoMod/MonoMod) | MIT |
| `Mono.Cecil.dll` | [Mono.Cecil](https://github.com/jbevain/cecil) | MIT |
| `Tomlet.dll` | [Tomlet](https://github.com/SamboyCoding/Tomlet) | MIT |

## Note on the `*.Melon` copies

`0Harmony.Melon.dll`, `MonoMod.Utils.Melon.dll`, and `MonoMod.RuntimeDetour.Melon.dll`
are **renamed (shaded)** copies of HarmonyX / MonoMod, produced by `tools/AssemblyShader`
(a Mono.Cecil-based rewriter included in this repository). Only the assembly identity
(simple name) was changed — the implementation is unmodified — to avoid runtime
collisions with the Harmony/MonoMod copies UnityModManager bundles for its own mods.
See `docs/superpowers/plans/2026-06-06-melon-loader-to-umm.md` for the rationale.
