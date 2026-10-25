# Unity AR Organ Viewer — Developer Handoff

This guide records the current project structure, the important behavior already implemented, known limitations, and the workflow for adding another organ. It is written for someone continuing the existing Unity project; keep the current scenes, prefabs, script references, and `.meta` GUIDs intact unless there is a specific reason to change them.

## Current project state

- Unity: **2022.3.62f3**
- Main platform: **Android / ARCore**
- AR Foundation and ARCore packages: **5.2.2**
- glTFast: **6.14.1**, embedded locally under `Packages/com.unity.cloud.gltfast` because of the Android GLES3 shader fix described below.
- Current Git branch: `codex/add-brain-organ`
- Review `git log --oneline` for the current feature and documentation commits on this branch.
- The enabled scene in `ProjectSettings/EditorBuildSettings.asset` is `Assets/Scenes/SampleScene.unity`.
- The app name used by the onboarding UI is **AnatoLens**.
- The current working tree contains uncommitted work from earlier sessions. Review `git status` before staging or reverting anything; organ assets, scenes, metadata, and scripts may all have user changes.

## Project layout

| Path | Purpose |
| --- | --- |
| `Assets/Scenes/SampleScene.unity` | Active build scene. Contains the AR session, image manager/spawner, organ controls, and app UI. |
| `Assets/ARapp.unity` | Older/alternate scene. It is not currently enabled in Build Settings and still has an `AnimalCell` image-to-prefab entry. Do not assume edits here affect the Android build. |
| `Assets/MyImages.asset` | XR Reference Image Library assigned to the active scene's `ARTrackedImageManager`. It contains the Heart, Brain, Eye, Lung, Kidney, Neuron, Female reproductive system, Digestive system, Plant cell, Nephron, and Animal cell markers. |
| `Assets/Prefabs/HeartPrefab.prefab` | Heart model, labels, heartbeat component, and interaction scripts. |
| `Assets/Prefabs/BrainPrefab.prefab` | Brain model with rotate, zoom, reset, tap/explanation, and organ identity components. It does not have Heart labels or heartbeat animation. |
| `Assets/Prefabs/EyePrefab.prefab` | Eye GLB model with rotate, zoom, reset, tap/explanation, and organ identity components. It has no anatomical labels or heartbeat animation yet. |
| `Assets/Prefabs/LungPrefab.prefab` | Lung FBX with rotate, zoom, reset, tap/explanation, and organ identity components. |
| `Assets/Prefabs/KidneyPrefab.prefab` | Kidney FBX with rotate, zoom, reset, tap/explanation, and organ identity components. |
| `Assets/Prefabs/NeuronPrefab.prefab` | Neuron model prefab with interaction controls and organ identity. Verify the imported mesh/materials on device when changing its source asset. |
| `Assets/Prefabs/AnimalCellPrefab.prefab` | Animal Cell GLB prefab with rotate, zoom, reset, tap, and organ identity components. |
| `Assets/Prefabs/FemaleReproductiveSystemPrefab.prefab` | Female reproductive system model prefab with interaction controls, organ identity, and hotspot label support. |
| `Assets/Models/Heart/` | Heart FBX and its texture assets. |
| `Assets/Models/Brain/source/Rotten Brain.glb` | Imported Brain GLB model. Its extracted textures are under `Assets/Models/Brain/textures/`. |
| `Assets/Models/Eye/` | Active Eye GLB under `Replacement/`; its base-color texture is embedded in the model. |
| `Assets/Models/Lung/` | Lung FBX under `source/` and its textures under `textures/`. |
| `Assets/Models/Kidney/kidney/` | Kidney FBX under `source/`, with its diffuse/normal textures and materials under `textures/` and `Materials/`. |
| `Assets/Models/Female_Reproductive_system/` | Female reproductive system GLB source used by `FemaleReproductiveSystemPrefab`. |
| `Assets/Models/Animal_cell/animal_cell.glb` | Animal Cell GLB by Forged1212, used by `AnimalCellPrefab`; the model contains colored materials and no separate image textures. Licensed CC BY 4.0; see the adjacent `ATTRIBUTION.txt`. |
| `Assets/Brain.png` | Brain tracking/reference image currently used by `MyImages.asset`. |
| `Assets/Eye.png` | Eye tracking/reference image currently used by `MyImages.asset`. |
| `Assets/Lung.png` | Lung tracking/reference image used by `MyImages.asset`. Its physical size is unspecified so ARCore can estimate it. |
| `Assets/Scripts/` | AR tracking, model interactions, numbered anatomy hotspots, UI overlays, chat, explanation, voice input, and support scripts. |
| `Packages/com.unity.cloud.gltfast/` | Embedded copy of glTFast 6.14.1 with one GLES3 shader include correction. The manifest points to this local package. |

