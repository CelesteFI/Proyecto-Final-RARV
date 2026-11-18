using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public static class AuraDiagAR
{
    public static void Execute()
    {
        StringBuilder sb = new StringBuilder();

        var anchors = UnityEngine.Object.FindObjectsByType<PlateAnchorManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        sb.AppendLine("=== PlateAnchorManager encontrados: " + anchors.Length + " ===");

        foreach (var a in anchors)
        {
            sb.AppendLine("-- en objeto: " + Ruta(a.gameObject) + " | enabled=" + a.enabled + " | activeInHierarchy=" + a.gameObject.activeInHierarchy);
            DumpFields(sb, a);
        }

        var cam = UnityEngine.Object.FindObjectsByType<ARCameraController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in cam)
        {
            sb.AppendLine("=== ARCameraController en " + Ruta(c.gameObject) + " enabled=" + c.enabled + " ARVisible=" + c.ARVisible + " ===");
            DumpFields(sb, c);
        }

        var cal = UnityEngine.Object.FindObjectsByType<PlateCalibrationController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in cal)
        {
            sb.AppendLine("=== PlateCalibrationController en " + Ruta(c.gameObject) + " active=" + c.gameObject.activeSelf + " ===");
            DumpFields(sb, c);
        }

        var ui = UnityEngine.Object.FindObjectsByType<ARUIManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var u in ui)
        {
            sb.AppendLine("=== ARUIManager en " + Ruta(u.gameObject) + " enabled=" + u.enabled + " ===");
            DumpFields(sb, u);
        }

        Debug.Log(sb.ToString());

        // Arbol completo bajo XR Origin
        var xr = GameObject.Find("XR Origin");
        if (xr != null)
        {
            StringBuilder arbol = new StringBuilder();
            Arbol(xr, 0, arbol);
            Debug.Log("[ARBOL XR Origin]\n" + arbol);
        }
        else
        {
            Debug.Log("[ARBOL] No se encontro 'XR Origin' en la escena.");
        }
    }

    private static void Arbol(GameObject go, int nivel, StringBuilder sb)
    {
        string sangria = new string(' ', nivel * 2);
        sb.AppendLine(sangria + go.name + "  [self=" + go.activeSelf + " hier=" + go.activeInHierarchy +
                      " pos=" + go.transform.position.ToString("F3") + " scale=" + go.transform.localScale.ToString("F3") + "]  comps: " + Comps(go));
        for (int i = 0; i < go.transform.childCount; i++)
        {
            Arbol(go.transform.GetChild(i).gameObject, nivel + 1, sb);
        }
    }

    private static string Comps(GameObject go)
    {
        var comps = go.GetComponents<Component>();
        var nombres = new List<string>();
        foreach (var c in comps)
        {
            nombres.Add(c == null ? "MISSING" : c.GetType().Name);
        }
        return string.Join(",", nombres.ToArray());
    }

    private static string Ruta(GameObject go)
    {
        string ruta = go.name;
        Transform t = go.transform.parent;
        while (t != null)
        {
            ruta = t.name + "/" + ruta;
            t = t.parent;
        }
        return ruta;
    }

    private static void DumpFields(StringBuilder sb, Component comp)
    {
        var so = new UnityEditor.SerializedObject(comp);
        var it = so.GetIterator();
        bool entrar = true;
        while (it.NextVisible(entrar))
        {
            entrar = false;
            if (it.propertyType == UnityEditor.SerializedPropertyType.ObjectReference)
            {
                var obj = it.objectReferenceValue;
                string extra = "";
                if (obj is GameObject g)
                {
                    extra = " -> " + Ruta(g) + " [self=" + g.activeSelf + " hier=" + g.activeInHierarchy +
                            " pos=" + g.transform.position.ToString("F4") + " scale=" + g.transform.localScale.ToString("F4") + "]";
                }
                else if (obj is Component c2)
                {
                    extra = " -> " + Ruta(c2.gameObject) + " (" + c2.GetType().Name + ")";
                }

                sb.AppendLine("   " + it.propertyPath + " = " + (obj == null ? "<NULL>" : obj.name) + extra);
            }
            else if (it.propertyType == UnityEditor.SerializedPropertyType.Vector3 ||
                     it.propertyType == UnityEditor.SerializedPropertyType.Vector2 ||
                     it.propertyType == UnityEditor.SerializedPropertyType.Float ||
                     it.propertyType == UnityEditor.SerializedPropertyType.Boolean ||
                     it.propertyType == UnityEditor.SerializedPropertyType.String ||
                     it.propertyType == UnityEditor.SerializedPropertyType.Enum ||
                     it.propertyType == UnityEditor.SerializedPropertyType.Integer)
            {
                sb.AppendLine("   " + it.propertyPath + " = " + it.displayName + " : " + Valor(it));
            }
        }
    }

    private static string Valor(UnityEditor.SerializedProperty it)
    {
        switch (it.propertyType)
        {
            case UnityEditor.SerializedPropertyType.Float: return it.floatValue.ToString("F4");
            case UnityEditor.SerializedPropertyType.Integer: return it.intValue.ToString();
            case UnityEditor.SerializedPropertyType.Boolean: return it.boolValue.ToString();
            case UnityEditor.SerializedPropertyType.String: return it.stringValue;
            case UnityEditor.SerializedPropertyType.Enum: return it.enumValueIndex.ToString();
            case UnityEditor.SerializedPropertyType.Vector2: return it.vector2Value.ToString("F3");
            case UnityEditor.SerializedPropertyType.Vector3: return it.vector3Value.ToString("F3");
            default: return it.propertyType.ToString();
        }
    }
}
