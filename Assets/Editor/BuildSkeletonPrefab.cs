using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.ARSubsystems;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.SceneManagement;
using UnityEditorInternal;

public static class BuildSkeletonPrefab
{
    private const string ModelPath = "Assets/Models/Skeleton/source/Skeleton.obj";
    private const string PrefabPath = "Assets/Prefabs/SkeletonPrefab.prefab";
    private const string MarkerPath = "Assets/Skeleton.png";
    private const string ImageLibraryPath = "Assets/MyImages.asset";
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("AnatoLens/Build Generated Skeleton Prefab")]
    public static void Build()
    {
        AssetDatabase.Refresh();
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (source == null)
        {
            Debug.LogError("[AnatoLens] Skeleton OBJ has not imported yet: " + ModelPath);
            return;
        }

        GameObject root = new GameObject("SkeletonModel");
        GameObject mesh = (GameObject)PrefabUtility.InstantiatePrefab(source);
        mesh.name = "Skeleton";
        mesh.transform.SetParent(root.transform, false);
        mesh.transform.localScale = Vector3.one * 0.25f;

        root.AddComponent<TouchRotate>();
        PinchZoom zoom = root.AddComponent<PinchZoom>();
        zoom.minScale = 0.5f;
        zoom.maxScale = 2f;
        root.AddComponent<ResetHeart>();
        root.AddComponent<HeartTapDetector>();
        ModelInfo info = root.AddComponent<ModelInfo>();
        info.organName = "Skeleton";

        Bounds bounds = CalculateBounds(root.transform);
        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.center = bounds.center;
        collider.size = bounds.size * 1.05f;

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AddSkeletonMarker();
        AddSceneMapping();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[AnatoLens] Prepared " + PrefabPath +
                  " and Skeleton marker. Print the target at 20 x 23.33 cm and verify tracking on device.");
    }

    private static void AddSkeletonMarker()
    {
        Texture2D marker = AssetDatabase.LoadAssetAtPath<Texture2D>(MarkerPath);
        XRReferenceImageLibrary library =
            AssetDatabase.LoadAssetAtPath<XRReferenceImageLibrary>(ImageLibraryPath);
        if (marker == null || library == null)
        {
            Debug.LogError("[AnatoLens] Could not load Skeleton.png or MyImages.asset.");
            return;
        }

        int index = -1;
        for (int i = 0; i < library.count; i++)
            if (library[i].name == "Skeleton")
            {
                index = i;
                break;
            }
        if (index < 0)
        {
            library.Add();
            index = library.count - 1;
        }

        library.SetName(index, "Skeleton");
        library.SetTexture(index, marker, true);
        library.SetSize(index, new Vector2(0.20f, 0.233333f));
        library.SetSpecifySize(index, true);
        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
    }

    private static Bounds CalculateBounds(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds result = new Bounds(Vector3.zero, Vector3.zero);
        foreach (Renderer renderer in renderers)
        {
            Bounds b = renderer.bounds;
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 corner = root.InverseTransformPoint(b.center +
                    Vector3.Scale(b.extents, new Vector3(x, y, z)));
                if (!hasBounds)
                {
                    result = new Bounds(corner, Vector3.zero);
                    hasBounds = true;
                }
                else result.Encapsulate(corner);
            }
        }
        return result;
    }

    private static void AddSceneMapping()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        TrackedImageSpawner spawner = Object.FindObjectOfType<TrackedImageSpawner>(true);
        if (spawner == null)
        {
            Debug.LogError("[AnatoLens] Could not find TrackedImageSpawner in SampleScene.");
            return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        SerializedObject serialized = new SerializedObject(spawner);
        SerializedProperty mappings = serialized.FindProperty("imagePrefabs");
        for (int i = 0; i < mappings.arraySize; i++)
        {
            SerializedProperty item = mappings.GetArrayElementAtIndex(i);
            if (item.FindPropertyRelative("imageName").stringValue == "Skeleton")
            {
                item.FindPropertyRelative("prefab").objectReferenceValue = prefab;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.SaveScene(scene);
                return;
            }
        }

        int index = mappings.arraySize;
        mappings.InsertArrayElementAtIndex(index);
        SerializedProperty added = mappings.GetArrayElementAtIndex(index);
        added.FindPropertyRelative("imageName").stringValue = "Skeleton";
        added.FindPropertyRelative("prefab").objectReferenceValue = prefab;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
