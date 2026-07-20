# Unity Project Context

<!-- unity-onboarding:generated:start -->

## Project Summary

- Project root: `/Users/daniilsvedun/ProjectPollora`
- Small first-person horror gameplay prototype centred on hiding from Pollora, closing the player's eyes during inspection, and managing stress.
- Last analyzed: 2026-07-20
- Last analyzed commit: unknown (local Git is unavailable because Apple command-line developer tools are not installed).

## Confirmed Environment

- Unity version: 6000.4.11f1 (revision `b0a1d6caadd2`).
- Render pipeline: Universal Render Pipeline 17.4.0; a custom render-pipeline asset is assigned in Graphics Settings.
- Input system: both (`activeInputHandler: 2`), but first-party gameplay code uses the legacy `UnityEngine.Input` API.
- Target platforms: not explicitly documented. Standalone, Android, and iPhone settings exist; this does not confirm supported release targets.

## Important Packages And Frameworks

| Area | Finding | Confidence | Evidence |
| --- | --- | --- | --- |
| Rendering | URP 17.4.0 | Confirmed | `Packages/manifest.json`, `ProjectSettings/GraphicsSettings.asset` |
| Input | Input System package 1.19.0 plus legacy input usage | Confirmed | `Packages/manifest.json`, `ProjectSettings/ProjectSettings.asset`, `Assets/Scripts/Player/PlayerController.cs` |
| UI | uGUI and TextMesh Pro | Confirmed | package manifest and player/game-over UI scripts |
| Navigation | AI Navigation 2.0.13; Pollora uses `NavMeshAgent` and the test scene builds its `NavMeshSurface` once at runtime | Confirmed | package manifest, `PolloraController.cs`, `Test_Hiding_v01.unity` |
| Multiplayer | Multiplayer Center is installed, but no first-party multiplayer implementation was found | Confirmed | package manifest and first-party code search |
| Async/events | Unity coroutines and one static C# event (`PlayerStress.OnPlayerScreamed`) | Confirmed | first-party scripts |

## Directory Structure

| Path | Purpose | Confidence | Evidence |
| --- | --- | --- | --- |
| `Assets/Scripts/Player` | Movement, interaction, hiding, eyes, stress, detection, and player audio | Confirmed | source files |
| `Assets/Scripts/Pollora` | Enemy sequence, movement, footsteps, and breathing audio | Confirmed | source files |
| `Assets/Scenes` | One first-party test/gameplay scene | Confirmed | `Test_Hiding_v01.unity` |
| `Assets/Audio`, `Materials`, `Models`, `Prefabs`, `UI` | First-party content grouped by asset type | Likely | directory names and scene references |
| `Assets/TextMesh Pro`, `Assets/TutorialInfo` | Imported Unity/template content | Confirmed | contents |

## Assembly Boundaries

- No first-party `.asmdef` or `.asmref` files exist. Runtime scripts compile into the default `Assembly-CSharp` assembly.
- Scripts use the global namespace; editor-only imported tutorial code is kept under an `Editor` folder.

## Scenes And Startup Flow

- Enabled build scenes: `Assets/Scenes/Test_Hiding_v01.unity` only.
- Startup scene: `Test_Hiding_v01` (confirmed by Editor Build Settings).
- The scene contains the Player, Pollora, GameManager, three wardrobe hiding spots, two explicit NavMesh test obstacles, UI, and Pollora start/fallback-inspect/leave points. Pollora has serialized references to all three hiding spots.
- No scene-loading code was found; gameplay and respawn occur within the same scene.

## Architecture

| Pattern | Finding | Confidence | Evidence |
| --- | --- | --- | --- |
| Scene-composed MonoBehaviours | Behaviour is divided into small components connected through serialized references or sibling `GetComponent` lookups | Confirmed | scene YAML and first-party scripts |
| Event-driven enemy reaction | Stress overload raises a static scream event consumed by Pollora and scream audio | Confirmed | `PlayerStress`, `PolloraController`, `PlayerScreamAudio` |
| Coroutine sequences | Game-over UI/respawn and Pollora movement/inspection are coroutine-driven | Confirmed | `GameOverManager`, `GameOverUI`, `PolloraController` |
| Global manager | `GameOverManager` is a scene singleton | Confirmed | `GameOverManager.cs` |
| Enemy state machine | `PolloraState` explicitly represents waiting, approaching, inspecting, leaving, and scream-response states; one owned coroutine is cancelled with detection/audio cleanup | Confirmed | `PolloraController.cs` |

Core loop: interact with a wardrobe -> player movement is disabled and the player is snapped to its hide point -> after a random delay Pollora selects a non-repeating random hiding spot and inspects it -> keeping eyes open in that spot fills detection; closing them prevents detection but fills stress -> maximum stress triggers a scream and interrupts the normal sequence -> Pollora runs to the compromised hiding spot -> capture leads to a game-over fade and delayed respawn.

## Coding Conventions

- Global namespace, one main type per file, PascalCase public members/types, camelCase fields and locals.
- Inspector configuration uses `[SerializeField] private` and `[Header]`; sibling dependencies commonly fall back to `GetComponent` in `Awake`.
- Braces use Allman style. Guard clauses are common.
- No nullable-reference annotations, XML documentation convention, DI framework, or custom async abstraction was found.

## Testing And Validation

- Unity Test Framework 1.6.0 is installed.
- No first-party EditMode/PlayMode tests or test assemblies were found.
- No CI configuration or documented build/test command was found.
- The running Unity Editor compiled and reloaded the modified first-party assembly successfully on 2026-07-20. Runtime Play Mode behaviour remains to be verified.

## Available Unity Tooling

- No callable Unity MCP integration is available in the current session. Repository/serialized-scene inspection and the running Editor log are available; Play Mode control, scene runtime inspection, tests, and profiler data remain unavailable.

## Important Constraints And Risks

- Required same-object dependencies are declared with `RequireComponent`; camera, hiding-point, Pollora waypoint, and Game Over boundaries report invalid configuration instead of dereferencing missing references.
- The prototype builds its NavMesh synchronously when the scene starts. This avoids a separate bake step but should be replaced with pre-baked NavMesh data if scene complexity or startup cost grows.
- Gameplay uses legacy input calls even though both input backends are enabled.

## Unknowns And Confidence

- Runtime gameplay behaviour is unknown until the random inspection and scream-interruption paths are exercised in Play Mode.
- Intended shipping platforms and game flow beyond the single prototype scene are unknown.
- Prefab composition and whether the breathing component is intended for later use were not established.

## Source Files Inspected

- `ProjectSettings/ProjectVersion.txt`
- `ProjectSettings/EditorBuildSettings.asset`
- `ProjectSettings/GraphicsSettings.asset`
- `ProjectSettings/ProjectSettings.asset`
- `Packages/manifest.json`
- `Assets/Scenes/Test_Hiding_v01.unity`
- All first-party scripts under `Assets/Scripts`

<!-- unity-onboarding:generated:end -->
