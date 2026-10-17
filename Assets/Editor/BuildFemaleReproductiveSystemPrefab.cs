using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.ARSubsystems;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARSubsystems;

public static class BuildFemaleReproductiveSystemPrefab
{
    private const string ModelPath = "Assets/Models/Female_Reproductive_system/female_reproductive_system.glb";
    private const string PrefabPath = "Assets/Prefabs/FemaleReproductiveSystemPrefab.prefab";
    private const string MarkerPath = "Assets/Female_Reproductive_system.png";
    private const string LibraryPath = "Assets/MyImages.asset";
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string ImageKey = "Female_Reproductive_system";
    private const string RunOnceTriggerPath = "Assets/BuildFemaleReproductiveSystem.once";
    private const float LongestDimensionMeters = 0.16f;

    [InitializeOnLoadMethod]
    private static void RunWhenRequested()
    {
        if (!File.Exists(RunOnceTriggerPath))
            return;

        EditorApplication.delayCall += () =>
        {
            File.Delete(RunOnceTriggerPath);
            if (File.Exists(RunOnceTriggerPath + ".meta"))
                File.Delete(RunOnceTriggerPath + ".meta");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Build();
        };
    }

    [MenuItem("AnatoLens/Build Female Reproductive System Prefab")]
    public static void Build()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (source == null)
        {
            Debug.LogError("[AnatoLens] Female reproductive GLB has not imported: " + ModelPath);
            return;
        }

        GameObject root = new GameObject("FemaleReproductiveSystemModel");
        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(source);
        model.name = "FemaleReproductiveSystem";
        model.transform.SetParent(root.transform, false);

        Bounds bounds = CalculateBounds(root.transform);
        float longest = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
        if (longest <= 0.00001f)
        {
            Object.DestroyImmediate(root);
            Debug.LogError("[AnatoLens] Female reproductive model has no visible renderer bounds.");
            return;
        }

        model.transform.localScale *= LongestDimensionMeters / longest;
        bounds = CalculateBounds(root.transform);
        model.transform.localPosition -= bounds.center;

        root.AddComponent<TouchRotate>();
        PinchZoom zoom = root.AddComponent<PinchZoom>();
        zoom.minScale = 0.5f;
        zoom.maxScale = 2f;
        root.AddComponent<ResetHeart>();
        root.AddComponent<HeartTapDetector>();
        ModelInfo info = root.AddComponent<ModelInfo>();
        info.organName = "Female reproductive system";

        bounds = CalculateBounds(root.transform);
        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.center = bounds.center;
        collider.size = bounds.size * 1.05f;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        if (prefab == null)
        {
            Debug.LogError("[AnatoLens] Could not save " + PrefabPath);
            return;
        }

        XRReferenceImageLibrary library = AssetDatabase.LoadAssetAtPath<XRReferenceImageLibrary>(LibraryPath);
        Texture2D marker = AssetDatabase.LoadAssetAtPath<Texture2D>(MarkerPath);
        if (library == null || marker == null)
        {
            Debug.LogError("[AnatoLens] Could not load the female marker or MyImages library.");
            return;
        }
        SetMarker(library, marker);

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        TrackedImageSpawner spawner = Object.FindObjectOfType<TrackedImageSpawner>(true);
        if (spawner == null)
        {
            Debug.LogError("[AnatoLens] Could not find TrackedImageSpawner in SampleScene.");
            return;
        }
        SetSceneMapping(spawner, prefab);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[AnatoLens] Built and mapped " + PrefabPath +
                  " with imported GLB materials and textures. Longest dimension: " +
                  LongestDimensionMeters.ToString("F2") + " m. Scan target name: " + ImageKey + ".");
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

    private static void SetMarker(XRReferenceImageLibrary library, Texture2D marker)
    {
        int index = -1;
        for (int i = 0; i < library.count; i++)
            if (library[i].name == ImageKey)
            {
                index = i;
                break;
            }
        if (index < 0)
        {
            library.Add();
            index = library.count - 1;
        }
        library.SetName(index, ImageKey);
        library.SetTexture(index, marker, true);
        // Keep the real 250:200 source-image aspect ratio at a 20 cm print width.
        library.SetSize(index, new Vector2(0.20f, 0.16f));
        library.SetSpecifySize(index, true);
        EditorUtility.SetDirty(library);
    }

    private static void SetSceneMapping(TrackedImageSpawner spawner, GameObject prefab)
    {
        SerializedObject serialized = new SerializedObject(spawner);
        SerializedProperty mappings = serialized.FindProperty("imagePrefabs");
        for (int i = 0; i < mappings.arraySize; i++)
        {
            SerializedProperty item = mappings.GetArrayElementAtIndex(i);
            if (item.FindPropertyRelative("imageName").stringValue == ImageKey)
            {
                item.FindPropertyRelative("prefab").objectReferenceValue = prefab;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                return;
            }
        }

        int index = mappings.arraySize;
        mappings.InsertArrayElementAtIndex(index);
        SerializedProperty added = mappings.GetArrayElementAtIndex(index);
        added.FindPropertyRelative("imageName").stringValue = ImageKey;
        added.FindPropertyRelative("prefab").objectReferenceValue = prefab;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
