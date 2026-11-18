using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;

public static class AuraDiagGraph
{
    public static void Execute()
    {
        StringBuilder sb = new StringBuilder();

        // 1) Raices de la escena
        sb.AppendLine("[RAICES]");
        var escena = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var raices = escena.GetRootGameObjects();
        for (int i = 0; i < raices.Length; i++)
        {
            sb.AppendLine("  " + raices[i].name + " [" + raices[i].activeSelf + "] comps=" + Lista(raices[i]));
        }

        // 2) ARSession
        sb.AppendLine("[ARSession] " + (Object.FindFirstObjectByType<ARSession>(FindObjectsInactive.Include) == null ? "NO EXISTE" : "presente"));

        // 3) Camara: culling mask y background
        Camera cam = Camera.main;
        if (cam != null)
        {
            sb.AppendLine("[CAMARA] " + cam.name + " cullingMask=" + cam.cullingMask + " clearFlags=" + cam.clearFlags + " near=" + cam.nearClipPlane + " far=" + cam.farClipPlane);
        }

        // 4) Pipeline: renderer features del URP
        var urpAsset = GraphicsSettings.currentRenderPipeline;
        sb.AppendLine("[RP] " + (urpAsset == null ? "Built-in" : urpAsset.name + " (" + urpAsset.GetType().Name + ")"));
        if (urpAsset != null)
        {
            var so = new SerializedObject(urpAsset);
            var lista = so.FindProperty("m_RendererDataList");
            if (lista != null)
            {
                for (int i = 0; i < lista.arraySize; i++)
                {
                    var rd = lista.GetArrayElementAtIndex(i).objectReferenceValue;
                    if (rd == null) continue;
                    sb.AppendLine("  renderer[" + i + "] = " + rd.name);
                    Features(sb, rd, "    ");
                }
            }
        }

        Debug.Log(sb.ToString());
    }

    private static void Features(StringBuilder sb, Object rd, string sangria)
    {
        var so = new SerializedObject(rd);
        var it = so.GetIterator();
        while (it.NextVisible(true))
        {
            if (it.propertyType == SerializedPropertyType.ObjectReference && it.propertyPath.Contains("RendererFeature"))
            {
                sb.AppendLine(sangria + it.propertyPath + " = " + (it.objectReferenceValue == null ? "<vacio>" : it.objectReferenceValue.name));
            }
            if (it.propertyType == SerializedPropertyType.Generic && it.name.ToLower().Contains("feature") && it.isArray)
            {
                for (int j = 0; j < it.arraySize; j++)
                {
                    var el = it.GetArrayElementAtIndex(j);
                    sb.AppendLine(sangria + it.propertyPath + "[" + j + "] = " + (el.objectReferenceValue == null ? "<vacio>" : el.objectReferenceValue.name));
                }
            }
        }
    }

    private static string Lista(GameObject go)
    {
        var comps = go.GetComponents<Component>();
        var nombres = new System.Text.StringBuilder();
        for (int i = 0; i < comps.Length; i++)
        {
            if (i > 0) nombres.Append(",");
            nombres.Append(comps[i] == null ? "MISSING" : comps[i].GetType().Name);
        }
        return nombres.ToString();
    }
}
