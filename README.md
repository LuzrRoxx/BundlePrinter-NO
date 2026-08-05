Standalone fork from https://github.com/Dev1-bit/Blueprinter-NO, meant to be more drop-in and more documented.

---

## About this repository

Reconstructed C# source for the **Blueprinter** BepInEx plugin (`com.nikkorap.blueprinter`) for *Nuclear Option*, recovered from the shipped assembly. The tree builds cleanly (`dotnet build -c Release`).

- `com.nikkorap.blueprinter/` — the reconstructed project (`.csproj` + sources under `Blueprinter/`, `Blueprinter/Ops/`, `MiniJSON/`).
- `solution.sln` — solution file.

**Building:** targets `netstandard2.1`. Reference assemblies resolve from a local *Nuclear Option* install (`NuclearOption_Data/Managed` and `BepInEx/core`); adjust the `<HintPath>` entries and the `GameManaged` / `BepInExCore` properties in the `.csproj` to your install path. Private/internal game members are accessed via a publicizer (the project uses `Krafs.Publicizer` on `Assembly-CSharp` and `UnityEngine.CoreModule`).

---

# Blueprinter Modding Guide (Nuclear Option)

A practical, source-accurate guide to building mods with **Blueprinter** (`com.nikkorap.blueprinter`) for *Nuclear Option*. Everything here is derived from the reconstructed plugin source.

- **Plugin ID:** `com.nikkorap.blueprinter`
- **Type:** BepInEx plugin (managed .NET, Harmony patches)
- **Game engine:** Unity **2022.3.62f2** (bundles must be built with this exact version)
- **Manifest schema version:** `3`

---

## 1. What Blueprinter is (and the core idea)

Blueprinter is a **content loader**. It lets you add aircraft, weapons, missions, liveries, loading screens and encyclopedia entries to the game **without shipping or recompiling the base game**.

The key trick is that a Blueprinter mod does **not** carry copies of the game's shaders, materials, meshes, audio or weapon data. Instead your bundle ships **placeholder assets**, and a JSON **manifest** tells Blueprinter to *rewire* those placeholders to the **real base-game assets** at load time. This keeps mods copyright-clean and comparatively small.

Two things happen at load:

1. **Patches** — inject resolved base-game assets (a shader, a material, a mesh, an audio clip, a weapon `WeaponInfo`, …) into slots on *your* bundle's prefabs/materials.
2. **Ops** — high-level registrations: add your aircraft to a hangar, register weapon mounts in the encyclopedia, add missions, add loading screens, etc.

Plus **Addressable overrides** to replace base-game addressable assets (e.g. liveries) by GUID.

---

## 2. Load lifecycle (what the plugin actually does)

From the reconstructed `Plugin` / `PatchRunner` / `BundleRegistry`:

1. `Plugin.Awake()` — force-hides the BepInEx manager object, creates the `BundleRegistry`, registers the post-op handlers, sets up the addressable override system, then `Harmony.PatchAll()`.
2. `RunRoutine()` (coroutine) runs the pipeline:
   - `BlueprinterLoadingScreen.Create()`.
   - `BundleRegistry.ScanAndLoad(pluginLocation)` — recursively scans **`BepInEx/plugins`** for **`*.nobp`** files, ordered **newest-first** (by last-write, then creation time). Each `.nobp` is a Unity AssetBundle; it loads the `patch_manifest` TextAsset out of it.
   - Computes `BundlesHash` (a signature of loaded bundles), assigns prefab hashes.
   - `PatchRunner.ApplyAllPatchesCoroutine()` — applies every manifest's **Patches** (yields periodically so the game doesn't freeze; reports progress to the loading screen).
   - `PatchRunner.ApplyAllOps()` — runs every manifest's **Ops** through the registered handlers.
   - `RegisterAddressableOverrides()` — installs the **Addressables** overrides into Unity's Addressables system.
   - Logs `"Done."` and tears the loading screen down.

Registered op handlers (opIds you can use):

| opId | Purpose |
|---|---|
| `OpAddToHangar` | Add a bundle aircraft (`AircraftDefinition`) to hangar(s) |
| `OpFindAircraftToHangar` | Add an **existing base-game** aircraft to a hangar by name |
| `OpAddToEncyclopedia` | Register assets (e.g. `WeaponMount`) into the encyclopedia |
| `OpAddMissions` | Add mission JSON `TextAsset`s to mission groups |
| `OpAddLoadingScreens` | Add loading-screen `Sprite`s |
| `OpAddWeaponMountToWeaponManager` | Attach weapon mounts to weapon managers |

