using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>Comprobacion final de los cuatro puntos de la reestructuracion.</summary>
public static class AuraFinalCheck
{
    public static string Execute()
    {
        StringBuilder sb = new StringBuilder();
        GameObject canvasGo = GameObject.Find("Canvas");

        // 1. Aros conectados.
        PlateAnchorManager pam = Object.FindObjectOfType<PlateAnchorManager>(true);
        SerializedObject soAros = new SerializedObject(pam);
        int aros = 0;

        foreach (string campo in new[] { "spriteGuiaCircular", "spriteFijadoCircular", "spriteGuiaCuadrado", "spriteFijadoCuadrado" })
        {
            SerializedProperty prop = soAros.FindProperty(campo);

            if (prop != null && prop.objectReferenceValue != null)
            {
                aros++;
            }
        }

        sb.AppendLine("1) Aros con referencia serializada: " + aros + "/4");

        // 2. Dropdown con miniaturas y boton Servir en el Panel_AR.
        Transform panelAr = canvasGo.transform.Find("Panel_AR");
        TMP_Dropdown dd = panelAr.Find("PanelSuperior/dropdownAlimentos").GetComponent<TMP_Dropdown>();
        int conMiniatura = 0;

        foreach (TMP_Dropdown.OptionData opcion in dd.options)
        {
            if (opcion.image != null)
            {
                conMiniatura++;
            }
        }

        sb.AppendLine("2) Dropdown: " + dd.options.Count + " opciones, " + conMiniatura + " con miniatura; " +
                      "itemImage " + (dd.itemImage != null) + "; caption con miniatura " + (dd.captionImage != null && dd.captionImage.sprite != null));

        Button servir = panelAr.Find("PanelSuperior/Btn_ServirPlato") != null
            ? panelAr.Find("PanelSuperior/Btn_ServirPlato").GetComponent<Button>()
            : null;

        sb.AppendLine("   Btn_ServirPlato en el PanelSuperior: " + (servir != null) +
                      " | listeners " + (servir != null ? servir.onClick.GetPersistentEventCount() : 0));

        // 3. Panel Guia: sin boton de servir.
        Transform guia = canvasGo.transform.Find("Panel_GuiaNutricional");
        int servirEnGuia = 0;

        foreach (Button b in guia.GetComponentsInChildren<Button>(true))
        {
            string nombre = b.gameObject.name.ToLowerInvariant();

            if (nombre.Contains("servir"))
            {
                servirEnGuia++;
                sb.AppendLine("   BOTON DE SERVIR EN GUIA: " + Ruta(b.transform));
            }
        }

        sb.AppendLine("3) Panel Guia: botones de servir encontrados = " + servirEnGuia +
                      " | Botones totales " + guia.GetComponentsInChildren<Button>(true).Length);

        // 4. Panel Perfil.
        Transform perfil = canvasGo.transform.Find("Panel_Perfil");
        Transform volver = perfil.Find("Btn_Volver");
        RectTransform vrt = volver as RectTransform;
        LayoutElement vle = volver.GetComponent<LayoutElement>();
        Button vb = volver.GetComponent<Button>();

        sb.AppendLine("4) Btn_Volver anchor " + vrt.anchorMin + " pivot " + vrt.pivot + " pos " + vrt.anchoredPosition +
                      " size " + vrt.sizeDelta + " ignoreLayout " + (vle != null && vle.ignoreLayout) +
                      " listeners " + (vb != null ? vb.onClick.GetPersistentEventCount() : 0));

        Transform card = perfil.Find("TarjetaResultados");
        LayoutElement cle = card.GetComponent<LayoutElement>();
        sb.AppendLine("   TextoResultados eliminado: " + (perfil.Find("TextoResultados") == null));
        sb.AppendLine("   TarjetaResultados " + ((RectTransform)card).sizeDelta + " ignoreLayout " +
                      (cle != null && cle.ignoreLayout) + " textos " + card.childCount);

        int cableados = 0;
        SerializedObject soPerfil = new SerializedObject(Object.FindObjectOfType<BiometricProfile>(true));
        string[] camposPerfil =
        {
            "resultadoTitulo", "resultadoEstado", "kcalTitulo", "kcalNumero", "kcalSufijo",
            "proteinaTitulo", "proteinaNumero", "proteinaSufijo",
            "carboTitulo", "carboNumero", "carboSufijo",
            "grasaTitulo", "grasaNumero", "grasaSufijo",
            "tmbTitulo", "tmbNumero", "tmbSufijo", "metaPesoTexto", "avisoTexto"
        };

        foreach (string campo in camposPerfil)
        {
            SerializedProperty prop = soPerfil.FindProperty(campo);

            if (prop == null || prop.objectReferenceValue == null)
            {
                sb.AppendLine("   SIN CABLEAR: " + campo);
                continue;
            }

            cableados++;
        }

        sb.AppendLine("   Textos de BiometricProfile cableados: " + cableados + "/19");

        // Estado guardado de la escena.
        StringBuilder activos = new StringBuilder();

        foreach (Transform t in canvasGo.transform)
        {
            if (t.gameObject.activeSelf)
            {
                activos.Append(t.name + " ");
            }
        }

        sb.AppendLine("Paneles activos en la escena guardada: " + activos);

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
