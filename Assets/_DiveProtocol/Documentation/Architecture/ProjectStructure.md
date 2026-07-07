# DiveProtocol Project Structure

## Main directories

- `Art`: characters, environments, models, textures, materials, animation, VFX, and concept art.
- `Audio`: ambience, sound effects, music, broadcasts, and audio mixers.
- `Code`: runtime code, editor-only tools, and automated tests.
- `Data`: authored configuration and static definitions for rules, items, enemies, levels, scoring, and progression.
- `Prefabs`: reusable Unity objects grouped by gameplay responsibility.
- `Scenes`: system, level, development, and template scenes.
- `Rendering`: URP renderer assets, shaders, render textures, volume profiles, and lighting settings.
- `Settings`: project-owned input, physics, tags and layers, and localization assets.
- `UI`: fonts, icons, sprites, HUD, menus, build selection, advice, and results presentation.
- `Documentation`: architecture, level design, naming rules, and data sheets.

## Assembly dependency direction

`DiveProtocol.Runtime` is the base gameplay assembly. `DiveProtocol.Editor`, `DiveProtocol.Tests.EditMode`, and `DiveProtocol.Tests.PlayMode` may reference Runtime. Runtime must never reference Editor or either test assembly. PlayMode tests must not reference the Editor assembly.

Runtime code cannot reference `UnityEditor` because it must remain available to Windows player builds. Editor APIs are excluded from players; introducing that dependency would make runtime compilation or builds fail and would couple gameplay code to authoring tools.

## Data ownership

- Static authored data belongs under `Assets/_DiveProtocol/Data`, typically as version-controlled ScriptableObject assets.
- Current-run state belongs to runtime-owned plain C# state or runtime components; it must not be written back into ScriptableObject assets.
- Permanent save data belongs in a future dedicated persistence system and is written to the platform-appropriate persistent data location, not into project assets.

## Asset boundaries

Third-party assets and source code must live under `Assets/ThirdParty` or their package-owned location, never under `Assets/_DiveProtocol`.

Future scenes and Prefabs should be created by purpose-built Unity Editor tools or manually by the user in Unity. Their YAML files must not be authored or batch-edited directly outside Unity.