---

## 3. Two kinds of mod

Blueprinter add-ons live under:

```
BepInEx/plugins/com.nikkorap.blueprinter/addons/<addon-id>/
```

**a) Data-only add-on** — a single `.nobp` bundle + `meta.json`. No code. This is the normal way to ship an aircraft.

**b) DLL add-on** — a BepInEx `.dll` + `meta.json` (optionally its own `.nobp`). Used when the mod needs custom runtime code.

> Note: `ScanAndLoad` discovers bundles by walking `BepInEx/plugins` for `*.nobp`, so the exact subfolder is a convention for organization; the loader finds the bundle regardless. `meta.json` is registry/metadata used by the add-on manager, not strictly required for the loader to apply a bundle.

---

## 4. `meta.json` (add-on metadata)

```json
{
    "id": "yourname.myaircraft",
    "artifact": {
        "fileName": "MyAircraft_1.0.0.nobp",
        "version": "1.0.0",
        "category": "release",
        "type": "addon",
        "gameVersion": "0.33",
        "downloadUrl": "https://github.com/you/repo/releases/download/1.0.0/MyAircraft_1.0.0.nobp",
        "hash": "sha256:<sha256 of the .nobp>",
        "extends": { "id": "com.nikkorap.blueprinter", "version": "1.8.19" },
        "dependencies": [],
        "incompatibilities": []
    }
}
```

- `type`: `"addon"` for a bundle mod, `"plugin"` for the base Blueprinter itself.
- `extends`: the Blueprinter version your mod targets.
- `hash`: `sha256:` of the artifact file.

---

## 5. The `.nobp` bundle

A `.nobp` is just a **Unity AssetBundle** (`UnityFS` container) renamed to `.nobp`, built with **Unity 2022.3.62f2**. Inside it you place:

- One `TextAsset` named **`patch_manifest`** (the JSON described below).
- Your aircraft **prefab**, its **`AircraftDefinition`** ScriptableObject, **placeholder materials/meshes**, weapon `WeaponMount`/`WeaponInfo` assets, liveries, mission JSON, loading-screen sprites, etc.

Assets are addressed by their **project asset path** ("locator"), e.g.
`Assets/Blueprinter/Blueprint Bundles/P_MyAircraft/P_MyAircraft_definition.asset`.
At runtime Blueprinter resolves them with `AssetBundle.LoadAsset(locator, type)` (falling back to `LoadAsset(locator)`), so the locator strings in the manifest **must match the asset paths packed into the bundle**.

---

## 6. `patch_manifest` schema

Top-level object (reconstructed `PatchManifest`):

```jsonc
{
  "modName": "My Aircraft",
  "schemaVersion": 3,
  "modVersion": "1.0.0",
  "Patches": [ /* AssetPatch[] */ ],
  "Ops": [ /* Op[] */ ],
  "Addressables": [ /* AddressableOverride[] */ ]
}
```

### 6.1 `AssetRef` — how any asset is named

```jsonc
{
  "locator": "Assets/.../P_MyAircraft_definition.asset", // asset path in bundle, OR an addressable key/name for base-game assets
  "name":    "P_MyAircraft_definition",                  // fallback name
  "type":    "AircraftDefinition, Assembly-CSharp"       // "<Type>, <Assembly>"
}
```

Resolution rules (from `ResourcesAssetResolver`):
- **Bundle asset** (`ResolveBundleAsset`): tries `AssetBundle.LoadAsset(locator, type)`, else `LoadAsset(locator)`. Use `locator` = the packed asset path.
- **Base-game asset** (`ResolveGameAsset`): looks the asset up by `name`/`locator` (addressable key or object name) and `type`. This is how you reference the game's own shaders/materials/meshes/audio without shipping them.

Common `type` strings you'll use:
`AircraftDefinition, Assembly-CSharp`, `WeaponMount, Assembly-CSharp`, `WeaponInfo, Assembly-CSharp`, `LiveryData, Assembly-CSharp`, `Faction, Assembly-CSharp`,
`UnityEngine.Shader, UnityEngine.CoreModule`, `UnityEngine.Material, UnityEngine.CoreModule`, `UnityEngine.Mesh, UnityEngine.CoreModule`, `UnityEngine.GameObject, UnityEngine.CoreModule`, `UnityEngine.Sprite, UnityEngine.CoreModule`, `UnityEngine.TextAsset, UnityEngine.CoreModule`, `UnityEngine.AudioClip, UnityEngine.CoreModule`.

