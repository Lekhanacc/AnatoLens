using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.ARSubsystems;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARSubsystems;

/// <summary>Builds the supplied neuron GLB into the same AR prefab workflow as the other organs.
/// The female reproductive system marker is registered with an intentionally empty prefab slot
/// until a 3D model is supplied, so it cannot show an unrelated fallback organ.</summary>
public static class ConfigureNeuronAndFemaleTarget
{
    private const string NeuronModelPath = "Assets/Models/Neuron/NEURON.glb";
    private const string NeuronPrefabPath = "Assets/Prefabs/NeuronPrefab.prefab";
    private const string NeuronMarkerPath = "Assets/Neuron.png";
    private const string FemaleMarkerPath = "Assets/Female_Reproductive_system.png";
    private const string LibraryPath = "Assets/MyImages.asset";
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string NeuronKey = "Neuron";
    private const string FemaleKey = "Female_Reproductive_system";
    private const float MarkerWidthMeters = 0.20f;
    // The source image files contain JPEG data despite their .png names. Unity's
    // imported Texture2D dimensions are power-of-two adjusted, so use the original
    // pixel dimensions here to preserve each printed target's real aspect ratio.
    private const float NeuronMarkerHeightMeters = MarkerWidthMeters * 243f / 411f;
    private const float FemaleMarkerHeightMeters = MarkerWidthMeters * 200f / 250f;
    private const float NeuronLongestDimensionMeters = 0.16f;

    [MenuItem("AnatoLens/Configure Neuron and Female Target")]
    public static void Configure()
    {
        AssetDatabase.Refresh();
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(NeuronModelPath);
        if (source == null)
        {
            Debug.LogError("[AnatoLens] Neuron model has not imported as a prefab: " + NeuronModelPath);
            return;
        }

        GameObject prefab = BuildNeuronPrefab(source);
        if (prefab == null)
            return;

        XRReferenceImageLibrary library = AssetDatabase.LoadAssetAtPath<XRReferenceImageLibrary>(LibraryPath);
        Texture2D neuronTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(NeuronMarkerPath);
        Texture2D femaleTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(FemaleMarkerPath);
        if (library == null || neuronTexture == null || femaleTexture == null)
        {
            Debug.LogError("[AnatoLens] Could not load MyImages.asset and both marker images. Check the asset paths.");
            return;
        }

        SetMarker(library, NeuronKey, neuronTexture, MarkerWidthMeters,
            NeuronMarkerHeightMeters);
        SetMarker(library, FemaleKey, femaleTexture, MarkerWidthMeters,
            FemaleMarkerHeightMeters);
        EditorUtility.SetDirty(library);

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        TrackedImageSpawner spawner = Object.FindObjectOfType<TrackedImageSpawner>(true);
        if (spawner == null)
        {
            Debug.LogError("[AnatoLens] Could not find TrackedImageSpawner in SampleScene.");
            return;
        }

        SetSceneMapping(spawner, NeuronKey, prefab);
        SetSceneMapping(spawner, FemaleKey, null);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[AnatoLens] Configured NeuronPrefab, Neuron marker and Female_Reproductive_system marker. " +
                  "Neuron target: 20 x " + (NeuronMarkerHeightMeters * 100f).ToString("F1") +
                  " cm. Female reproductive model is still missing; its marker is safely mapped to no prefab.");
    }

    private static GameObject BuildNeuronPrefab(GameObject source)
    {
        GameObject root = new GameObject("NeuronModel");
        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(source);
        model.name = "Neuron";
        model.transform.SetParent(root.transform, false);

        Bounds bounds = CalculateBounds(root.transform);
        float longest = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
        if (longest <= 0.00001f)
        {
            Object.DestroyImmediate(root);
            Debug.LogError("[AnatoLens] Neuron model has no visible renderer bounds.");
            return null;
        }

        model.transform.localScale *= NeuronLongestDimensionMeters / longest;
        bounds = CalculateBounds(root.transform);
        model.transform.localPosition -= bounds.center;

        root.AddComponent<TouchRotate>();
        PinchZoom zoom = root.AddComponent<PinchZoom>();
        zoom.minScale = 0.5f;
        zoom.maxScale = 2f;
        root.AddComponent<ResetHeart>();
        root.AddComponent<HeartTapDetector>();
        ModelInfo info = root.AddComponent<ModelInfo>();
        info.organName = "Neuron";

        bounds = CalculateBounds(root.transform);
        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.center = bounds.center;
        collider.size = bounds.size * 1.05f;

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, NeuronPrefabPath);
        Object.DestroyImmediate(root);
        return saved;
    }

    private static Bounds CalculateBounds(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bool found = false;
        Bounds result = new Bounds(Vector3.zero, Vector3.zero);
        foreach (Renderer renderer in renderers)
        {
            Bounds world = renderer.bounds;
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 corner = root.InverseTransformPoint(world.center +
                    Vector3.Scale(world.extents, new Vector3(x, y, z)));
                if (!found)
                {
                    result = new Bounds(corner, Vector3.zero);
                    found = true;
                }
                else result.Encapsulate(corner);
            }
        }
        return result;
    }

    private static void SetMarker(XRReferenceImageLibrary library, string key,
        Texture2D texture, float width, float height)
    {
        int index = -1;
        for (int i = 0; i < library.count; i++)
            if (library[i].name == key)
            {
                index = i;
                break;
            }
        if (index < 0)
        {
            library.Add();
            index = library.count - 1;
        }
        library.SetName(index, key);
        library.SetTexture(index, texture, true);
        library.SetSize(index, new Vector2(width, height));
        library.SetSpecifySize(index, true);
    }

    private static void SetSceneMapping(TrackedImageSpawner spawner, string key, GameObject prefab)
    {
        SerializedObject serialized = new SerializedObject(spawner);
        SerializedProperty mappings = serialized.FindProperty("imagePrefabs");
        for (int i = 0; i < mappings.arraySize; i++)
        {
            SerializedProperty item = mappings.GetArrayElementAtIndex(i);
            if (item.FindPropertyRelative("imageName").stringValue == key)
            {
                item.FindPropertyRelative("prefab").objectReferenceValue = prefab;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                return;
            }
        }
        int index = mappings.arraySize;
        mappings.InsertArrayElementAtIndex(index);
        SerializedProperty added = mappings.GetArrayElementAtIndex(index);
        added.FindPropertyRelative("imageName").stringValue = key;
        added.FindPropertyRelative("prefab").objectReferenceValue = prefab;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
