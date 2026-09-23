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

## Project layout

| Path | Purpose |
| --- | --- |
| `Assets/Scenes/SampleScene.unity` | Active build scene. Contains the AR session, image manager/spawner, organ controls, and app UI. |
| `Assets/ARapp.unity` | Older/alternate scene. It is not currently enabled in Build Settings and still has an `AnimalCell` image-to-prefab entry. Do not assume edits here affect the Android build. |
| `Assets/MyImages.asset` | XR Reference Image Library assigned to the active scene's `ARTrackedImageManager`. It currently contains the Heart and Brain marker images. |
| `Assets/Prefabs/HeartPrefab.prefab` | Heart model, labels, heartbeat component, and interaction scripts. |
| `Assets/Prefabs/BrainPrefab.prefab` | Brain model with rotate, zoom, reset, tap/explanation, and organ identity components. It does not have Heart labels or heartbeat animation. |
| `Assets/Prefabs/EyePrefab.prefab` | Eye FBX model with rotate, zoom, reset, tap/explanation, and organ identity components. It has no anatomical labels or heartbeat animation yet. |
| `Assets/Models/Heart/` | Heart FBX and its texture assets. |
| `Assets/Models/Brain/source/Rotten Brain.glb` | Imported Brain GLB model. Its extracted textures are under `Assets/Models/Brain/textures/`. |
| `Assets/Models/Eye/` | Eye FBX under `source/` and its color, bump, and specular textures under `textures/`. |
| `Assets/Brain.png` | Brain tracking/reference image currently used by `MyImages.asset`. |
| `Assets/Eye.png` | Eye tracking/reference image currently used by `MyImages.asset`. |
| `Assets/Scripts/` | AR tracking, model interactions, labels, UI overlays, chat, explanation, voice input, and support scripts. |
| `Packages/com.unity.cloud.gltfast/` | Embedded copy of glTFast 6.14.1 with one GLES3 shader include correction. The manifest points to this local package. |

Unity `.meta` files contain asset GUIDs referenced by scenes, prefabs, and libraries. Keep them alongside their assets and avoid deleting/recreating them when moving files.

## How image tracking and prefab selection work

1. `ARTrackedImageManager` in `SampleScene` uses `Assets/MyImages.asset`.
2. AR Foundation reports the **Name** assigned to the tracked reference image.
3. `TrackedImageSpawner` looks for an exact, case-sensitive `imageName` match in its `imagePrefabs` list.
4. If there is no exact entry, the current legacy fallback returns the **first configured prefab**. That fallback is not organ recognition and can show the wrong organ. Add an explicit mapping for each organ marker.
5. When a model is spawned, the spawner parents it to the tracked image and sets the prefab root's local scale to `(1,1,1)`. It repeats this reset when unlocking/re-attaching a model.

**Important distinction:** the app tracks images that have been added to `MyImages.asset`; it does not currently recognize an arbitrary image from the camera or determine that an unknown picture is a heart. The earlier idea to ask the user to spell a name for an unmatched image, then associate that image with a model/library entry, has not been implemented. Runtime creation/updating of image targets is not part of the current workflow.

## Existing app behavior