### 6.2 `LocationRef` — a precise target/source location

```jsonc
{
  "id": "human-readable id",              // free-form label used in logs; for GameAsset it's "<key>|<Type>"
  "asset": { /* AssetRef */ },            // which asset (bundle or game)
  "hierarchyPath": "wing_light_L",        // optional: child transform path under a prefab
  "componentType": "UnityEngine.MeshRenderer, UnityEngine.CoreModule", // optional
  "componentIndex": 0,                    // optional: which component of that type
  "memberPath": "sharedMaterials[0]"      // optional: field/property to write
}
```

### 6.3 `AssetPatch` — inject a base-game asset into your bundle

```jsonc
{
  "GameAsset": { /* LocationRef -> resolves to a BASE-GAME asset */ },
  "PatchLocations": [ /* LocationRef[] -> slots IN YOUR BUNDLE to receive it */ ]
}
```

Meaning: *resolve `GameAsset` from the base game, then write it into each `PatchLocation`.* This is the mechanism that gives your placeholder materials the real aircraft skin shader, gives your renderers real base-game meshes/materials, wires audio, weapon `info`, etc.

**`memberPath` grammar** (handled by `PatchRunner` / `MemberPathSetter`):

| memberPath | Effect |
|---|---|
| `sharedMaterials[i]` / `materials[i]` | Set slot `i` of a `Renderer`'s (shared)material array |
| `material` | Set a single material reference |
| `sharedMesh` | Set a `MeshFilter`/collider mesh |
| `rendererIndex` / `m_RendererIndex` | Special URP renderer-index resolve (matches game asset in the pipeline renderer list) |
| `outputAudioMixerGroup::GroupName` | Find an `AudioMixerGroup` by name via `FindMatchingGroups` and assign it |
| `hardpointSets`, `joints`, `info`, `clip`, `hitSound`, `m_FontData.m_Font`, … | Generic reflective set (supports nested `.` and `[index]`) |

`hierarchyPath` + `componentType` + `componentIndex` pick *which* object/component under the target prefab receives the value; `memberPath` picks the field.

**Example** — assign a base-game material to a specific renderer inside a bundle model:

```jsonc
{
  "GameAsset": {
    "id": "MyLightMaterial|UnityEngine.Material, UnityEngine.CoreModule",
    "asset": { "locator": "MyLightMaterial", "name": "MyLightMaterial",
               "type": "UnityEngine.Material, UnityEngine.CoreModule" }
  },
  "PatchLocations": [{
    "id": "P_MyAircraft_lights/wing_light_L/MeshRenderer#0",
    "asset": { "locator": "Assets/.../Models/P_MyAircraft_lights.fbx",
               "name": "P_MyAircraft_lights",
               "type": "UnityEngine.GameObject, UnityEngine.CoreModule" },
    "hierarchyPath": "wing_light_L",
    "componentType": "UnityEngine.MeshRenderer, UnityEngine.CoreModule",
    "componentIndex": 0,
    "memberPath": "sharedMaterials[0]"
  }]
}
```

**Skin/shader example** — bind the game's aircraft shader onto your skin material:

```jsonc
{
  "GameAsset": {
    "id": "Shader Graphs/AircraftSkin|UnityEngine.Shader, UnityEngine.CoreModule",
    "asset": { "locator": "Shader Graphs/AircraftSkin", "name": "Shader Graphs/AircraftSkin",
               "type": "UnityEngine.Shader, UnityEngine.CoreModule" }
  },
  "PatchLocations": [{
    "id": "P_MyAircraft_skin",
    "asset": { "locator": "Assets/.../Materials/P_MyAircraft_skin.mat", "name": "P_MyAircraft_skin",
               "type": "UnityEngine.Material, UnityEngine.CoreModule" },
    "memberPath": "shader"
  }]
}
```

> In practice most patches are generated by Blueprinter's Unity tooling when you author the aircraft, not hand-written. A full aircraft commonly has 150–250 patches covering materials, meshes, audio, fonts, weapon infos and mounts.

