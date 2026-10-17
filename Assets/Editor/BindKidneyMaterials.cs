using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class BindKidneyMaterials
{
    const string ModelPath = "Assets/Models/Kidney/kidney/source/kidney.fbx";
    const string MaterialsPath = "Assets/Models/Kidney/kidney/Materials";
    const string PrefabPath = "Assets/Prefabs/KidneyPrefab.prefab";
    const string ReportPath = "Assets/KidneyMaterialCheck.txt";
    const string StageKey = "ARApp.KidneyMaterialBindingStageV2";

    static BindKidneyMaterials()
    {
        if (SessionState.GetInt(StageKey, 0) < 2)
            EditorApplication.delayCall += Run;
    }

    [MenuItem("Tools/AR App/Bind Kidney Materials")]
    static void Run()
    {
        if (SessionState.GetInt(StageKey, 0) >= 2) return;
        var report = new StringBuilder();
        var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        if (importer == null) throw new System.InvalidOperationException("Kidney FBX importer not found.");

        var external = AssetDatabase.FindAssets("t:Material", new[] { MaterialsPath })
            .Select(guid => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(material => material != null)
            .ToDictionary(material => Normalize(material.name));
        var sourceMaterials = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Material>().ToArray();
        report.AppendLine("FBX source materials: " + sourceMaterials.Length);
        foreach (var source in sourceMaterials)
        {
            report.AppendLine("Source: " + source.name);
            var mappedName = GetExternalMaterialName(source.name);
            if (external.TryGetValue(Normalize(mappedName), out var target))
            {
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), source.name), target);
                report.AppendLine("  mapped to " + AssetDatabase.GetAssetPath(target));
            }
            else if (mappedName == source.name)
                report.AppendLine("  retaining FBX material (no external replacement requested)");
            else
                report.AppendLine("  NO MATERIAL FOUND named " + mappedName);
        }

        if (SessionState.GetInt(StageKey, 0) == 0)
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
                report.AppendLine(renderer.name + " slots=" + slots.Length);
                for (int i = 0; i < slots.Length; i++)
                {
                    if (slots[i] == null) continue;
                    var mappedMaterial = FindMaterial(GetExternalMaterialName(slots[i].name));
                    if (mappedMaterial != null) slots[i] = mappedMaterial;
                    report.AppendLine("  " + slots[i].name + " | " + AssetDatabase.GetAssetPath(slots[i]) + " | shader=" + (slots[i].shader == null ? "NULL" : slots[i].shader.name) + " | albedo=" + slots[i].GetTexture("_MainTex") + " | tint=" + slots[i].GetColor("_Color"));
                }
                renderer.sharedMaterials = slots;
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
        Debug.Log("Kidney materials explicitly rebound and verified. See " + ReportPath);
    }

    static Material FindMaterial(string sourceName)
    {
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { MaterialsPath });
        foreach (string guid in guids)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (material != null && Normalize(material.name) == Normalize(sourceName)) return material;
        }
        return null;
    }

    static string GetExternalMaterialName(string sourceName)
    {
        string normalized = Normalize(sourceName);
        if (normalized.EndsWith("capsule2")) return "Capsule 1";
        if (normalized.EndsWith("cortexdiffuse")) return "Cortex Diffuse";
        if (normalized.EndsWith("renalureterdiffuse")) return "Renal & Ureter Diffuse";
        if (normalized.StartsWith("maya201803")) return "Medulla pyramid Diffuse";
        return sourceName;
    }

    static string Normalize(string value) => new string(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
}