- **Image-to-organ display:** `TrackedImageSpawner.cs` maintains tracked image references and spawned models. `SampleScene` maps `Heart` to `HeartPrefab` and `Brain` to `BrainPrefab`.
- **Touch rotation:** `TouchRotate.cs` rotates the organ on one-finger drags. Horizontal screen swipes drive world-Y rotation and vertical swipes drive world-X rotation consistently in portrait and landscape. World axes keep these gestures consistent even when an organ mesh/controller has a custom authored starting orientation.
- **Pinch zoom:** `PinchZoom.cs` scales the controller object on two-finger pinches. Its current default limits are 0.5–2.0 times that object's authored scale.
- **Reset:** `ResetHeart.cs` restores the controller's starting local scale and rotation. The class name is historical; it is used on the Brain prefab too. The global UI bridge `ResetButtonController.cs` finds a `ResetHeart` in the scene.
- **Tap for explanation:** `HeartTapDetector.cs` detects a short tap on the organ collider and opens the explanation panel. A tap on a label is treated as label selection instead.
- **Organ context:** `ModelInfo.cs` exposes `ActiveModel` and the `organName`. Chat and explanation prompts use this name, so adding a correctly configured `ModelInfo` lets the shared UI discuss that organ without a separate organ-specific prompt.
- **Heart labels:** `BillboardLabel.cs`, `LabelSelectionInteraction.cs`, and `LabelToggleController.cs` provide screen-facing text, leader lines, selectable labels, and the label visibility mode. These are authored in the Heart prefab. Brain currently has no authored anatomical labels.
- **Heartbeat:** `HeartbeatAnimator.cs` can pulse the Heart; the Heart prefab currently starts with `isBeating` off. Do not add this behavior to other organs unless it makes sense for them.
- **Fix/Unlock:** `TrackedImageSpawner.ToggleLock()` detaches the current visible model from its image and adds `ScreenLockFollower.cs` so it remains in front of the camera. Unlock reparents it to the tracked image. The follower corrects for the rendered model bounds and smooths camera position movement.
- **Overlays:** menu, labels, explanation, and chat cooperate to close other modes when one opens. See `MenuToggle.cs`, `LabelToggleController.cs`, `ChatbotController.cs`, and `ExplanationPanelController.cs` when changing overlay behavior.
- **Contextual touch menu:** the menu opens from a larger bottom-left launcher and lays out larger action buttons in a compact drawer. The `Beat` item is visible only when the active model has `HeartbeatAnimator`; `Labels` is visible only when it has `LabelToggleController`. The Reset control is outside the drawer, at the bottom beside Fix, so opening the menu cannot cover it. Layout sizes are applied by `MenuToggle.cs` against the Canvas scaler, so edit its constants if adapting the menu for a different screen design.
- **AI chat and explanation:** the scripts send requests to OpenAI's Chat Completions endpoint; chat uses organ context and explanation requests a short student-friendly explanation.
- **Voice input:** `VoiceInputController.cs` records microphone audio, requests Android microphone permission, transcribes through Whisper, and places the transcript into chat.
- **Camera configuration:** `ARCameraConfigSelector.cs` chooses the lowest available camera configuration once after receiving a frame.
- **AnchorStabilizer:** this script exists in `Assets/Scripts/`, but there is no current scene or prefab reference to it. It is not part of the active tracking/locking flow. Locking currently uses `ScreenLockFollower`.

## Adding another organ

Use this checklist for a new organ such as Lung or Kidney.

### 1. Add the model asset

1. Put the source model and its texture files in a clear folder, for example `Assets/Models/Lung/source/` and `Assets/Models/Lung/textures/`.
2. Import the model in Unity. Check its materials and textures in the Inspector; make sure the model is visible and the materials render on Android.
3. Preserve the original files and their `.meta` files. Do not place the only copy inside `Library/` or `Temp/`.

### 2. Create and configure its prefab

