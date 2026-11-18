using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Inspeccion previa del Panel_Perfil y del componente BiometricProfile.</summary>
public static class AuraInspectPerfil
{
    public static string Execute()
    {
        StringBuilder sb = new StringBuilder();

        GameObject canvasGo = GameObject.Find("Canvas");
        if (canvasGo == null)
        {
            return "FALTA Canvas";
        }

        Transform perfil = canvasGo.transform.Find("Panel_Perfil");
        if (perfil == null)
        {
            return "FALTA Panel_Perfil";
        }

        RectTransform prt = perfil as RectTransform;
        sb.AppendLine("Panel_Perfil rect: " + prt.rect.width + " x " + prt.rect.height +
                      "  anchoredPos " + prt.anchoredPosition + "  anchorMin " + prt.anchorMin +
                      "  anchorMax " + prt.anchorMax + "  pivot " + prt.pivot);

        foreach (Transform hijo in perfil)
        {
            RectTransform rt = hijo as RectTransform;
            string tipo = string.Empty;

            if (hijo.GetComponent<Button>() != null) tipo += "[Button]";
            if (hijo.GetComponent<TMP_Text>() != null) tipo += "[TMP]";
            if (hijo.GetComponent<TMP_InputField>() != null) tipo += "[Input]";
            if (hijo.GetComponent<TMP_Dropdown>() != null) tipo += "[Dropdown]";

            sb.AppendLine("  " + hijo.name + " " + tipo + " pos " + (rt != null ? rt.anchoredPosition.ToString() : "-") +
                          " size " + (rt != null ? rt.sizeDelta.ToString() : "-") + " activo " + hijo.gameObject.activeSelf);
        }

        Transform plantilla = canvasGo.transform.Find("Panel_GuiaNutricional/TituloGuia");
        sb.AppendLine("Plantilla TituloGuia: " + (plantilla != null) + (plantilla != null && plantilla.GetComponent<TMP_Text>() != null ? " (TMP ok)" : " (SIN TMP)"));

        BiometricProfile[] perfiles = Object.FindObjectsOfType<BiometricProfile>(true);
        sb.AppendLine("BiometricProfile encontrados: " + perfiles.Length);

        foreach (BiometricProfile p in perfiles)
        {
            sb.AppendLine("   en '" + p.gameObject.name + "'");
        }

        return sb.ToString();
    }
}
