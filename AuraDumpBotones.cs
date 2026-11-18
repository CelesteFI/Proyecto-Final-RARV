using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class AuraDumpBotones
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

        sb.AppendLine("BOTONES EN ESCENA: " + botones.Count);

        for (int i = 0; i < botones.Count; i++)
        {
            Button b = botones[i];
            sb.AppendLine("--- " + Ruta(b.transform) + " | interactable=" + b.interactable + " | activoEnJerarquia=" + b.gameObject.activeInHierarchy);

            int persistentes = b.onClick.GetPersistentEventCount();

            for (int j = 0; j < persistentes; j++)
            {
                Object objetivo = b.onClick.GetPersistentTarget(j);
                string nombreObjetivo = objetivo == null
                    ? "NULL"
                    : objetivo.GetType().Name + "@" + (objetivo is Component c ? Ruta(c.transform) : objetivo.name);

                sb.AppendLine("    [" + j + "] " + nombreObjetivo + " -> " + b.onClick.GetPersistentMethodName(j));
            }

            if (persistentes == 0)
            {
                sb.AppendLine("    (sin listeners persistentes)");
            }
        }

        sb.AppendLine();
        sb.AppendLine("MENUCONTROLLER / MENULATERAL / SHAPE: metodos publicos");

        MonoBehaviour[] todos = new MonoBehaviour[0];

        for (int i = 0; i < raices.Length; i++)
        {
            MonoBehaviour[] encontrados = raices[i].GetComponentsInChildren<MonoBehaviour>(true);
            MonoBehaviour[] combinados = new MonoBehaviour[todos.Length + encontrados.Length];
            todos.CopyTo(combinados, 0);
            encontrados.CopyTo(combinados, todos.Length);
            todos = combinados;
        }

        for (int i = 0; i < todos.Length; i++)
        {
            MonoBehaviour m = todos[i];

            if (m == null)
            {
                continue;
            }

            string tipo = m.GetType().Name;

            if (tipo != "MenuController" && tipo != "MenuLateralARController" && tipo != "PlateShapeController" && tipo != "PlateAnchorManager" && tipo != "ARUIManager")
            {
                continue;
            }

            sb.AppendLine("== " + tipo + " en " + Ruta(m.transform));

            MethodInfo[] metodos = m.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            for (int j = 0; j < metodos.Length; j++)
            {
                MethodInfo info = metodos[j];

                if (info.IsSpecialName)
                {
                    continue;
                }

                sb.Append("    ").Append(info.ReturnType.Name).Append(' ').Append(info.Name).Append('(');

                ParameterInfo[] parametros = info.GetParameters();

                for (int k = 0; k < parametros.Length; k++)
                {
                    if (k > 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(parametros[k].ParameterType.Name);
                }

                sb.AppendLine(")");
            }
        }

        return sb.ToString();
    }

    private static string Ruta(Transform t)
    {
        if (t == null)
        {
            return "NULL";
        }

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