1. Create a new prefab under `Assets/Prefabs/`, named `<Organ>Prefab` (for example `LungPrefab`). Use the Brain prefab for a simple unlabelled organ or Heart for an organ that needs authored labels/heartbeat behavior.
2. Keep a controller parent (for example `<Organ>Model`) and put the imported mesh below it. The controller should carry the interaction scripts needed by that organ: usually `TouchRotate`, `PinchZoom`, `ResetHeart`, `HeartTapDetector`, and `ModelInfo`.
3. Set `ModelInfo.organName` to the exact readable organ name, such as `Lung`. This is used by chat and explanation prompts.
4. Ensure the mesh has a collider so touch raycasts can hit it. A `BoxCollider` is sufficient for simple models; use a better-fitting collider if the box causes inaccurate taps.
5. **Scale the mesh child, not only the prefab root.** The spawner resets the prefab root to scale one. The Brain GLB imported at roughly 76 × 85 × 99 Unity units, so its mesh child is set to `(0.0015, 0.0015, 0.0015)`, giving a rendered size around 11.4 × 12.8 × 14.9 cm before pinch zoom. Use measured model bounds and choose a suitable real-world AR size for each new model. Keep scale uniform unless distortion is intended.
6. Keep `PinchZoom` and `ResetHeart` on the controller parent so pinch/reset changes the organ as a whole. If changing the controller's starting scale, update the `PinchZoom` min/max values accordingly; its current limits are absolute local-scale values, not multipliers of the imported mesh scale.
7. If the new organ needs labels, author label anchor objects and text under a `Labels` container using the same pattern as `HeartPrefab`. On Brain, add `Labels` as a child of `BrainModel` and as a **sibling** of the scaled `Brain` mesh. This keeps anchor positions out of the mesh's small `(0.0015)` scale. Add one empty anchor GameObject per anatomical label under `Labels`, position each anchor at the matching organ part in the prefab, then add the `BillboardLabel` component to each anchor and set its label text. Add `LabelToggleController` to `BrainModel`, assign its `labelsContainer` field to `Labels`, and leave the container inactive initially. `MenuToggle` will then show the Labels action for Brain. The current Brain prefab does not yet have these label objects.
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
| Brain prefab contents | `BrainPrefab` → `BrainModel` (rotate/zoom/reset/tap and ModelInfo scripts) → `Brain` mesh with renderer and collider. Add future `Labels` as a sibling of `Brain` under `BrainModel`, with label anchors and `LabelToggleController` wired as described above. There is no heartbeat animation. |
| Eye marker | `Assets/Eye.png`; entry named `Eye` in `Assets/MyImages.asset`. Its physical size is unspecified so ARCore can estimate it; use a sharp, flat print with enough detail and test tracking on the target device. |
| Eye model | `Assets/Models/Eye/Replacement/eye.glb` is the active and only Eye model. It is imported by the embedded glTFast package, and its base-color texture is embedded in the GLB. Keep the model’s own materials; no external eye texture is needed. |
| Eye prefab contents | `EyePrefab` → `EyeModel` (rotate/zoom/reset/tap, BoxCollider, and `ModelInfo` set to `Eye`) → imported `eye.glb` model. The replacement is uniformly scaled to about 12 cm on its longest axis. It has no anatomical labels or heartbeat animation yet. |
| Sample scene mapping | `Heart` → `HeartPrefab`; `Brain` → `BrainPrefab`; `Eye` → `EyePrefab`. |

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
- The first-prefab fallback in `TrackedImageSpawner` can display the wrong organ for a configured image that lacks an explicit mapping. Add explicit mappings and consider replacing this fallback with a warning/no-spawn behavior as part of future hardening.
- Heart anatomical labels are authored only for the Heart model. Brain has no label anchors/content yet.
- `ModelInfo.ActiveModel` and the reset UI use a single active/global lookup. If the product needs multiple organs visible and independently controllable at the same time, revisit how UI actions choose their target model.
- The chat, explanation, and voice scripts expose API-key fields and currently use placeholder strings. Never commit a real OpenAI key in scripts, scenes, or prefabs, and do not ship a client-embedded secret in a public Android app. A server-side proxy/key-management design is needed before public release.
- Brain size and tracking have not yet been verified on a physical device after the latest scale/reference-image edits.
- `Assets/ARapp.unity` and `Assets/Scenes/SampleScene.unity` are separate scenes with different spawner setup. Build Settings currently selects only `SampleScene`.

## Recent development milestones

The branch history shows the project evolving from initial AR setup through pinch zoom/reset and touch rotation, then adding model locking/stability, menu and heartbeat behavior, readable/selectable heart labels, coordinated overlays, and finally the Brain prefab, Brain reference image, Android glTFast shader correction, and a larger organ-aware menu. Review `git log --oneline` for the exact commit history. This handoff describes the current checkout; check the active branch and Unity Editor state before making follow-up changes.