### 6.4 `Op` — high-level actions

```jsonc
{ "opId": "OpAddToHangar", "payloadJson": "{...}" }
```

`payloadJson` is a **JSON string** (escaped) whose shape depends on `opId`:

**`OpAddToHangar`** — add your aircraft to hangar(s):
```jsonc
{
  "BundleAsset": { "locator": "Assets/.../P_MyAircraft_definition.asset", "name": "P_MyAircraft_definition",
                   "type": "AircraftDefinition, Assembly-CSharp" },
  "Hangars": ["hangar_med__hangar_med"]
}
```

**`OpFindAircraftToHangar`** — add an existing base-game aircraft:
```jsonc
{ "HangarKey": "hangar_med__hangar_med", "AircraftNames": ["<BaseAircraftName>"] }
```

**`OpAddToEncyclopedia`** — register weapon mounts (etc.):
```jsonc
{ "entries": [ { "locator": "Assets/.../MyWeaponMount.asset", "name": "MyWeaponMount",
                 "type": "WeaponMount, Assembly-CSharp" } ] }
```

**`OpAddMissions`** — add missions to groups:
```jsonc
{ "MissionAssets": [ { "locator": "Assets/.../Missions/MyMission.json", "name": "MyMission",
                       "type": "UnityEngine.TextAsset, UnityEngine.CoreModule" } ],
  "MissionGroups": ["Tutorials"] }
```

**`OpAddLoadingScreens`**:
```jsonc
{ "imagesAssets": [ { "locator": "Assets/.../LoadingScreens/myscreen.png", "name": "myscreen",
                      "type": "UnityEngine.Sprite, UnityEngine.CoreModule" } ] }
```

**`OpAddWeaponMountToWeaponManager`** — payload: `{ "bundleAsset": AssetRef, "weaponManagers": WeaponManagerTarget[] }`.

### 6.5 `AddressableOverride` — replace base-game addressables by GUID

```jsonc
{
  "guid": "<32-hex-guid-of-base-game-asset>",
  "subObjectName": "",
  "subObjectType": "",
  "BundleAsset": { "locator": "Assets/.../Liveries/P_MyAircraft_livery_gray.asset",
                   "name": "P_MyAircraft_livery_gray", "type": "LiveryData, Assembly-CSharp" }
}
```

At load these get inserted into Unity's Addressables so requests for the base GUID return your bundle asset instead. Commonly used for liveries.

---

## 7. End-to-end: authoring an aircraft mod

You need **Unity 2022.3.62f2** and Blueprinter's aircraft SDK/template project (the `Assets/Blueprinter/Blueprint Bundles/...` layout). High-level steps:

1. **Model** — in your 3D tool, finalize the mesh, name child objects meaningfully (they become `hierarchyPath`s), set up UVs. Export **FBX** (Y-up, apply scale). Bring in your textures (diffuse / spec / etc.).
2. **Unity import** — drop the FBX + textures into `Assets/Blueprinter/Blueprint Bundles/P_MyAircraft/`. Create placeholder **materials** (`P_MyAircraft_skin.mat`, light materials, …). Shaders/meshes that belong to the base game stay as references resolved by patches.
3. **Prefab** — build the aircraft prefab: engines, control surfaces, colliders, hardpoints/weapon mounts, cockpit, lights. Wire game components (`Aircraft`, weapon managers, etc.). Anything base-game-owned becomes a **patch target**.
4. **`AircraftDefinition`** — create the ScriptableObject (`P_MyAircraft_definition.asset`): flight model, mass, thrust, HP, hardpoint sets, faction, hangar sizing, etc. This is what `OpAddToHangar` points at.
5. **Weapons/liveries/missions** (optional) — author `WeaponMount`/`WeaponInfo` assets, `LiveryData`, mission JSON, loading-screen sprites.
6. **Manifest** — produce `patch_manifest` (`TextAsset`) with `Patches` (rewire placeholders → base-game shaders/materials/meshes/audio) and `Ops` (`OpAddToHangar`, `OpAddToEncyclopedia`, …) and any `Addressables`. Blueprinter's Unity tooling generates most `Patches` automatically from the scene references.
7. **Build the bundle** — assign every asset (including `patch_manifest`) to one AssetBundle, build for Standalone with the same Unity version, then rename the output to `MyAircraft_1.0.0.nobp`.
8. **Package** — put `MyAircraft_1.0.0.nobp` + `meta.json` in `BepInEx/plugins/com.nikkorap.blueprinter/addons/yourname.myaircraft/`.
9. **Test** — launch the game; watch the BepInEx console for Blueprinter logs (`PatchRunner: Applying patches from bundle 'My Aircraft'...`, bundle report, `Done.`).

