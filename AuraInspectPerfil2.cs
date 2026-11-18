using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>Verifica el cableado de los 20 textos del Panel_Perfil.</summary>
public static class AuraInspectPerfil2
{
    public static string Execute()
    {
        StringBuilder sb = new StringBuilder();

        GameObject canvasGo = GameObject.Find("Canvas");
        Transform perfil = canvasGo.transform.Find("Panel_Perfil");
        RectTransform prt = perfil as RectTransform;

        sb.AppendLine("Panel_Perfil " + prt.rect.width + " x " + prt.rect.height);

        Transform volver = perfil.Find("Btn_Volver");
        RectTransform vrt = volver as RectTransform;
        sb.AppendLine("Btn_Volver anchor " + vrt.anchorMin + " / " + vrt.anchorMax + " pivot " + vrt.pivot +
                      " pos " + vrt.anchoredPosition + " size " + vrt.sizeDelta);

        foreach (Transform hijo in volver)
        {
            RectTransform hrt = hijo as RectTransform;
            sb.AppendLine("   hijo '" + hijo.name + "' size " + (hrt != null ? hrt.sizeDelta.ToString() : "-"));
        }

        sb.AppendLine("TextoResultados presente: " + (perfil.Find("TextoResultados") != null));

        Transform card = perfil.Find("TarjetaResultados");
        RectTransform crt = card as RectTransform;
        sb.AppendLine("TarjetaResultados anchor " + crt.anchorMin + " / " + crt.anchorMax + " pivot " + crt.pivot +
                      " pos " + crt.anchoredPosition + " size " + crt.sizeDelta + " hijos " + card.childCount);

        foreach (Transform t in card)
        {
            TMP_Text tmp = t.GetComponent<TMP_Text>();
            RectTransform trt = t as RectTransform;
            sb.AppendLine("   '" + t.name + "' pos " + trt.anchoredPosition + " size " + trt.sizeDelta +
                          " fs " + (tmp != null ? tmp.fontSize : -1f) + " txt '" + (tmp != null ? tmp.text : "?") + "'");
        }

        BiometricProfile p = Object.FindObjectOfType<BiometricProfile>(true);
        SerializedObject so = new SerializedObject(p);
        string[] campos =
        {
            "resultadoTitulo", "resultadoEstado", "kcalTitulo", "kcalNumero", "kcalSufijo",
            "proteinaTitulo", "proteinaNumero", "proteinaSufijo",
            "carboTitulo", "carboNumero", "carboSufijo",
            "grasaTitulo", "grasaNumero", "grasaSufijo",
            "tmbTitulo", "tmbNumero", "tmbSufijo", "metaPesoTexto", "avisoTexto"
        };

        int ok = 0;

        foreach (string campo in campos)
        {
            SerializedProperty prop = so.FindProperty(campo);

            if (prop == null)
            {
                sb.AppendLine("   CAMPO INEXISTENTE: " + campo);
                continue;
            }

            Object refe = prop.objectReferenceValue;

            if (refe != null)
            {
                ok++;
            }

            sb.AppendLine("   " + campo + " -> " + (refe != null ? refe.name : "None"));
        }

        sb.AppendLine("Cableados " + ok + "/" + campos.Length);

        return sb.ToString();
    }
}
