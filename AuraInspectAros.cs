using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Estado de los 4 aros en PlateAnchorManager y configuracion del VerticalLayoutGroup del perfil.</summary>
public static class AuraInspectAros
{
    public static string Execute()
    {
        StringBuilder sb = new StringBuilder();

        PlateAnchorManager pam = Object.FindObjectOfType<PlateAnchorManager>(true);
        sb.AppendLine("PlateAnchorManager en: " + (pam != null ? pam.gameObject.name : "NO ENCONTRADO"));

        if (pam != null)
        {
            SerializedObject so = new SerializedObject(pam);
            string[] campos =
            {
                "spriteGuiaCircular", "spriteFijadoCircular", "spriteGuiaCuadrado", "spriteFijadoCuadrado",
                "arosRaiz", "moldeHolograma", "catalogo", "arUiManager"
            };

            foreach (string campo in campos)
            {
                SerializedProperty prop = so.FindProperty(campo);

                if (prop == null)
                {
                    sb.AppendLine("   " + campo + ": CAMPO INEXISTENTE");
                    continue;
                }

                Object refe = prop.objectReferenceValue;
                sb.AppendLine("   " + campo + " -> " + (refe != null ? refe.name : "None"));
            }

            SerializedProperty escala = so.FindProperty("_escalaAros");
            sb.AppendLine("   _escalaAros = " + (escala != null ? escala.floatValue.ToString() : "n/a"));
        }

        Transform aros = GameObject.Find("XR Origin").transform.Find("ArosAR");
        sb.AppendLine("ArosAR: " + (aros != null) + " localScale " + (aros != null ? aros.localScale.ToString() : "-"));

        if (aros != null)
        {
            foreach (Transform t in aros)
            {
                sb.AppendLine("   " + t.name + " activo " + t.gameObject.activeSelf + " localScale " + t.localScale);
            }
        }

        GameObject canvasGo = GameObject.Find("Canvas");
        Transform perfil = canvasGo.transform.Find("Panel_Perfil");
        VerticalLayoutGroup vlg = perfil.GetComponent<VerticalLayoutGroup>();
        sb.AppendLine("VerticalLayoutGroup: " + (vlg != null));

        if (vlg != null)
        {
            sb.AppendLine("   padding " + vlg.padding.left + "," + vlg.padding.right + "," + vlg.padding.top + "," + vlg.padding.bottom);
            sb.AppendLine("   spacing " + vlg.spacing + " alignment " + vlg.childAlignment +
                          " controlW " + vlg.childControlWidth + " controlH " + vlg.childControlHeight +
                          " expandW " + vlg.childForceExpandWidth + " expandH " + vlg.childForceExpandHeight +
                          " scaleW " + vlg.childScaleWidth + " scaleH " + vlg.childScaleHeight);
        }

        Transform card = perfil.Find("TarjetaResultados");
        sb.AppendLine("TarjetaResultados rect " + ((RectTransform)card).rect +
                      " layoutElement " + (card.GetComponent<LayoutElement>() != null));

        return sb.ToString();
    }
}
