using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Rect de los 4 aros y de su raiz, para validar la formula de escala.</summary>
public static class AuraInspect4
{
    public static string Execute()
    {
        GameObject canvasGo = GameObject.Find("Canvas");
        Transform aros = canvasGo.transform.Find("Panel_AR") != null ? null : null;

        // ArosAR vive dentro del XR Origin.
        GameObject xr = GameObject.Find("XR Origin");
        Transform arosRaiz = xr != null ? xr.transform.Find("ArosAR") : null;

        StringBuilder sb = new StringBuilder();

        if (arosRaiz == null)
        {
            sb.AppendLine("ArosAR no encontrado bajo XR Origin");
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.name == "ArosAR")
                {
                    sb.AppendLine("encontrado en: " + Ruta(t));
                }
            }

            return sb.ToString();
        }

        sb.AppendLine("ArosAR ruta=" + Ruta(arosRaiz) + " localScale=" + arosRaiz.localScale);
        RectTransform raizRt = arosRaiz as RectTransform;
        if (raizRt != null)
        {
            sb.AppendLine("  sizeDelta=" + raizRt.sizeDelta + " rect=" + raizRt.rect.size);
        }

        for (int i = 0; i < arosRaiz.childCount; i++)
        {
            Transform hijo = arosRaiz.GetChild(i);
            RectTransform rt = hijo as RectTransform;
            sb.AppendLine("  " + hijo.name + " activo=" + hijo.gameObject.activeSelf +
                          " localScale=" + hijo.localScale +
                          (rt == null
                              ? ""
                              : " anc=" + rt.anchorMin + "/" + rt.anchorMax +
                                " sizeDelta=" + rt.sizeDelta + " pos=" + rt.anchoredPosition +
                                " rect=" + rt.rect.size));

            Image img = hijo.GetComponent<Image>();
            if (img != null)
            {
                sb.AppendLine("     img sprite=" + (img.sprite == null ? "none" : img.sprite.name) +
                              " preserveAspect=" + img.preserveAspect);
            }
        }

        return sb.ToString();
    }

    private static string Ruta(Transform t)
    {
        string ruta = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            ruta = t.name + "/" + ruta;
        }

        return ruta;
    }
}
