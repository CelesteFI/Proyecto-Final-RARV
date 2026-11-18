using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Volcado de estructura (RectTransform + textos) para auditar el layout de los paneles nuevos.
/// </summary>
public static class AuraInspect
{
    public static string Execute()
    {
        GameObject canvasGo = GameObject.Find("Canvas");
        Transform canvas = canvasGo.transform;

        StringBuilder sb = new StringBuilder();

        Volcar(sb, canvas.Find("Panel_GuiaNutricional"), 0, 4);
        Volcar(sb, canvas.Find("Panel_GuiaNutricional/PanelDetalle"), 0, 5);
        Volcar(sb, canvas.Find("Panel_DashboardMetas"), 0, 4);
        Volcar(sb, canvas.Find("Panel_Tutorial"), 0, 4);

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
              " anc=" + F(rt.anchorMin) + "/" + F(rt.anchorMax) +
              " piv=" + F(rt.pivot);

        string extra = "";

        TextMeshProUGUI tmp = t.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            extra += " TMP[\"" + tmp.text + "\" fs=" + tmp.fontSize + " align=" + tmp.alignment +
                     " wrap=" + tmp.enableWordWrapping + " color=" + ColorUtility.ToHtmlStringRGB(tmp.color) + "]";
        }

        Image img = t.GetComponent<Image>();
        if (img != null)
        {
            extra += " IMG[sprite=" + (img.sprite == null ? "none" : img.sprite.name) +
                     " color=" + ColorUtility.ToHtmlStringRGB(img.color) +
                     " type=" + img.type + " raycast=" + img.raycastTarget + "]";
        }

        LayoutGroup lg = t.GetComponent<LayoutGroup>();
        if (lg != null)
        {
            extra += " LG[" + lg.GetType().Name + "]";
        }

        GridLayoutGroup grid = t.GetComponent<GridLayoutGroup>();
        if (grid != null)
        {
            extra += " GRID[cell=" + F(grid.cellSize) + " sp=" + F(grid.spacing) +
                     " cols=" + grid.constraintCount + "]";
        }

        Button btn = t.GetComponent<Button>();
        if (btn != null)
        {
            extra += " BTN";
        }

        sb.AppendLine(sangria + t.name + " activeSelf=" + t.gameObject.activeSelf + rect + extra);

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
