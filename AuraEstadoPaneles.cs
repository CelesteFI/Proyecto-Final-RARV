using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AuraEstadoPaneles
{
    public static string Execute()
    {
        StringBuilder sb = new StringBuilder(1024);
        Scene escena = SceneManager.GetActiveScene();

        sb.AppendLine("escena=" + escena.name + " dirty=" + escena.isDirty);

        GameObject[] raices = escena.GetRootGameObjects();

        for (int i = 0; i < raices.Length; i++)
        {
            Transform[] hijos = raices[i].GetComponentsInChildren<Transform>(true);

            for (int j = 0; j < hijos.Length; j++)
            {
                string n = hijos[j].name;

                if (!n.StartsWith("Panel_") || hijos[j].parent == null || hijos[j].parent.name != "Canvas")
                {
                    continue;
                }

                sb.AppendLine("  " + n + " activo=" + hijos[j].gameObject.activeSelf);
            }
        }

        Transform molde = null;
        GameObject[] otraVez = escena.GetRootGameObjects();

        for (int i = 0; i < otraVez.Length; i++)
        {
            Transform[] hijos = otraVez[i].GetComponentsInChildren<Transform>(true);

            for (int j = 0; j < hijos.Length; j++)
            {
                if (hijos[j].name == "MoldeHolograma")
                {
                    molde = hijos[j];
                }
            }
        }

        if (molde != null)
        {
            sb.AppendLine("  MoldeHolograma activo=" + molde.gameObject.activeSelf);
        }

        return sb.ToString();
    }
}
