using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Diagnostico: posicion real de Btn_Volver y sprites de iconos del Panel_AR.</summary>
public static class AuraInspectSprites
{
    public static string Execute()
    {
        StringBuilder sb = new StringBuilder();

        GameObject canvasGo = GameObject.Find("Canvas");
        Transform perfil = canvasGo.transform.Find("Panel_Perfil");
        Transform volver = perfil.Find("Btn_Volver");
        RectTransform vrt = volver as RectTransform;

        Vector3[] esquinas = new Vector3[4];
        vrt.GetWorldCorners(esquinas);
        sb.AppendLine("Btn_Volver world corners:");
        foreach (Vector3 e in esquinas)
        {
            sb.AppendLine("   " + e);
        }

        sb.AppendLine("Btn_Volver rect(local) " + vrt.rect + " activo " + volver.gameObject.activeInHierarchy);

        foreach (Transform t in perfil)
        {
            if (t.name.ToLowerInvariant().Contains("volver"))
            {
                sb.AppendLine("   match en Panel_Perfil: " + t.name);
            }
        }

        // Iconos del dropdown
        Transform panelAr = canvasGo.transform.Find("Panel_AR");
        sb.AppendLine("Panel_AR: " + (panelAr != null));

        Transform dd = panelAr.Find("PanelSuperior/dropdownAlimentos");
        sb.AppendLine("dropdown ruta 'PanelSuperior/dropdownAlimentos': " + (dd != null));

        if (dd == null)
        {
            dd = Buscar(panelAr, "dropdownAlimentos");
            sb.AppendLine("dropdown encontrado por busqueda: " + (dd != null ? Ruta(dd) : "no"));
        }

        if (dd != null)
        {
            TMP_Dropdown dropdown = dd.GetComponent<TMP_Dropdown>();
            sb.AppendLine("captionImage: " + (dropdown.captionImage != null ? dropdown.captionImage.name : "None") +
                          " sprite " + (dropdown.captionImage != null && dropdown.captionImage.sprite != null ? dropdown.captionImage.sprite.name : "None") +
                          " color " + (dropdown.captionImage != null ? dropdown.captionImage.color.ToString() : "-"));
            sb.AppendLine("itemImage: " + (dropdown.itemImage != null ? dropdown.itemImage.name : "None") +
                          " sprite " + (dropdown.itemImage != null && dropdown.itemImage.sprite != null ? dropdown.itemImage.sprite.name : "None"));

            for (int i = 0; i < dropdown.options.Count; i++)
            {
                Sprite s = dropdown.options[i].image;
                sb.AppendLine("   opcion " + i + " '" + dropdown.options[i].text + "' sprite " + (s != null ? s.name : "None"));
            }

            Transform capIcon = dd.Find("Caption Icon");
            if (capIcon != null)
            {
                Image img = capIcon.GetComponent<Image>();
                sb.AppendLine("Caption Icon: sprite " + (img != null && img.sprite != null ? img.sprite.name : "None") +
                              " color " + (img != null ? img.color.ToString() : "-") + " rt " + ((RectTransform)capIcon).sizeDelta);
            }
            else
            {
                sb.AppendLine("Caption Icon: NO EXISTE");
            }

            Transform flecha = dd.Find("Arrow");
            if (flecha != null)
            {
                Image img = flecha.GetComponent<Image>();
                sb.AppendLine("Arrow: sprite " + (img != null && img.sprite != null ? img.sprite.name : "None") +
                              " color " + (img != null ? img.color.ToString() : "-"));
            }
        }

        Transform metas = Buscar(panelAr, "PanelMetas");
        if (metas != null)
        {
            Transform icono = metas.Find("IconoMetas");
            Image img = icono != null ? icono.GetComponent<Image>() : null;
            sb.AppendLine("IconoMetas: " + (icono != null) + " sprite " + (img != null && img.sprite != null ? img.sprite.name : "None") +
                          " color " + (img != null ? img.color.ToString() : "-"));
        }
        else
        {
            sb.AppendLine("PanelMetas no encontrado");
        }

        string[] iconos = AssetDatabase.FindAssets("t:Sprite Icono_", new[] { "Assets/Sprites/IconosAlimentos" });
        sb.AppendLine("Iconos importados: " + iconos.Length);

        foreach (string guid in iconos)
        {
            sb.AppendLine("   " + AssetDatabase.GUIDToAssetPath(guid));
        }

        return sb.ToString();
    }

    private static Transform Buscar(Transform raiz, string nombre)
    {
        foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == nombre)
            {
                return t;
            }
        }

        return null;
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
