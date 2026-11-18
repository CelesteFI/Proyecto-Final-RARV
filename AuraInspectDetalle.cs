using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class AuraInspectDetalle
{
    public static void Execute()
    {
        StringBuilder sb = new StringBuilder();

        GameObject panel = GameObject.Find("Canvas/Panel_GuiaNutricional/PanelDetalle");
        if (panel == null) { Debug.LogError("[DET] sin PanelDetalle"); return; }

        sb.AppendLine("[DET] PanelDetalle activo=" + panel.activeSelf);
        Dump(panel.transform, 0, sb);

        // Opciones del dropdown del Panel_AR
        GameObject xr = GameObject.Find("XR Origin");
        ARUIManager ui = xr.GetComponent<ARUIManager>();
        var so = new UnityEditor.SerializedObject(ui);
        var prop = so.FindProperty("dropdownAlimentos");
        TMP_Dropdown dd = prop.objectReferenceValue as TMP_Dropdown;

        if (dd != null)
        {
            sb.AppendLine("[DROPDOWN] " + dd.options.Count + " opciones:");
            for (int i = 0; i < dd.options.Count; i++)
            {
                sb.AppendLine("  " + i + " = '" + dd.options[i].text + "'");
            }
        }

        Debug.Log(sb.ToString());
    }

    private static void Dump(Transform t, int nivel, StringBuilder sb)
    {
        RectTransform rt = t as RectTransform;
        string sangria = new string(' ', nivel * 2);
        string rect = rt == null ? "" : " anchorMin=" + rt.anchorMin.ToString("F2") + " anchorMax=" + rt.anchorMax.ToString("F2") +
                      " pivot=" + rt.pivot.ToString("F2") + " anchoredPos=" + rt.anchoredPosition.ToString("F1") +
                      " size=" + rt.sizeDelta.ToString("F1") + " rect=" + rt.rect.width.ToString("F1") + "x" + rt.rect.height.ToString("F1");

        string extra = "";
        TMP_Text tmp = t.GetComponent<TMP_Text>();
        if (tmp != null)
        {
            extra = " TMP(fs=" + tmp.fontSize + " auto=" + tmp.enableAutoSizing + " wrap=" + tmp.enableWordWrapping + ") text='" + tmp.text.Replace("\n", "\\n") + "'";
        }
        Image img = t.GetComponent<Image>();
        if (img != null)
        {
            extra += " Image(color=" + img.color.ToString("F2") + " sprite=" + (img.sprite == null ? "NULL" : img.sprite.name) + ")";
        }
        Button b = t.GetComponent<Button>();
        if (b != null) extra += " Button";

        sb.AppendLine(sangria + t.name + " self=" + t.gameObject.activeSelf + rect + extra);

        for (int i = 0; i < t.childCount; i++)
        {
            Dump(t.GetChild(i), nivel + 1, sb);
        }
    }
}
