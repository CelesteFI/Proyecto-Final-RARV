using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class AuraDumpTextos
{
    public static string Execute()
    {
        StringBuilder sb = new StringBuilder(4096);
        Scene escena = SceneManager.GetActiveScene();
        GameObject[] raices = escena.GetRootGameObjects();

        for (int i = 0; i < raices.Length; i++)
        {
            TMP_Text[] textos = raices[i].GetComponentsInChildren<TMP_Text>(true);

            for (int j = 0; j < textos.Length; j++)
            {
                string ruta = Ruta(textos[j].transform);

                if (!ruta.Contains("MenuLateralAR") && !ruta.Contains("Panel_DashboardMetas") && !ruta.Contains("Panel_MenuPrincipal"))
                {
                    continue;
                }

                sb.AppendLine(ruta + " | \"" + textos[j].text + "\" | fontSize=" + textos[j].fontSize +
                              " | color=" + textos[j].color.ToString("F2") + " | bold=" + (textos[j].fontStyle.ToString()));
            }
        }

        sb.AppendLine();
        sb.AppendLine("=== Imagen / color de los botones del menu lateral ===");

        for (int i = 0; i < raices.Length; i++)
        {
            Button[] botones = raices[i].GetComponentsInChildren<Button>(true);

            for (int j = 0; j < botones.Length; j++)
            {
                string ruta = Ruta(botones[j].transform);

                if (!ruta.StartsWith("Canvas/Panel_AR/MenuLateralAR/"))
                {
                    continue;
                }

                Image img = botones[j].targetGraphic as Image;
                sb.AppendLine(ruta + " | targetGraphic=" + (img != null ? "Image color=" + img.color.ToString("F3") : "NULL") +
                              " | transition=" + botones[j].transition + " | raycast=" + (img != null ? img.raycastTarget.ToString() : "-"));
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