### Minimal Unity AssetBundle build script

```csharp
// Assets/Editor/BuildBlueprint.cs
using UnityEditor;
using System.IO;

public static class BuildBlueprint
{
    [MenuItem("Blueprinter/Build .nobp")]
    public static void Build()
    {
        string outDir = "BuiltBundles";
        Directory.CreateDirectory(outDir);
        BuildPipeline.BuildAssetBundles(
            outDir,
            BuildAssetBundleOptions.ChunkBasedCompression,
            BuildTarget.StandaloneWindows64);
        // Then rename BuiltBundles/<bundlename> -> MyAircraft_1.0.0.nobp
    }
}
```

Assign the bundle name on each asset (Inspector → bottom **AssetBundle** dropdown), including the `patch_manifest` TextAsset. The paths you assign are exactly the `locator`s the manifest must use.

---

## 8. How resolution/patching can fail (troubleshooting)

Watch the BepInEx log. The plugin emits specific warnings you can grep:

- `ResolveBundleAsset: ... has no locator/name.` → an `AssetRef` in the manifest is missing `locator`/`name`.
- `PatchRunner: patch '<id>' could not resolve base-game source asset. Skipping its locations.` → the `GameAsset` name/type didn't match any base-game asset (wrong key or wrong `type`).
- `PatchRunner: location '<id>' ... could not resolve bundle target asset.` → the `PatchLocation.asset.locator` doesn't exist in your bundle (locator/name mismatch with what you packed).
- `PatchRunner: location '<id>' ... could not resolve target object.` → `hierarchyPath`/`componentType`/`componentIndex` didn't find the object/component.
- `PatchRunner: ... material index N out of range` → a `sharedMaterials[i]`/`materials[i]` index exceeds the renderer's array length.
- `PatchRunner: Bundle report: <mod> v <ver>` with `X/Y patches applied` — if `X < Y`, some patches failed; the line is logged as a **warning** (all-applied is logged as info).
- `no handler registered for op '<opId>'` → wrong/misspelled `opId`.

General rule: **`locator` strings are the contract**. Bundle-side locators must equal the asset paths you packed; game-side names/keys must equal real base-game addressable keys / object names, with the correct `type`.

---

## 9. What a full aircraft typically rewires

A complete aircraft leans heavily on Patches so the bundle only ships original geometry, textures and definitions while pulling everything else from the base game. Typical patch targets:

- **Renderer materials** (`sharedMaterials[i]` / `material`) and **meshes** (`sharedMesh`) bound to the game's shaders and shared assets.
- **Weapon data** — `WeaponInfo` via `info`, plus `WeaponMount` assets registered through ops.
- **Audio** — `AudioClip`s (`clip`, `hitSound`, `deploySound`, …) and `AudioMixerGroup` routing (`outputAudioMixerGroup::<Group>`).
- **UI/fonts** — `m_FontData.m_Font` on text components.
- **Physics** — colliders/joints and physic materials.
- **Rendering** — URP renderer indices, render textures, etc.

Ops then register the aircraft into hangars/encyclopedia, and Addressables override liveries.

---

## 10. Quick checklist

- [ ] Unity **2022.3.62f2** + Blueprinter aircraft SDK/template.
- [ ] FBX + textures imported under `Assets/.../P_MyAircraft/`.
- [ ] Prefab wired; base-game-owned refs left as patch targets.
- [ ] `AircraftDefinition` asset created.
- [ ] `patch_manifest` TextAsset with correct `Patches` + `Ops` (+ `Addressables`).
- [ ] All assets + manifest assigned to one AssetBundle; locators match manifest.
- [ ] Build → rename to `<Name>_<ver>.nobp`.
- [ ] `meta.json` with correct `hash`/`extends`.
- [ ] Drop in `BepInEx/plugins/com.nikkorap.blueprinter/addons/<id>/`.
- [ ] Launch, read Blueprinter log, confirm `Done.` and aircraft in hangar.