Unity `.meta` files contain asset GUIDs referenced by scenes, prefabs, and libraries. Keep them alongside their assets and avoid deleting/recreating them when moving files.

## How image tracking and prefab selection work

1. `ARTrackedImageManager` in `SampleScene` uses `Assets/MyImages.asset`.
2. AR Foundation reports the **Name** assigned to the tracked reference image.
3. `TrackedImageSpawner` looks for an exact, case-sensitive `imageName` match in its `imagePrefabs` list.
4. `TrackedImageSpawner` uses an exact, case-sensitive mapping when one exists. An exact entry with an empty prefab intentionally spawns nothing; only an image with no mapping at all uses the legacy first-prefab fallback. This lets a marker be registered before its model is ready without showing the wrong organ.
5. When a model is spawned, the spawner parents it to the tracked image and sets the prefab root's local scale to `(1,1,1)`. It repeats this reset when unlocking/re-attaching a model.

**Important distinction:** the app tracks images that have been added to `MyImages.asset`; it does not currently recognize an arbitrary image from the camera or determine that an unknown picture is a heart. The earlier idea to ask the user to spell a name for an unmatched image, then associate that image with a model/library entry, has not been implemented. Runtime creation/updating of image targets is not part of the current workflow.

## Existing app behavior

- **Image-to-organ display:** `TrackedImageSpawner.cs` maintains tracked image references and spawned models. `SampleScene` maps each configured reference image to its matching prefab, including `Animal_cell` → `AnimalCellPrefab`.
- **Touch rotation:** `TouchRotate.cs` rotates the organ on one-finger drags. Horizontal screen swipes drive world-Y rotation and vertical swipes drive world-X rotation consistently in portrait and landscape. World axes keep these gestures consistent even when an organ mesh/controller has a custom authored starting orientation.
- **Pinch zoom:** `PinchZoom.cs` scales the controller object on two-finger pinches. Its current default limits are 0.5–2.0 times that object's authored scale.
- **Reset:** `ResetHeart.cs` restores the controller's starting local scale and rotation. The class name is historical; it is used on the Brain prefab too. The global UI bridge `ResetButtonController.cs` finds a `ResetHeart` in the scene.
- **Tap for explanation:** `HeartTapDetector.cs` detects a short tap on the organ collider and opens the explanation panel. A tap on a label is treated as label selection instead.
- **Organ context:** `ModelInfo.cs` exposes `ActiveModel` and the `organName`. Chat and explanation prompts use this name, so adding a correctly configured `ModelInfo` lets the shared UI discuss that organ without a separate organ-specific prompt.
- **Numbered labels:** `AnatomyHotspotOverlay.cs` creates screen-space numbered pins and tap-to-open information cards for supported organs, including the Female reproductive system and Brain. The `Labels` menu button toggles pins; tapping one opens its anatomy name and description. Per-organ hotspot data is in `BuildOrganHotspots()`. Brain pins can use numbered model-root child anchors and optional surface/visibility helpers. `LabelTogglerController.cs`, `LabelsToggleButtonBridge.cs`, and `MenuToggle.cs` coordinate the toggle.
- **Brain label authoring:** `Tools > Anatomy Label Authoring` opens `AnatomyLabelAuthoringWindow.cs`. Open `BrainPrefab` in Prefab Mode, create missing anchors, select a numbered label, and position its `BrainModel` child anchor with the Scene handle or model-local coordinates. The authoring window parents new anchors under `BrainModel`, which rotates with the brain mesh; existing anchors are also children of `BrainModel`. A mesh is not required for attachment. Optional surface snapping picks the nearest vertex on the largest mesh near the intended point; visibility/occlusion checks can use that mesh. `Lock placement` prevents accidental position edits in the authoring window; it does not freeze the pin on screen. Save the prefab after placing anchors.
- **Heartbeat:** `HeartbeatAnimator.cs` can pulse the Heart; the Heart prefab currently starts with `isBeating` off. Do not add this behavior to other organs unless it makes sense for them.
- **Fix/Unlock:** `TrackedImageSpawner.ToggleLock()` detaches the current visible model from its image and adds `ScreenLockFollower.cs` so it remains in front of the camera. Unlock reparents it to the tracked image. The follower corrects for the rendered model bounds and smooths camera position movement.
- **Overlays:** menu, labels, explanation, and chat cooperate to close other modes when one opens. See `MenuToggle.cs`, `LabelToggleController.cs`, `ChatbotController.cs`, and `ExplanationPanelController.cs` when changing overlay behavior.
- **Contextual touch menu and assistant:** the menu opens from a larger bottom-left launcher and lays out actions in a compact drawer. `Beat` appears only for models with `HeartbeatAnimator`, and `Labels` is contextual to supported models. Reset remains beside Fix at the bottom. Chat and Voice are separate icon buttons under a draggable robot widget; its position is saved with `PlayerPrefs`, and it makes a gentle wave motion at intervals. `MenuToggle.cs`, `AssistantWidgetDragHandler.cs`, and `AssistantRobotIdleAnimation.cs` create and control it at runtime. Voice records on the first tap, stops on the next, then opens Chat with the recognized transcript.
- **AI chat and explanation:** the scripts send requests to OpenAI's Chat Completions endpoint; chat uses organ context and explanation requests a short student-friendly explanation.
- **Chat panel layout:** `ChatbotController.ConfigureChatLayout()` expands the existing `ChatPanel` to most of the display when chat opens and resizes its scroll area, message text, input, Send button, and close control. It reuses the scene's existing UI objects, creating only the header if missing. If the chat UI is changed in the scene, confirm its immediate child names still match `ChatScrollView`, `ChatInputField`, and `SendButton`.
- **Voice input:** `VoiceInputController.cs` records microphone audio, requests Android microphone permission, transcribes through Whisper, and places the transcript into chat.
- **Onboarding:** `WelcomeScreenController.cs` creates the AnatoLens welcome UI at runtime on every app launch. It shows a short splash, a welcome/get-started page, then instructions with a back arrow and a Start Scanning button. AR sessions and image tracking stay disabled until the user starts scanning. The UI is generated in code; it is not a serialized Canvas under `SampleScene`.
- **Camera configuration:** `ARCameraConfigSelector.cs` chooses the lowest available camera configuration once after receiving a frame.
- **AnchorStabilizer:** this script exists in `Assets/Scripts/`, but there is no current scene or prefab reference to it. It is not part of the active tracking/locking flow. Locking currently uses `ScreenLockFollower`.

