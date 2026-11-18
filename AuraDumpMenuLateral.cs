using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AuraDumpMenuLateral
{
    public static string Execute()
    {
        StringBuilder sb = new StringBuilder(4096);
        Scene escena = SceneManager.GetActiveScene();
        GameObject[] raices = escena.GetRootGameObjects();
        List<Transform> todos = new List<Transform>();

        for (int i = 0; i < raices.Length; i++)
        {
            Transform[] hijos = raices[i].GetComponentsInChildren<Transform>(true);
            todos.AddRange(hijos);
        }

        sb.AppendLine("=== OBJETOS CON 'Molde' o 'Aro' en el nombre ===");

        for (int i = 0; i < todos.Count; i++)
        {
            string n = todos[i].name;

            if (n.Contains("Molde") || n.Contains("Aros") || n.Contains("Holograma"))
            {
                sb.AppendLine("  " + Ruta(todos[i]) + " | activo=" + todos[i].gameObject.activeSelf);
            }
        }

        sb.AppendLine();
        sb.AppendLine("=== MenuLateralAR y PanelSuperior / PanelInferior: geometria ===");

        for (int i = 0; i < todos.Count; i++)
        {
            string ruta = Ruta(todos[i]);

            if (ruta == "Canvas/Panel_AR/MenuLateralAR" || ruta.StartsWith("Canvas/Panel_AR/MenuLateralAR/") ||
                ruta == "Canvas/Panel_AR/PanelSuperior" || ruta.StartsWith("Canvas/Panel_AR/PanelSuperior/") && !ruta.Contains("dropdownAlimentos"))
            {
                RectTransform rt = todos[i] as RectTransform;

                if (rt == null)
                {
                    sb.AppendLine("  " + ruta + " (sin RectTransform)");
                    continue;
                }

                sb.AppendLine("  " + ruta +
                              " | anchorMin=" + rt.anchorMin.ToString("F2") + " anchorMax=" + rt.anchorMax.ToString("F2") +
                              " pivot=" + rt.pivot.ToString("F2") + " pos=" + rt.anchoredPosition.ToString("F1") +
                              " size=" + rt.sizeDelta.ToString("F1") + " escala=" + rt.localScale.ToString("F2"));
            }
        }

        return sb.ToString();
    }

    private static string Ruta(Transform t)
    {
        StringBuilder sb = new StringBuilder(t.name);
        Transform padre = t.parent;

        while (padre != null)
        {
            sb.Insert(0, padre.name + "/");
            padre = padre.parent;
        }

        return sb.ToString();
    }
}
