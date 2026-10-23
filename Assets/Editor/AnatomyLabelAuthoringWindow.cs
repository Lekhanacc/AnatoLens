using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Scene-view workflow for positioning and tuning Brain hotspots.</summary>
public sealed class AnatomyLabelAuthoringWindow : EditorWindow
{
    private sealed class BrainLabelSeed
    {
        public readonly string title;
        public readonly string description;
        public readonly Vector3 normalizedPosition;

        public BrainLabelSeed(string title, string description, Vector3 normalizedPosition)
        {
            this.title = title;
            this.description = description;
            this.normalizedPosition = normalizedPosition;
        }
    }

    private static readonly BrainLabelSeed[] BrainLabels =
    {
        new BrainLabelSeed("Frontal lobe", "The anterior-most region of the cerebrum, extending from the frontal pole back to the central sulcus and inferiorly to the lateral sulcus (Sylvian fissure).", new Vector3(0f, 0.45f, 0.75f)),
        new BrainLabelSeed("Parietal lobe", "The superior-posterior region of the cerebrum, posterior to the central sulcus, superior to the lateral sulcus, and anterior to the parieto-occipital sulcus.", new Vector3(0.35f, 0.45f, -0.1f)),
        new BrainLabelSeed("Temporal lobe", "The inferior-lateral region of the cerebrum, inferior to the lateral sulcus (Sylvian fissure) and anterior to the preoccipital notch on both the left and right sides.", new Vector3(-0.55f, -0.45f, 0.35f)),
        new BrainLabelSeed("Occipital lobe", "Processes visual information.", new Vector3(0f, 0f, -0.95f)),
        new BrainLabelSeed("Cerebellum", "Helps coordinate balance and precise movement.", new Vector3(0f, -0.68f, -0.65f)),
        new BrainLabelSeed("Brainstem", "Connects the brain with the spinal cord and regulates essential functions.", new Vector3(0f, -0.82f, 0.1f)),
        new BrainLabelSeed("Cerebrum", "The largest brain region, supporting thought, sensation, and voluntary action.", new Vector3(0f, 0f, 0.95f))
    };

    private GameObject modelRoot;
    private int selectedLabel;

    [MenuItem("Tools/Anatomy Label Authoring")]
    private static void Open()
    {
        AnatomyLabelAuthoringWindow window = GetWindow<AnatomyLabelAuthoringWindow>(
            "Anatomy Labels");
        window.minSize = new Vector2(320f, 360f);
        window.TryUsePrefabStageRoot();
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += DrawSceneHandle;
        TryUsePrefabStageRoot();
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= DrawSceneHandle;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Brain label authoring", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Anchors are children of BrainModel, so their positions follow the rotating brain. " +
            "Mesh snapping and visibility checks are optional surface helpers.",
            MessageType.Info);

        modelRoot = (GameObject)EditorGUILayout.ObjectField("Model root", modelRoot,
            typeof(GameObject), true);
        if (modelRoot == null)
        {
            EditorGUILayout.HelpBox("Open BrainPrefab in Prefab Mode, or assign its root here.",
                MessageType.Warning);
            if (GUILayout.Button("Use current Prefab Stage"))
                TryUsePrefabStageRoot();
            return;
        }

        selectedLabel = EditorGUILayout.Popup("Label", selectedLabel, GetLabelNames());
        AnatomyHotspotAnchorSettings settings = GetSelectedSettings();
        if (settings == null)
        {
            EditorGUILayout.HelpBox("Create the Brain anchors to start editing positions.",
                MessageType.Warning);
            if (GUILayout.Button("Create missing Brain anchors"))
                CreateMissingBrainAnchors();
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Selected: " + settings.labelTitle, EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(settings.locked))
        {
            EditorGUI.BeginChangeCheck();
            Vector3 localPosition = EditorGUILayout.Vector3Field("Model-local position",
                settings.transform.localPosition);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(settings.transform, "Set anatomy label position");
                settings.transform.localPosition = localPosition;
                EditorUtility.SetDirty(settings.transform);
                RecordPrefabModification(settings.transform);
                SceneView.RepaintAll();
            }
        }

        if (GUILayout.Button("Frame selected label in Scene view"))
        {
            SceneView view = SceneView.lastActiveSceneView;
            if (view != null)
            {
                view.LookAt(settings.transform.position,
                    view.rotation, Mathf.Max(0.08f, HandleUtility.GetHandleSize(
                        settings.transform.position) * 2f));
                view.Repaint();
            }
        }