## Adding another organ

Use this checklist for a new organ such as Lung or Kidney.

### 1. Add the model asset

1. Put the model in its own folder under `Assets/Models/`, for example `Assets/Models/Lung/`. Separate `source/` and `textures/` subfolders are optional; keep external texture files beside the model or in a clearly named subfolder so the importer can find them.
2. Import the model in Unity. Check its materials and textures in the Inspector; make sure the model is visible and the materials render on Android.
3. Preserve the original files and their `.meta` files. Do not place the only copy inside `Library/` or `Temp/`.

### 2. Create and configure its prefab

1. Create a new prefab under `Assets/Prefabs/`, named `<Organ>Prefab` (for example `LungPrefab`). Use the Brain prefab for a simple unlabelled organ or Heart for an organ that needs authored labels/heartbeat behavior.
2. Keep a controller parent (for example `<Organ>Model`) and put the imported mesh below it. The controller should carry the interaction scripts needed by that organ: usually `TouchRotate`, `PinchZoom`, `ResetHeart`, `HeartTapDetector`, and `ModelInfo`.
3. Set `ModelInfo.organName` to the exact readable organ name, such as `Lung`. This is used by chat and explanation prompts.
4. Ensure the mesh has a collider so touch raycasts can hit it. A `BoxCollider` is sufficient for simple models; use a better-fitting collider if the box causes inaccurate taps.
5. **Scale the mesh child, not only the prefab root.** The spawner resets the prefab root to scale one. The Brain GLB imported at roughly 76 × 85 × 99 Unity units, so its mesh child is set to `(0.0015, 0.0015, 0.0015)`, giving a rendered size around 11.4 × 12.8 × 14.9 cm before pinch zoom. Use measured model bounds and choose a suitable real-world AR size for each new model. Keep scale uniform unless distortion is intended.
6. Keep `PinchZoom` and `ResetHeart` on the controller parent so pinch/reset changes the organ as a whole. If changing the controller's starting scale, update the `PinchZoom` min/max values accordingly; its current limits are absolute local-scale values, not multipliers of the imported mesh scale.
7. The spawner attaches `AnatomyHotspotOverlay` automatically to mapped tracked models. Add the new organ's numbered hotspot definitions and short descriptions to `BuildOrganHotspots()` in `AnatomyHotspotOverlay.cs`, keyed by the exact marker name. Positions are normalized to the renderer bounds (roughly -1 to +1); inspect and tune them for the actual model orientation. Do not add the legacy `BillboardLabel` objects for new organs.
8. Do not add `HeartbeatAnimator` by default. Add it only if the organ needs a pulse effect and expose an appropriate UI control.

