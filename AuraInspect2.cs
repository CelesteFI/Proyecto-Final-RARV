using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Volcado de los paneles que hay que reestructurar (AR y Perfil).</summary>
public static class AuraInspect2
{
    public static string Execute()
    {
        GameObject canvasGo = GameObject.Find("Canvas");
        Transform canvas = canvasGo.transform;

        StringBuilder sb = new StringBuilder();

        Volcar(sb, canvas.Find("Panel_AR"), 0, 4);
        Volcar(sb, canvas.Find("Panel_Perfil"), 0, 4);
        Volcar(sb, canvas.Find("Panel_MenuPrincipal"), 0, 2);

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
              " anc=" + F(rt.anchorMin) + "/" + F(rt.anchorMax) + " piv=" + F(rt.pivot);

        string extra = "";

        TextMeshProUGUI tmp = t.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            extra += " TMP[\"" + Corte(tmp.text) + "\" fs=" + tmp.fontSize + "]";
        }

        Image img = t.GetComponent<Image>();
        if (img != null)
        {
            extra += " IMG[" + (img.sprite == null ? "none" : img.sprite.name) + " " +
                     ColorUtility.ToHtmlStringRGB(img.color) + "]";
        }

        if (t.GetComponent<Slider>() != null) { extra += " SLIDER"; }
        if (t.GetComponent<TMP_Dropdown>() != null) { extra += " DROPDOWN"; }
        if (t.GetComponent<Toggle>() != null) { extra += " TOGGLE"; }
        if (t.GetComponent<Button>() != null) { extra += " BTN"; }
        if (t.GetComponent<CanvasGroup>() != null) { extra += " CG"; }

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

    private static string Corte(string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return "";
        }

        s = s.Replace("\n", " | ");
        return s.Length <= 60 ? s : s.Substring(0, 60) + "...";
    }

    private static string F(Vector2 v)
    {
        return "(" + v.x.ToString("0.##") + "," + v.y.ToString("0.##") + ")";
    }
}
