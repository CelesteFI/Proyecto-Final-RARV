using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Detalle interno del dropdown de alimentos, sus opciones y el recuadro de metas del Panel_AR.</summary>
public static class AuraInspect3
{
    public static string Execute()
    {
        GameObject canvasGo = GameObject.Find("Canvas");
        Transform canvas = canvasGo.transform;

        StringBuilder sb = new StringBuilder();

        Transform panelAR = canvas.Find("Panel_AR");
        Transform dropdown = panelAR.Find("PanelSuperior/dropdownAlimentos");

        Volcar(sb, dropdown, 0, 7);

        TMP_Dropdown dd = dropdown != null ? dropdown.GetComponent<TMP_Dropdown>() : null;
        if (dd != null)
        {
            sb.AppendLine("OPCIONES=" + dd.options.Count + " valor=" + dd.value +
                          " captionImage=" + (dd.captionImage == null ? "NONE" : dd.captionImage.name) +
                          " itemImage=" + (dd.itemImage == null ? "NONE" : dd.itemImage.name) +
                          " itemText=" + (dd.itemText == null ? "NONE" : dd.itemText.name) +
                          " captionText=" + (dd.captionText == null ? "NONE" : dd.captionText.name));

            for (int i = 0; i < dd.options.Count; i++)
            {
                sb.AppendLine("  [" + i + "] '" + dd.options[i].text + "' img=" +
                              (dd.options[i].image == null ? "none" : dd.options[i].image.name));
            }
        }

        sb.AppendLine();
        Volcar(sb, panelAR.Find("PanelSuperior"), 0, 1);
        Volcar(sb, panelAR.Find("PanelMetas"), 0, 3);

        return sb.ToString();
    }

    private static void Volcar(StringBuilder sb, Transform t, int nivel, int maxNivel)
    {
        if (t == null)
        {
            sb.AppendLine("(no encontrado)");
            return;
        }

        string sangria = new string(' ', nivel * 2);
        RectTransform rt = t as RectTransform;
        string rect = rt == null
            ? ""
            : " pos=" + F(rt.anchoredPosition) + " size=" + F(rt.sizeDelta) +
              " anc=" + F(rt.anchorMin) + "/" + F(rt.anchorMax);

        string extra = "";

        TextMeshProUGUI tmp = t.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            extra += " TMP[\"" + tmp.text.Replace("\n", " | ") + "\" fs=" + tmp.fontSize + "]";
        }

        Image img = t.GetComponent<Image>();
        if (img != null)
        {
            extra += " IMG[" + (img.sprite == null ? "none" : img.sprite.name) + " " +
                     ColorUtility.ToHtmlStringRGB(img.color) + "]";
        }

        if (t.GetComponent<ScrollRect>() != null) { extra += " SCROLL"; }
        if (t.GetComponent<Toggle>() != null) { extra += " TOGGLE"; }

        sb.AppendLine(sangria + t.name + " activo=" + t.gameObject.activeSelf + rect + extra);

        if (nivel >= maxNivel)
        {
            return;
        }

        for (int i = 0; i < t.childCount; i++)
        {
            Volcar(sb, t.GetChild(i), nivel + 1, maxNivel);
        }
    }

    private static string F(Vector2 v)
    {
        return "(" + v.x.ToString("0.##") + "," + v.y.ToString("0.##") + ")";
    }
}