        EditorGUI.BeginChangeCheck();
        bool locked = EditorGUILayout.Toggle("Lock placement", settings.locked);
        float zoom = EditorGUILayout.FloatField("Click zoom", settings.focusZoom);
        Vector2 popupOffset = EditorGUILayout.Vector2Field("Popup offset", settings.popupOffset);
        bool customDirection = EditorGUILayout.Toggle("Custom click angle",
            settings.useCustomFocusDirection);
        Vector3 focusDirection = settings.focusDirectionLocal;
        using (new EditorGUI.DisabledScope(!customDirection))
            focusDirection = EditorGUILayout.Vector3Field("Model-local view direction",
                focusDirection);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(settings, "Edit anatomy label settings");
            settings.locked = locked;
            settings.focusZoom = Mathf.Max(0f, zoom);
            settings.popupOffset = popupOffset;
            settings.useCustomFocusDirection = customDirection;
            settings.focusDirectionLocal = focusDirection.sqrMagnitude > 0.0001f
                ? focusDirection.normalized : Vector3.forward;
            EditorUtility.SetDirty(settings);
            RecordPrefabModification(settings);
        }

        if (GUILayout.Button("Capture current Scene view as click angle"))
            CaptureSceneViewDirection(settings);

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(settings.locked))
        {
            if (GUILayout.Button("Snap selected label to nearest brain surface point"))
                SnapSelectedAnchor(settings);
        }

        EditorGUILayout.HelpBox(
            "Drag the numbered handle in the Scene view. Snap is optional. Lock a label when its " +
            "position is approved. Save the prefab with Ctrl+S when finished.",
            MessageType.None);
    }

    private void DrawSceneHandle(SceneView sceneView)
    {
        AnatomyHotspotAnchorSettings settings = GetSelectedSettings();
        if (settings == null)
            return;

        Transform anchor = settings.transform;
        Handles.color = Color.cyan;
        Handles.SphereHandleCap(0, anchor.position, Quaternion.identity,
            HandleUtility.GetHandleSize(anchor.position) * 0.045f, EventType.Repaint);
        Handles.Label(anchor.position, settings.labelNumber + ". " + settings.labelTitle);
        if (settings.locked)
            return;

        EditorGUI.BeginChangeCheck();
        Vector3 movedPosition = Handles.PositionHandle(anchor.position, anchor.rotation);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(anchor, "Move anatomy label");
            anchor.position = movedPosition;
            EditorUtility.SetDirty(anchor);
            RecordPrefabModification(anchor);
        }
    }

    private void CreateMissingBrainAnchors()
    {
        if (modelRoot == null)
            return;

        Transform anchorParent = FindBrainMotionRoot();
        MeshFilter mesh = FindLargestMeshFilter(modelRoot.transform);
        Bounds localBounds = GetRootLocalBounds(anchorParent);
        for (int i = 0; i < BrainLabels.Length; i++)
        {
            Transform existing = FindAnchor(i + 1);
            if (existing != null)
            {
                if (existing.GetComponent<AnatomyHotspotAnchorSettings>() == null)
                {
                    Undo.AddComponent<AnatomyHotspotAnchorSettings>(existing.gameObject);
                    InitializeSettings(existing.GetComponent<AnatomyHotspotAnchorSettings>(), i, mesh);
                }
                continue;
            }

            GameObject anchorObject = new GameObject("LabelAnchor_" + (i + 1).ToString("00"));
            Undo.RegisterCreatedObjectUndo(anchorObject, "Create brain label anchor");
            Transform anchor = anchorObject.transform;
            anchor.SetParent(anchorParent, false);
            anchor.localPosition = localBounds.center + Vector3.Scale(localBounds.extents,
                BrainLabels[i].normalizedPosition);
            AnatomyHotspotAnchorSettings settings = Undo.AddComponent<AnatomyHotspotAnchorSettings>(
                anchorObject);
            InitializeSettings(settings, i, mesh);
            if (string.Equals(settings.labelTitle, "Cerebrum", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(settings.labelTitle, "Frontal lobe", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(settings.labelTitle, "Parietal lobe", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(settings.labelTitle, "Temporal lobe", StringComparison.OrdinalIgnoreCase))
                SnapSelectedAnchor(settings);
        }

        EditorUtility.SetDirty(modelRoot);
        selectedLabel = Mathf.Clamp(selectedLabel, 0, BrainLabels.Length - 1);
        SceneView.RepaintAll();
    }

    private Transform FindBrainMotionRoot()
    {
        if (modelRoot == null)
            return null;
        if (string.Equals(modelRoot.name, "BrainModel", StringComparison.OrdinalIgnoreCase))
            return modelRoot.transform;

        Transform brainModel = modelRoot.transform.Find("BrainModel");
        return brainModel != null ? brainModel : modelRoot.transform;
    }

    private void InitializeSettings(AnatomyHotspotAnchorSettings settings, int index,
        MeshFilter mesh)
    {
        settings.labelNumber = index + 1;
        settings.labelTitle = BrainLabels[index].title;
        settings.description = BrainLabels[index].description;
        settings.focusZoom = 2.65f;
        settings.visibilityTarget = mesh != null ? mesh.transform : null;
        EditorUtility.SetDirty(settings);
        RecordPrefabModification(settings);
    }

    private void SnapSelectedAnchor(AnatomyHotspotAnchorSettings settings)
    {
        MeshFilter meshFilter = settings.visibilityTarget != null
            ? settings.visibilityTarget.GetComponent<MeshFilter>()
            : FindLargestMeshFilter(modelRoot.transform);
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            EditorUtility.DisplayDialog("No mesh found",
                "The brain mesh could not be found. You can still position this anchor on the model root manually.",
                "OK");
            return;
        }

        Vector3[] vertices = meshFilter.sharedMesh.vertices;
        Vector3[] normals = meshFilter.sharedMesh.normals;
        if (vertices == null || vertices.Length == 0)
            return;

        Vector3 current = settings.transform.position;
        int nearest = 0;
        float nearestDistance = float.PositiveInfinity;
        for (int i = 0; i < vertices.Length; i++)
        {
            float distance = (meshFilter.transform.TransformPoint(vertices[i]) - current).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = i;
            }
        }

        Undo.RecordObject(settings.transform, "Snap anatomy label to brain");
        settings.transform.position = meshFilter.transform.TransformPoint(vertices[nearest]);
        if (normals != null && normals.Length == vertices.Length)
            settings.transform.rotation = Quaternion.FromToRotation(Vector3.forward,
                meshFilter.transform.TransformDirection(normals[nearest]).normalized);
        EditorUtility.SetDirty(settings.transform);
        RecordPrefabModification(settings.transform);
        SceneView.RepaintAll();
    }

    private void CaptureSceneViewDirection(AnatomyHotspotAnchorSettings settings)
    {
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null || sceneView.camera == null)
            return;

        Vector3 worldDirection = sceneView.camera.transform.position - settings.transform.position;
        if (worldDirection.sqrMagnitude < 0.0001f)
            return;

        Undo.RecordObject(settings, "Capture anatomy label view angle");
        settings.focusDirectionLocal = modelRoot.transform.InverseTransformDirection(
            worldDirection.normalized);
        settings.useCustomFocusDirection = true;
        EditorUtility.SetDirty(settings);
        RecordPrefabModification(settings);
    }

    private AnatomyHotspotAnchorSettings GetSelectedSettings()
    {
        Transform anchor = FindAnchor(selectedLabel + 1);
        return anchor != null ? anchor.GetComponent<AnatomyHotspotAnchorSettings>() : null;
    }

    private Transform FindAnchor(int number)
    {
        if (modelRoot == null)
            return null;
        string wantedName = "LabelAnchor_" + number.ToString("00");
        Transform[] children = modelRoot.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
            if (child.name == wantedName)
                return child;
        return null;
    }

    private void TryUsePrefabStageRoot()
    {
        PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null)
            modelRoot = stage.prefabContentsRoot;
        else if (Selection.activeGameObject != null && modelRoot == null)
            modelRoot = Selection.activeGameObject;
    }

    private static MeshFilter FindLargestMeshFilter(Transform root)
    {
        MeshFilter best = null;
        int bestVertexCount = -1;
        foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            int vertexCount = filter.sharedMesh != null ? filter.sharedMesh.vertexCount : 0;
            if (vertexCount > bestVertexCount)
            {
                best = filter;
                bestVertexCount = vertexCount;
            }
        }
        return best;
    }

    private static Bounds GetRootLocalBounds(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bool found = false;
        Vector3 min = Vector3.positiveInfinity;
        Vector3 max = Vector3.negativeInfinity;
        foreach (Renderer renderer in renderers)
        {
            Bounds bounds = renderer.bounds;
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 corner = root.InverseTransformPoint(bounds.center +
                    Vector3.Scale(bounds.extents, new Vector3(x, y, z)));
                min = Vector3.Min(min, corner);
                max = Vector3.Max(max, corner);
                found = true;
            }
        }
        return found ? new Bounds((min + max) * 0.5f, max - min) :
            new Bounds(Vector3.zero, Vector3.one);
    }

    private string[] GetLabelNames()
    {
        string[] names = new string[BrainLabels.Length];
        for (int i = 0; i < BrainLabels.Length; i++)
            names[i] = (i + 1) + ". " + BrainLabels[i].title;
        return names;
    }

    private static void RecordPrefabModification(UnityEngine.Object target)
    {
        if (PrefabUtility.IsPartOfPrefabInstance(target))
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }
}
