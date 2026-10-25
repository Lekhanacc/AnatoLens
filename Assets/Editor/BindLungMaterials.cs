using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class BindLungMaterials
{
    const string ModelPath = "Assets/Models/Lung/source/Lung_with_Holes_Final.fbx";
    const string PrefabPath = "Assets/Prefabs/LungPrefab.prefab";
    const string MaterialsPath = "Assets/Models/Lung/Materials";
    const string ReportPath = "Assets/LungMaterialCheck.txt";
    const string StageKey = "ARApp.LungMaterialBindingStageV1";

    static BindLungMaterials()
    {
        if (SessionState.GetInt(StageKey, 0) < 2)
            EditorApplication.delayCall += Run;
    }

    [MenuItem("Tools/AR App/Bind Lung Materials")]
    static void Run()
    {
        int stage = SessionState.GetInt(StageKey, 0);
        if (stage >= 2) return;

        var report = new StringBuilder();
        EnsureFolder();
        EnsureNormalMap("Assets/Models/Lung/textures/Respiratoy_Lungs_Bump.jpg");
        EnsureNormalMap("Assets/Models/Lung/textures/Respiratoy_Larynx_Bump.jpeg");
        EnsureNormalMap("Assets/Models/Lung/textures/Respiratoy_Trachea_Bump.jpeg");

        var lung = CreateOrUpdateMaterial(
            "Male_Anatomy_Pack_Complete_V05_Male_Respiratory_System_V05_Respiratoy_Lung_mat",
            "Assets/Models/Lung/textures/Lung_with_Holes_Male_Anatomy_Pack_Complete_V05_Male_Respirat.png",
            "Assets/Models/Lung/textures/Respiratoy_Lungs_Bump.jpg");
        var larynx = CreateOrUpdateMaterial(
            "Male_Anatomy_Pack_Complete_V05_Male_Respiratory_System_V05_Respiratory_Cricothyroid_Ligament_mat",
            "Assets/Models/Lung/textures/Respiratoy_Larynx_Dif.jpeg",
            "Assets/Models/Lung/textures/Respiratoy_Larynx_Bump.jpeg");
        var bronchi = CreateOrUpdateMaterial(
            "Male_Anatomy_Pack_Complete_V05_Male_Respiratory_System_V05_Respiratoy_Bronchi_mat",
            "Assets/Models/Lung/textures/Respiratoy_Trachea_Dif.jpeg",
            "Assets/Models/Lung/textures/Respiratoy_Trachea_Bump.jpeg");
        AssetDatabase.SaveAssets();

        var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        if (importer == null)
            throw new System.InvalidOperationException("Lung FBX importer not found at " + ModelPath);

        var external = new[] { lung, larynx, bronchi }
            .ToDictionary(material => Normalize(material.name));
        var sourceMaterials = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
            .OfType<Material>().ToArray();
        report.AppendLine("FBX source materials: " + sourceMaterials.Length);
        foreach (var source in sourceMaterials)
        {
            report.AppendLine("Source: " + source.name);
            if (external.TryGetValue(Normalize(source.name), out var target))
            {
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), source.name), target);
                report.AppendLine("  mapped to " + AssetDatabase.GetAssetPath(target));
            }
            else
            {
                report.AppendLine("  no matching external material");
            }
        }

        if (stage == 0)
        {
            SessionState.SetInt(StageKey, 1);
            importer.SaveAndReimport();
            EditorApplication.delayCall += Run;
            return;
        }

        importer.SaveAndReimport();
        AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceUpdate);
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var slots = renderer.sharedMaterials;
                for (int i = 0; i < slots.Length; i++)
                {
                    if (slots[i] == null) continue;
                    string sourceName = Normalize(slots[i].name);
                    if (sourceName.EndsWith("respiratoylungmat")) slots[i] = lung;
                    else if (sourceName.EndsWith("respiratoybronchimat")) slots[i] = bronchi;
                    else if (sourceName.EndsWith("respiratorycricothyroidligamentmat")) slots[i] = larynx;
                }
                renderer.sharedMaterials = slots;
                foreach (var material in slots)
                    if (material != null)
                        report.AppendLine(renderer.name + " -> " + material.name +
                            " | albedo=" + (material.mainTexture == null ? "NULL" : AssetDatabase.GetAssetPath(material.mainTexture)) +
                            " | normal=" + (material.HasProperty("_BumpMap") && material.GetTexture("_BumpMap") != null ? AssetDatabase.GetAssetPath(material.GetTexture("_BumpMap")) : "none"));
            }
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        File.WriteAllText(ReportPath, report.ToString());
        SessionState.SetInt(StageKey, 2);
        AssetDatabase.Refresh();
        Debug.Log("Lung materials explicitly rebound to their color maps. See " + ReportPath);
    }

    static Material CreateOrUpdateMaterial(string name, string albedoPath, string normalPath)
    {
        var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
        if (albedo == null)
            throw new System.InvalidOperationException("Lung albedo texture not found: " + albedoPath);
        var shader = Shader.Find("Standard");
        if (shader == null)
            throw new System.InvalidOperationException("Unity Standard shader is not available.");

        string materialPath = MaterialsPath + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, materialPath);
        }
        else
        {
            material.shader = shader;
        }

        material.color = Color.white;
        material.mainTexture = albedo;
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Glossiness", 0.18f);
        var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
        if (normal != null)
        {
            material.SetTexture("_BumpMap", normal);
            material.EnableKeyword("_NORMALMAP");
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    static void EnsureNormalMap(string path)
    {
        if (AssetImporter.GetAtPath(path) is TextureImporter importer &&
            importer.textureType != TextureImporterType.NormalMap)
        {
            importer.textureType = TextureImporterType.NormalMap;
            importer.SaveAndReimport();
        }
    }

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Models/Lung"))
            throw new System.InvalidOperationException("Lung asset folder not found.");
        if (!AssetDatabase.IsValidFolder(MaterialsPath))
            AssetDatabase.CreateFolder("Assets/Models/Lung", "Materials");
    }

    static string Normalize(string value) =>
        new string(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
}