### 3. Add its tracking image

1. Add a clear, high-detail image as an entry in `Assets/MyImages.asset` through Unity's XR Reference Image Library Inspector.
2. Set the entry **Name** to exactly the organ key used in the scene mapping (case-sensitive, e.g. `Lung`).
3. Enable **Specify Size** and enter the real width and height of the printed/displayed marker in meters. Use its actual aspect ratio. A wrong physical size makes the model's AR scale unreliable.
4. `Keep Texture at Runtime` is optional for standard preconfigured image tracking. It is only needed if runtime code needs to read the source texture; it does not switch recognition on.
5. Verify `ARTrackedImageManager` in the active scene still references this library. The current maximum number of moving images is set to 1.

### 4. Map marker name to prefab

In `Assets/Scenes/SampleScene.unity`, select the GameObject with `TrackedImageSpawner` and add an `imagePrefabs` entry:

```text
imageName: Lung
prefab: LungPrefab
```

Use the same name as the reference image. Add an explicit entry rather than relying on the fallback-first-prefab behavior. If `ARapp.unity` is intentionally brought back into use, update its spawner mapping too and add the scene to Build Settings as appropriate; it is not the active build scene now.

### 5. Test the complete path

1. Let Unity finish importing the model/package and check the Console for compile/import errors.
2. In `SampleScene`, confirm the target library entry, exact mapping, prefab reference, organ name, mesh scale, and collider.
3. Build and run on an ARCore Android device. Scan the actual marker at its configured physical size and under reasonable lighting.
4. Confirm that only the intended prefab appears; rotate, pinch, reset, tap for explanation, and test Fix/Unlock.
5. Check visuals and real-world size on-device. Editor preview scale is not a substitute for scanning the printed marker.

## Existing Heart and Brain specifics

