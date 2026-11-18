using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class AuraVerificaNavegacion
{
    public static string Execute()
    {
        StringBuilder sb = new StringBuilder(4096);
        Scene escena = SceneManager.GetActiveScene();
        GameObject[] raices = escena.GetRootGameObjects();
        List<Button> botones = new List<Button>();

        for (int i = 0; i < raices.Length; i++)
        {
            botones.AddRange(raices[i].GetComponentsInChildren<Button>(true));
        }

        sb.AppendLine("=== Botones del menu lateral AR (orden de jerarquia) ===");

        for (int i = 0; i < raices.Length; i++)
        {
            Transform menuLateral = Buscar(raices[i].transform, "MenuLateralAR");

            if (menuLateral == null)
            {
                continue;
            }

            for (int j = 0; j < menuLateral.childCount; j++)
            {
                Transform hijo = menuLateral.GetChild(j);
                Button b = hijo.GetComponent<Button>();
                RectTransform rt = hijo as RectTransform;

                string geometria = rt != null
                    ? "pos=" + rt.anchoredPosition.ToString("F0") + " size=" + rt.sizeDelta.ToString("F0") +
                      " y[" + (rt.anchoredPosition.y + rt.sizeDelta.y * 0.5f).ToString("F0") + ".." +
                      (rt.anchoredPosition.y - rt.sizeDelta.y * 0.5f).ToString("F0") + "]"
                    : "-";

                sb.Append("  [" + j + "] " + hijo.name + " | " + geometria);

                if (b == null)
                {
                    sb.AppendLine("   (sin Button)");
                    continue;
                }

                int n = b.onClick.GetPersistentEventCount();
                sb.Append("   -> ");

                for (int k = 0; k < n; k++)
                {
                    Object objetivo = b.onClick.GetPersistentTarget(k);
                    sb.Append((objetivo != null ? objetivo.GetType().Name : "NULL") + "." + b.onClick.GetPersistentMethodName(k) + "  ");
                }

                if (n == 0)
                {
                    sb.Append("SIN LISTENERS");
                }

                sb.AppendLine();
            }

            RectTransform panel = menuLateral as RectTransform;
            sb.AppendLine("  alto del panel = " + panel.sizeDelta.y + " (los hijos cuelgan del borde superior)");
            break;
        }

        sb.AppendLine();
        sb.AppendLine("=== Boton Volver del dashboard ===");

        for (int i = 0; i < botones.Count; i++)
        {
            if (!botones[i].name.Equals("Btn_VolverMetas"))
            {
                continue;
            }

            int n = botones[i].onClick.GetPersistentEventCount();

            for (int k = 0; k < n; k++)
            {
                Object objetivo = botones[i].onClick.GetPersistentTarget(k);
                sb.AppendLine("  Btn_VolverMetas [" + k + "] -> " +
                              (objetivo != null ? objetivo.GetType().Name + "@" + objetivo.name : "NULL") + "." +
                              botones[i].onClick.GetPersistentMethodName(k));
            }

            if (n == 0)
            {
                sb.AppendLine("  Btn_VolverMetas SIN LISTENERS");
            }
        }

        sb.AppendLine();
        sb.AppendLine("=== Etiqueta y estado del boton nuevo ===");

        for (int i = 0; i < botones.Count; i++)
        {
            if (!botones[i].name.Equals("Btn_MenuPrincipal"))
            {
                continue;
            }

            TMPro.TMP_Text etiqueta = botones[i].GetComponentInChildren<TMPro.TMP_Text>(true);
            Image img = botones[i].targetGraphic as Image;

            sb.AppendLine("  etiqueta = \"" + (etiqueta != null ? etiqueta.text : "SIN TMP") + "\" fontSize=" +
                          (etiqueta != null ? etiqueta.fontSize : 0f) +
                          " | color fondo=" + (img != null ? img.color.ToString("F3") : "NULL") +
                          " | interactable=" + botones[i].interactable +
                          " | raycast=" + (img != null ? img.raycastTarget : false));
        }

        return sb.ToString();
    }

    private static Transform Buscar(Transform raiz, string nombre)
    {
        if (raiz.name == nombre)
        {
            return raiz;
        }

        Transform[] hijos = raiz.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < hijos.Length; i++)
        {
            if (hijos[i].name == nombre)
            {
                return hijos[i];
            }
        }

        return null;
    }
}