| Item | Current setup |
| --- | --- |
| Heart marker | Entry named `Heart` in `Assets/MyImages.asset`; explicitly maps to `HeartPrefab` in `SampleScene`. |
| Brain marker | `Assets/Brain.png`; entry named `Brain` in `Assets/MyImages.asset`; size is approximately 0.20 m × 0.1281 m. The physical print/display should match those dimensions. |
| Brain model | `Assets/Models/Brain/source/Rotten Brain.glb`, textures extracted under `Assets/Models/Brain/textures/`; prefab is `Assets/Prefabs/BrainPrefab.prefab`. |
| Brain model transform | The imported mesh child is scaled to `(0.0015,0.0015,0.0015)`, about 11.4 × 12.8 × 14.9 cm by its source bounds; the parent handles rotation and pinch zoom. The root has a different authored scale but is reset to one by the spawner at runtime. |
| Brain scan starting pose (before manual label placement) | Restore these prefab-local transforms after temporarily rotating the model to place labels: `BrainPrefab` root Position `(0, 0, 0)`, Scale `(1, 1, 1)`, serialized Rotation quaternion `(x=0.59933245, y=-0.017941942, z=-0.029279144, w=0.79976344)`; `BrainModel` child Position `(0, 0, 0)`, Rotation Euler `(0.457, -86.085, -5.529)`, Scale `(1, 1, 1)`; `Brain` mesh child Position `(0, 0, 0)`, Rotation Euler `(1.21, 0.849, -92.041)`, Scale `(0.0015, 0.0015, 0.0015)`. These are the saved prefab values inspected on 2026-10-19. Restore each transform on its named object; do not rotate the label anchors to restore the model pose. |
| Brain prefab contents | `BrainPrefab` → `BrainModel` (rotate/zoom/reset/tap and ModelInfo scripts) → `Brain` mesh with renderer and collider. Brain hotspot pins/cards are provided by `AnatomyHotspotOverlay.cs`; numbered `LabelAnchor_01` … `LabelAnchor_07` are children of `BrainModel`, the transform that rotates the brain. There is no heartbeat animation. |
| Brain labels | 1 Frontal lobe; 2 Parietal lobe; 3 Temporal lobe; 4 Occipital lobe; 5 Cerebellum; 6 Brainstem; 7 Cerebrum. Definitions and default points are in `BuildOrganHotspots()` and `AnatomyLabelAuthoringWindow.cs`. Current descriptions for labels 1–3 use the supplied anatomical boundaries. Surface snapping is enabled for labels 1–3 and 7 when anchors are generated at runtime or via the authoring tool. The unlocked Cerebellum and Brainstem anchors snap to their nearest brain-surface vertex at runtime so their pins do not hover. These default points are starting points; visually verify and refine them in Prefab Mode for this specific imported brain mesh.
| Eye marker | `Assets/Eye.png`; entry named `Eye` in `Assets/MyImages.asset`. Its physical size is unspecified so ARCore can estimate it; use a sharp, flat print with enough detail and test tracking on the target device. |
| Eye model | `Assets/Models/Eye/Replacement/eye.glb` is the active and only Eye model. It is imported by the embedded glTFast package, and its base-color texture is embedded in the GLB. Keep the model’s own materials; no external eye texture is needed. |
| Eye prefab contents | `EyePrefab` → `EyeModel` (rotate/zoom/reset/tap, BoxCollider, and `ModelInfo` set to `Eye`) → imported `eye.glb` model (`EyeReplacement`). The model is uniformly scaled to about 12 cm on its longest axis and turned 180° around local X so its front faces the screen. It has no anatomical labels or heartbeat animation yet. |
| Lung marker and model | `Assets/Lung.png` is registered as `Lung` with unspecified physical size. `Assets/Models/Lung/source/Lung_with_Holes_Final.fbx` supplies the model; the prefab scales its longest dimension to about 18 cm and centers it. Test the model's orientation and size in AR on the device. |
| Lung prefab contents | `LungPrefab` → `LungModel` (rotate/zoom/reset/tap, fitted BoxCollider, and `ModelInfo` set to `Lung`) → imported `Lung` FBX hierarchy. It has no labels or heartbeat animation yet. |
| Kidney marker and model | `Assets/Kidney.jpg` is registered as `Kidney` at 0.20 × 0.20 m. The model source is `Assets/Models/Kidney/kidney/source/kidney.fbx`; its renderer bounds are fitted uniformly to a 16 cm longest dimension and centered in the prefab. Diffuse textures are assigned to the matching materials; cortex and capsule materials use a soft warm tissue tint. Confirm appearance, orientation, and physical size on an Android device. |
| Kidney prefab contents | `KidneyPrefab` → `KidneyModel` (rotate/zoom/reset/tap, fitted BoxCollider, and `ModelInfo` set to `Kidney`) → imported Kidney FBX hierarchy. It has no labels or heartbeat animation yet. |
| Neuron marker and model | `Assets/Neuron.png` is registered as `Neuron`; its prefab is `Assets/Prefabs/NeuronPrefab.prefab`. The source folder contains GLB and 3ds Max assets. If replacing the source, update the prefab to reference the intended imported model and confirm its materials and hotspot positions on device. |
| Female reproductive system marker and model | `Assets/Female_Reproductive_system.png` is registered as `Female_Reproductive_system`, sized 0.20 × 0.16 m. The GLB is under `Assets/Models/Female_Reproductive_system/`; `Assets/Prefabs/FemaleReproductiveSystemPrefab.prefab` is assigned in the scene and has hotspot content in `AnatomyHotspotOverlay.cs`. Check orientation and pin placement on device after model changes. |
| Animal cell marker and model | `Assets/Animal_cell.png` is registered as `Animal_cell` (0.20 × 0.1169 m); it maps to `AnimalCellPrefab`. The GLB is fitted to about 16.5 cm on its longest axis, centered, and uses its authored colored materials. The GLB has no separate textures. Verify its final appearance and scale on an Android device. Model attribution and license are in `Assets/Models/Animal_cell/ATTRIBUTION.txt`. |
| Sample scene mapping | `Heart` → `HeartPrefab`; `Brain` → `BrainPrefab`; `Eye` → `EyePrefab`; `Lung` → `LungPrefab`; `Kidney` → `KidneyPrefab`; `Neuron` → `NeuronPrefab`; `Female_Reproductive_system` → `FemaleReproductiveSystemPrefab`; `Digestive_system` → `DigestiveSystemPrefab`; `Plant_cell` → `PlantCellPrefab`; `Nephron` → `NephronPrefab`; `Animal_cell` → `AnimalCellPrefab`. |

Eye model attribution: “Eye” by germydan, [Sketchfab source](https://sketchfab.com/3d-models/eye-07c3674cfc26460d803f8f497f8c0f35), licensed under [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/). The mesh is unchanged; it is uniformly scaled for AR display.

## Android shader build fix

The project received an Android GLES3 shader compilation failure in `glTF/PbrSpecularGlossiness`, inside glTFast's Built-In render-pipeline Meta include. The `SpecularSetup` call selected a three-argument form using `_METALLICGLOSSMAP`, while that shader's specular-glossiness function uses `_SPECGLOSSMAP` to determine its argument list.

To keep the correction across Library/package-cache refreshes, glTFast 6.14.1 is embedded in `Packages/com.unity.cloud.gltfast`; `Packages/manifest.json` references it as `file:com.unity.cloud.gltfast`. The targeted change is in:

```text
Packages/com.unity.cloud.gltfast/Runtime/Shader/Built-In/glTFIncludes/glTFUnityStandardMeta.cginc
```

The conditional around `UNITY_SETUP_BRDF_INPUT` uses `_SPECGLOSSMAP`. Do not delete the embedded package or revert the conditional without verifying an Android GLES3 build. If upgrading glTFast, check whether the upstream package has corrected this include and keep the dependency/lock files consistent.

The shader source fix is committed, but an Android build after the correction still needs to be confirmed in Unity. The earlier screenshot showed the failure before this patch.

## Known limitations and follow-up work

- The app does **not** currently classify arbitrary camera images as hearts or organs. Only images preconfigured in `MyImages.asset` can be tracked.
- The proposed “unmatched image → ask user to spell/name it → bind that image to an organ and save it” flow is a future feature, not implemented. Design it as a separate workflow; do not confuse a spelled word match with image recognition. Investigate AR Foundation's runtime/mutable reference-image support for the target ARCore version before promising permanent runtime library updates.
- Unconfigured marker names still use the legacy first-prefab fallback. Add exact entries for new targets; use an explicit empty prefab entry when a marker is ready before its model.
- Heart and Brain have numbered anatomy labels. Brain anchor positions and click angles still need visual review in Unity/AR; the source-mesh surface snap is an approximate nearest-vertex helper and does not identify anatomical sulci automatically.
- `ModelInfo.ActiveModel` and the reset UI use a single active/global lookup. If the product needs multiple organs visible and independently controllable at the same time, revisit how UI actions choose their target model.
- Chat, explanation, and voice use separate Inspector API-key fields on `ChatPanel`, `ExplanationPanel`, and `MicButton` in `SampleScene`. All three integrations call OpenAI directly: chat/explanation use `https://api.openai.com/v1/chat/completions` with `gpt-4o-mini`; transcription uses `https://api.openai.com/v1/audio/transcriptions` with `whisper-1`. The value must be an active OpenAI API key, not a key issued by another AI service. Current guards check for an OpenAI-style key shape and display a setup hint when that check fails; a passing shape check does not prove the key is active, has billing, or has API access. Never put a real key in scripts, scenes, prefabs, Git, or a distributed Android APK. The scene serializes Inspector values into project files and client builds expose embedded secrets. Rotate any key that has been included in a shared build/repository; move requests behind a server-side proxy before public release. Do not put a real key in this handoff document.
- The chat/API key values in the present scene did not match the format expected by the OpenAI endpoints during the latest local inspection, so the three AI features need an active OpenAI key before successful live requests can be expected. No key was sent to OpenAI as part of this inspection.
- Brain size and tracking have not yet been verified on a physical device after the latest scale/reference-image edits.
- `Assets/ARapp.unity` and `Assets/Scenes/SampleScene.unity` are separate scenes with different spawner setup. Build Settings currently selects only `SampleScene`.

## Recent development milestones

The branch history shows the project evolving from initial AR setup through pinch zoom/reset and touch rotation, then adding model locking/stability, menu and heartbeat behavior, the Brain/Eye/Lung/Kidney organs, Android glTFast shader correction, AnatoLens onboarding, API-key diagnostics, and a larger chat panel. The current checkout adds a shared numbered hotspot/card label system. Review `git status` before staging: this work may not yet be committed or pushed. Review `git log --oneline` for commit history.
